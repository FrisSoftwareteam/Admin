using Clear;
using FirstReg.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace FirstReg.OnlineAccess.Controllers;

[Route(Routes.ShareOffer)]
public class ShareOfferController(ILogger<BaseController> logger, Service service,
    IApiClient apiClient, EStockApiUrl apiUrl) : BaseController(service, AuditLogSection.ShareOffer)
{
    [HttpGet(Routes.ShareOfferHome)]
    public async Task<IActionResult> Index(string code)
    {
        try
        {
            if (!string.IsNullOrEmpty(code))
            {
                var offer = await service.Data.Get<ShareOffer>(x => x.UniqueKey == code);

                if (offer != null)
                {
                    return View("Offer", offer);
                }
                else
                {
                    ViewData["error"] = $"Share offer not found with the reference: {code}";
                    TempData["error"] = ViewData["error"];
                }
            }

            var date = DateTime.Now.Date;

            return View(await service.Data.Find<ShareOffer>(x => x.StartDate.Date <= date && x.EndDate >= date));

        }
        catch (Exception ex)
        {
            TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
            ViewData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
            return RedirectToAction("Error");
        }
    }

    [HttpGet(Routes.PublicOffer)]
    public async Task<IActionResult> PublicOffer(string code)
    {
        try
        {
            ShareOffer offer = await GetOffer(code, ShareSubscriptionType.PublicOffer);

            ViewBag.Code = code;
            ViewBag.Title = $"{offer.Description} - Public Offer";

            return View(PublicOfferModel.Create(offer.Id, offer.PublicOffer.Minimum, offer.PublicOffer.Factor));
        }
        catch (Exception ex)
        {
            TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
            ViewData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
            return View("Error");
        }
    }

    private async Task<ShareOffer> GetOffer(string code, ShareSubscriptionType type)
    {
        var offer = await service.Data.Get<ShareOffer>(x => x.UniqueKey == code)
                        ?? throw new InvalidOperationException($"No offer was found with the code: {code}, please check and try again");

        if (offer.StartDate > DateTime.Now)
            throw new InvalidOperationException($"The share offer with the code: {code} is not started, please check back by {offer.StartDate:dddd dd MMMM yyy}");

        if (offer.EndDate < DateTime.Now)
            throw new InvalidOperationException($"The share offer with the code: {code} ended on {offer.EndDate:dddd dd MMMM yyy}");

        if (type == ShareSubscriptionType.PublicOffer && !offer.AllowPublicOffer)
            throw new InvalidOperationException($"The share offer with the code: {code} does not allow public offer subscription");

        if (type == ShareSubscriptionType.RightIssue && !offer.AllowRightIssue)
            throw new InvalidOperationException($"The share offer with the code: {code} does not allow right issue subscription");

        return offer;
    }

    [HttpPost(Routes.PublicOffer)]
    public async Task<IActionResult> PublicOffer(PublicOfferModel model, string signatureString, IFormFile signatureFile)
    => await SaveForm(ShareSubscriptionType.PublicOffer, model, signatureString, signatureFile);


    [HttpGet(Routes.RightIssue)]
    public async Task<IActionResult> RightIssue(string code, string acc)
    {
        try
        {
            if (string.IsNullOrEmpty(acc))
            {
                return View("RightStart");
            }

            ShareOffer offer = await GetOffer(code, ShareSubscriptionType.RightIssue);

            var sh = await apiClient.GetAsync<RegSH>(
                $"{apiUrl.GetUnits}/{offer.RegisterId}/{acc}", "", Common.ApiKeyHeader);

            var issue = new RightIssueModel
            {
                OfferId = offer.Id,
                TotalUnits = sh.TotalUnits,
                Rights = (int)sh.TotalUnits / offer.RightIssue.NoOfShares * offer.RightIssue.Rights,
                LastName = sh.LastName,
                OtherName = $"{sh.FirstName} {sh.MiddleName}".Trim(),
                ClearingNo = sh.ClearingNo,
                Phone = sh.Phone,
                Email = sh.Email,
            };

            ViewBag.Code = code;
            ViewBag.Title = $"{offer.Description} - Right Issue";

            return View(issue);
        }
        catch (Exception ex)
        {
            TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
            ViewData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
            return View("Error");
        }
    }

    [HttpPost(Routes.RightIssue)]
    public async Task<IActionResult> RightIssue(RightIssueModel model, string signatureString, IFormFile signatureFile)
    => await SaveForm(ShareSubscriptionType.RightIssue, model, signatureString, signatureFile);

    public async Task<IActionResult> SaveForm<TForm>(ShareSubscriptionType subscriptionType,
        TForm model, string signatureString, IFormFile signatureFile) where TForm : IShareFormModel
    {
        try
        {
            if (model.NoOfShares <= 0)
            {
                throw new InvalidOperationException("Please enter number of shares you'd like to purchase");
            }

            model.Id = Clear.Tools.StringUtility.GetDateCode();
            model.Signature = GetSignature(signatureString, signatureFile);

            var formType = subscriptionType switch
            {
                ShareSubscriptionType.PublicOffer => Forms.PublicOffer,
                ShareSubscriptionType.RightIssue => Forms.RightIssue,
                _ => throw new InvalidOperationException($"The share subscription type: {subscriptionType} is not recognized")
            };

            await service.Data.SaveAsync(ShareSubscription.Create(model.OfferId, subscriptionType, model.NoOfShares, model.Rights,
                FormResponse.Create(model.Id, formType, JsonSerializer.Serialize(model), model.FullName, model.Phone, model.Email, DateTime.UtcNow)));

            TempData["success"] = "Your data was successfully submitted";

            try
            {
                await service.Email.SendShareOfferCompleted(model, subscriptionType, DateTime.UtcNow);
            }
            catch (Exception)
            {
                TempData["error"] = "Notification email could not be sent";
            }

            return RedirectToAction(nameof(Subscription), new { code = model.Id });
        }
        catch (Exception ex)
        {
            logger.LogError(Clear.Tools.GetAllExceptionMessage(ex));
            TempData["error"] = $"Your data could not be submitted because {ex.Message}";

            return View(model);
        }
    }

    [HttpGet(Routes.ShareOfferSubscription)]
    public async Task<IActionResult> Subscription(string code)
    {
        try
        {
            if (string.IsNullOrEmpty(code))
            {
                return View("FindSubscription");
            }

            ShareSubscription subscription = await service.Data.Get<ShareSubscription>(x => x.Response.UniqueKey == code)
                ?? throw new InvalidOperationException($"The share subscription - {code} was not found");

            return View(new ShareSubscriptionPageModel(subscription));
        }
        catch (Exception ex)
        {
            TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
            ViewData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
            return View("Error");
        }
    }

    [HttpPost(Routes.FindShareOfferSubscription)]
    public async Task<IActionResult> FindSubscription(FindShareSubscriptionModel model)
    {
        try
        {
            model.Query = model.Query.ToLower();

            var subscriptions = await service.Data.Find<ShareSubscription>(x =>
                x.Response.UniqueKey.ToLower() == model.Query ||
                x.Response.PhoneNumber.ToLower() == model.Query ||
                x.Response.EmailAddress.ToLower() == model.Query
            );

            if (subscriptions.Count == 0)
            {
                throw new InvalidOperationException($"We did not find any subscription with the details you have entered: {model.Query}");
            }

            if (subscriptions.Count == 1)
            {
                return RedirectToAction(nameof(Subscription), new { code = subscriptions.First().Code });
            }

            throw new InvalidOperationException(
                $"We found multiple subscriptions with the details you entered - {model.Query}, " +
                $"please use the reference in the email you received from us");

            //return View("SelectSubscription", subscriptions);
        }
        catch (Exception ex)
        {
            TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
            ViewData["error"] = Clear.Tools.GetAllExceptionMessage(ex);

            return View(model);
        }
    }


    [HttpGet(Routes.PayShareOffer)]
    public async Task<IActionResult> Pay(string code)
    {
        try
        {
            if (string.IsNullOrEmpty(code))
                throw new InvalidOperationException("Share subscription reference cannot be empty");

            ShareSubscription subscription = await service.Data.Get<ShareSubscription>(x => x.Response.UniqueKey == code)
                ?? throw new InvalidOperationException($"The share subscription - {code} was not found");

            if (!string.IsNullOrEmpty(subscription.PaymentId))
            {
                ViewData["error"] = $"The share subscription - {code} is already paid";
                TempData["error"] = ViewData["error"];

                return RedirectToAction(nameof(Subscription), new { code });
            }

            return View("Pay", PaymentModel.Create(PaymentItem.ShareSubscription, subscription.Response.FullName,
                subscription.Response.EmailAddress, subscription.Response.PhoneNumber,
                $"Payment for {subscription.Type} subscription: {code} for {subscription.NoOfShares} units for {subscription.Offer.Description}.",
                subscription.NoOfShares * subscription.Offer.GetPrice(subscription.Type) ?? throw new InvalidOperationException("Could not get offer price"),
                code, Tools.PaymentSettings, null)
            );
        }
        catch (Exception ex)
        {
            TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
            ViewData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
            return View("Error");
        }
    }


    [HttpPost(Routes.PayShareOffer)]
    public async Task<IActionResult> Pay(string code, BankPayModel model, IFormFile mfile)
    {
        try
        {
            ShareSubscription subscription = await service.Data.Get<ShareSubscription>(x => x.Response.UniqueKey == model.Id)
                ?? throw new InvalidOperationException($"The share subscription - {code} was not found");

            if (!string.IsNullOrEmpty(subscription.PaymentId))
                throw new InvalidOperationException($"The share subscription - {code} is already paid");

            model.Id ??= Clear.Tools.StringUtility.GetDateCode();
            model.Account = Tools.BankAccount;
            model.User = subscription.Response.FullName;

            var payment = Payment.CreateForShareOffer(model, subscription.Type,
                subscription.Offer.Description, subscription.Response.FullName, Tools.Now);

            subscription.Payment = payment;

            await service.Data.UpdateAsync(subscription);

            await SendProofOfPayment(mfile, payment, new(subscription.Response.EmailAddress, subscription.Response.FullName));

            return RedirectToAction(nameof(Subscription), new { code = model.Id });
        }
        catch (Exception ex)
        {
            TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
            ViewData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
            return View("Error");
        }
    }
}