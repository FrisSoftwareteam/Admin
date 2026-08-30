using Clear;
using FirstReg.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirstReg.Admin.Controllers;

[Authorize]
[Route("share-offers")]
public class ShareOffersController(ILogger<ShareOffersController> logger, Service service) : Controller
{
    private readonly ILogger<ShareOffersController> _logger = logger;

    [HttpGet]
    public async Task<IActionResult> Index()
    => View(await service.Data.Get<ShareOffer>());

    private async Task<ShareOffer> GetShareOffer(string code)
    => (await service.Data.Get<ShareOffer>(x => x.UniqueKey.ToLower() == code.ToLower())) ??
        throw new InvalidOperationException($"Share offer with the code {code} was not found");

    private async Task<ShareSubscription> GetSubscription(string code)
    => (await service.Data.Get<ShareSubscription>(x => x.Response.UniqueKey.ToLower() == code.ToLower())) ??
        throw new InvalidOperationException($"Share subscription with the code {code} was not found");

    [HttpGet("{code}")]
    public async Task<IActionResult> Details(string code)
    {
        try
        {
            var offer = await GetShareOffer(code);
            return View(new ShareOfferPageModel(
                Url.Action(nameof(GetSubscriptionLists), new { id = offer.Id }),
                Url.Action(nameof(DownloadShareSubscriptions), new { id = offer.Id }),
                offer
            ));
        }
        catch (Exception ex)
        {
            TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet("subscription/{code}")]
    public async Task<IActionResult> Subscription(string code)
    {
        try
        {
            return View(await GetSubscription(code));
        }
        catch (Exception ex)
        {
            TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet("update/{code?}")]
    public async Task<IActionResult> Update(string code)
    {
        try
        {
            if (string.IsNullOrEmpty(code))
            {
                return View(new ShareOfferModel());
            }

            var offer = await GetShareOffer(code);
            if (offer is null) return NotFound("The offer does not exist");
            return View(new ShareOfferModel(offer));
        }
        catch (Exception ex)
        {
            TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost("update/{code?}")]
    public async Task<IActionResult> Update(ShareOfferModel model, string code)
    {
        try
        {
            // validations

            if (model.RegisterId == 0)
                throw new InvalidOperationException("Please select a register");

            if (model.StartDate > model.EndDate)
                throw new InvalidOperationException("Start date cannot be earlier than end date");

            if (model.AllowPublicOffer == false && model.AllowRightIssue == false)
                throw new InvalidOperationException("Please enable public offer or right issue or both");

            if (model.AllowPublicOffer && (model.PublicOfferPrice <= 0 || model.PublicOfferMinimum <= 0 || model.PublicOfferFactor <= 0))
                throw new InvalidOperationException("Please set the public offer values");

            if (model.AllowRightIssue && (model.RightIssuePrice <= 0 || model.RightIssueRights <= 0 || model.RightIssueUnits <= 0 || model.RightIssueFactor <= 0))
                throw new InvalidOperationException("Please set the rights issue values");


            if (model.Id > 0) // update
            {
                var offer = await GetShareOffer(code);

                if (offer is null) return NotFound("The offer does not exist");

                //offer.RegisterId = model.RegisterId;
                //offer.UniqueKey = model.UniqueKey;
                offer.Description = model.Description;
                offer.StartDate = model.StartDate;
                offer.EndDate = model.EndDate;

                offer.AllowPublicOffer = model.AllowPublicOffer;
                offer.PublicOffer.Minimum = model.PublicOfferMinimum;
                offer.PublicOffer.Factor = model.PublicOfferFactor;
                offer.PublicOffer.Price = model.PublicOfferPrice;

                offer.AllowRightIssue = model.AllowRightIssue;
                offer.RightIssue.NoOfShares = model.RightIssueUnits;
                offer.RightIssue.Rights = model.RightIssueRights;
                offer.RightIssue.Factor = model.RightIssueFactor;
                offer.RightIssue.Price = model.RightIssuePrice;

                await service.Data.UpdateAsync(offer);
            }
            else
            {
                model.UniqueKey = Clear.Tools.StringUtility.GetDateCode();

                await service.Data.SaveAsync<ShareOffer>(new ShareOffer
                {
                    RegisterId = model.RegisterId,
                    UniqueKey = model.UniqueKey,
                    Description = model.Description,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    AllowPublicOffer = model.AllowPublicOffer,
                    AllowRightIssue = model.AllowRightIssue,
                    PublicOffer = new ShareOfferPublic
                    {
                        Minimum = model.PublicOfferMinimum,
                        Factor = model.PublicOfferFactor,
                        Price = model.PublicOfferPrice,
                    },
                    RightIssue = new ShareOfferRights
                    {
                        NoOfShares = model.RightIssueUnits,
                        Rights = model.RightIssueRights,
                        Factor = model.RightIssueFactor,
                        Price = model.RightIssuePrice,
                    },
                    Date = Tools.Now
                });
            }

            TempData["success"] = "Successful";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
            TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
            return View(model);
        }
    }

    [Route("delete/{code}")]
    public async Task<IActionResult> Delete(string code)
    {
        try
        {
            if (string.IsNullOrEmpty(code))
                throw new InvalidOperationException("You have not selected any offer to be deleted");

            var offer = await GetShareOffer(code);

            if (offer.Subscriptions.Count > 0)
                throw new InvalidOperationException($"The offer cannot be deleted, because it has {offer.Subscriptions.Count} subscriptions");

            await service.Data.DeleteAsync(offer);

            TempData["success"] = "The offer was successfully deleted";
        }
        catch (Exception ex)
        {
            TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("subscription/list")]
    public async Task<IActionResult> GetSubscriptionLists(int id, ShareSubscriptionType? t)
    {
        try
        {
            var query = service.Data.GetAsQueryable<ShareSubscription>()
                .Where(x => x.ShareOfferId == id);

            StringBuilder sb = new($"SELECT * FROM AspNetUsers WHERE (Id > 0) ");

            if (t != null)
            {
                query = query.Where(x => x.Type == t);
            }

            var list = await query.ToListAsync();

            return Ok(new
            {
                data = list.Select(x => new[]
                {
                    x.Code,
                    x.TypeDescription,
                    x.Response.FullName,
                    x.NoOfShares.ToString(),
                    x.Amount.ToString("N2"),
                    x.Payment == null ? "Pending" : "Complete",
                    x.Id.ToString(),
                    x.Payment == null ? "light" : "success",
                    Url.Action(nameof(Subscription), new { code = x.Code })
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
            return StatusCode(StatusCodes.Status500InternalServerError, Clear.Tools.GetAllExceptionMessage(ex));
        }
    }

    [HttpGet("subscriptions/download")]
    public async Task<IActionResult> DownloadShareSubscriptions(int id)
    {
        try
        {
            var subscription = await service.Data.Find<ShareSubscription>(x => x.ShareOfferId == id);

            var stream = Tools.ExportToXml(subscription);

            byte[] fileContent = stream.ToArray(); // simpler way of converting to array
            stream.Close();

            return File(fileContent, "application/force-download", $"offer-subscripions-{DateTime.Now:yyyMMddHHmmss}.xlsx");
        }
        catch (Exception ex)
        {
            logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
            return StatusCode(StatusCodes.Status500InternalServerError, Clear.Tools.GetAllExceptionMessage(ex));
        }
    }
}