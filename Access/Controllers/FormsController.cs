using Clear;
using DocumentFormat.OpenXml.EMMA;
using FirstReg.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NuGet.Packaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FirstReg.OnlineAccess.Controllers;

[Route(Routes.Forms)]
public class FormsController(ILogger<BaseController> logger, Service service)
    : BaseController(service, AuditLogSection.Forms)
{
    [HttpGet(Routes.DataUpdate)]
    public IActionResult DataUpdate() => View();

    [HttpPost(Routes.DataUpdate)]
    public async Task<IActionResult> DataUpdate(DataUpdateModel model, string signatureString, IFormFile signatureFile)
    => await SaveForm(Forms.DataUpload, model, signatureString, signatureFile);


    [HttpGet(Routes.ChangeOfAddress)]
    public IActionResult ChangeOfAddress() => View();

    [HttpPost(Routes.ChangeOfAddress)]
    public async Task<IActionResult> ChangeOfAddress(ChangeOfAddressModel model, string signatureString, IFormFile signatureFile)
    => await SaveForm(Forms.ChangeOfAddress, model, signatureString, signatureFile);


    [HttpGet(Routes.DividendCard)]
    public IActionResult DividendCard() => View();


    [HttpGet(Routes.EDividend)]
    public IActionResult EDividend() => View();

    [HttpPost(Routes.EDividend)]
    public async Task<IActionResult> EDividend(EDividendModel model, string signatureString, IFormFile signatureFile)
    => await SaveForm(Forms.EDividend, model, signatureString, signatureFile);

    [HttpGet(Routes.ShareholderUpdate)]
    public async Task<IActionResult> ShareholderUpdate()
    {
        return View();
    }

    [HttpPost(Routes.ShareholderUpdateSearch)]
    public async Task<IActionResult> ShareholderUpdateSearch(string name)
    {
        try
        {
            if (string.IsNullOrEmpty(name))
            {
                return View(nameof(ShareholderUpdate));
            }

            var list = await GetShareholderUpdateList();

            var matchingItems = FindMatchingItems(name, list);

            return View(matchingItems);
        }
        catch (Exception ex)
        {
            return Ok(Clear.Tools.GetAllExceptionMessage(ex));
        }
    }

    private static async Task<HashSet<AccountNameModel>> GetShareholderUpdateList()
    {
        var list = (await JsonFileHelper.ReadOandoShareholderAsync())
            .Select(x => new AccountNameModel(x.AccountNo, x.Name, x.Address, x.Phone)).ToHashSet();

        return list;
    }

    [HttpGet(Routes.ShareholderUpdateForm)]
    public async Task<IActionResult> ShareholderUpdateForm(string account)
    {
        try
        {
            if (string.IsNullOrEmpty(account))
            {
                return View(nameof(ShareholderUpdate));
            }

            var list = await GetShareholderUpdateList();

            var item = list.FirstOrDefault(x => x.AccountNo == account)
                ?? throw new InvalidOperationException("Account not found");

            var existingResponse = await service.Data.GetAsQueryable<FormResponse>()
                .Where(x => x.UniqueKey == account)
                .FirstOrDefaultAsync();

            var model = new ShareholderUpdateModel
            {
                Id = item.AccountNo,
                AccountNo = item.AccountNo,
                OtherName = item.Name
            };

            if (existingResponse != null)
            {
                var data = existingResponse?.GetData<ShareholderUpdateModel>();

                model.Phone = existingResponse?.PhoneNumber;
                model.Email = existingResponse?.EmailAddress;
                model.ClearingNo = data?.ClearingNo;
                model.StockBroker = data?.StockBroker;

                return View("ShareholderUpdateExists", model);
            }

            return View(model);
        }
        catch (Exception ex)
        {
            return Ok(Clear.Tools.GetAllExceptionMessage(ex));
        }
    }

    [HttpPost(Routes.ShareholderUpdate)]
    public async Task<IActionResult> ShareholderUpdate(ShareholderUpdateModel model)
    => await SaveForm(Forms.ShareholderUpdate, model);


    [HttpGet(Routes.Completed)]
    public IActionResult Completed()
    {
        return View();
    }

    public async Task<IActionResult> SaveForm<TForm>(Forms formType,
        TForm model, string signatureString = null, IFormFile signatureFile = null) where TForm : IFormModel
    {
        try
        {
            model.Id ??= Clear.Tools.StringUtility.GetDateCode();

            if (model.RequiresSignature)
            {
                model.Signature = GetSignature(signatureString, signatureFile);
            }

            if (model.RequiresHoldings)
            {
                model.Holdings.AddRange(GetHoldings());
            }

            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            await service.Data.SaveAsync(FormResponse.Create(model.Id,
                formType, JsonSerializer.Serialize(model, options), model.FullName,
                model.Phone, model.Email, DateTime.UtcNow)
            );

            TempData["success"] = "Your data was successfully submitted";

            return RedirectToAction(nameof(Completed));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, Clear.Tools.GetAllExceptionMessage(ex));
            TempData["error"] = "Your data could not be submitted";

            return View(model);
        }
    }

    private IEnumerable<DataHoldingModel> GetHoldings()
    {
        string key = "RegisterId";
        return Request.Form
            .Where(x => x.Key.Contains(key))
            .Select(a => new DataHoldingModel(int.Parse(a.Value), Request.Form[a.Key.Replace(key, "AccountNo")]));
    }

    private static IEnumerable<AccountNameModel> FindMatchingItems(string name, IEnumerable<AccountNameModel> list)
    {
        var nameParts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return list
            .Select(item => new
            {
                Item = item,
                MatchCount = nameParts.Count(part => item.Name.Contains(part, StringComparison.OrdinalIgnoreCase))
            })
            .Where(x => x.MatchCount > 0)
            .OrderByDescending(x => x.MatchCount)
            .Select(x => x.Item);
    }
}