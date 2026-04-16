using Clear;
using FirstReg.Data;
using ICG.NetCore.Utilities.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FirstReg.OnlineAccess.Controllers;

[Authorize]
[Route("co")]
public class CompanySecController : Controller
{
    private readonly ILogger<CompanySecController> _logger;
    private readonly Service _service;
    private readonly ISpreadsheetGenerator _exportGenerator;
    private readonly EStockApiUrl _apiUrl;
    private readonly IApiClient _apiClient;

    public CompanySecController(ILogger<CompanySecController> logger, Service service,
        ISpreadsheetGenerator exportGenerator, IApiClient apiClient, EStockApiUrl apiUrl)
    {
        _logger = logger;
        _service = service;
        _exportGenerator = exportGenerator;
        _apiClient = apiClient;
        _apiUrl = apiUrl;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var user = await _service.Data.Get<User>(x => x.UserName.ToLower() == User.Identity.Name);
            var regsumm = await _apiClient.GetAsync<Bson.RegSummary>(
                $"{_apiUrl.GetRegisterSummary}/{user.Register.Id}", "", Common.ApiKeyHeader);

            return View(regsumm);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
            return StatusCode(StatusCodes.Status500InternalServerError, Clear.Tools.GetAllExceptionMessage(ex));
        }
    }

    [Route("shareholders")]
    public IActionResult Shareholders(string s, decimal min, decimal max, DateTime? mindate, DateTime? maxdate)
    {
        try
        {
            return View(new RegisterSHSumm()
            {
                S = s,
                MaxUnits = 1000000000000, //1000000000000,
                Min = min > 0 ? min : 0,
                Max = max > 0 ? max : 1000000000000,
                MinDate = mindate,
                MaxDate = maxdate,
                ListUrl = Url.Action(nameof(GetShareholderLists), new { s, min, max, mindate, maxdate }),
                ExportUrl = Url.Action(nameof(DownloadShareholderLists), new { s, min, max, mindate, maxdate }),
                DetailsUrl = Url.Action(nameof(GetShareholderDetails))
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
            TempData["error"] = ex.Message;
            return View(new RegisterSHSumm());
        }
    }

    private async Task<List<Bson.RegHolding>> FetchShareholderLists(string s, decimal min,
        decimal max, DateTime? minDate = null, DateTime? maxDate = null, int page = 1, int pagesize = 10000)
    {
        var user = await _service.Data.Get<User>(x => x.UserName.ToLower() == User.Identity.Name);
        string url = $"{_apiUrl.GetRegisterShareholder}/{user.Register.Id}?s={s}&min={min}&max={max}&page={page}&size={pagesize}";

        if (minDate is not null && maxDate is not null)
        {
            url = $"{url}&mindate={minDate:dd/MMM/yyy}&maxdate={maxDate:dd/MMM/yyy}";
        }

        var unitArrays = await _apiClient.GetAsync<List<string[]>>(url, "", Common.ApiKeyHeader);

        return unitArrays.Select(x => new Bson.RegHolding(x)).ToList();
    }

    [HttpGet("shareholders-list")]
    public async Task<IActionResult> GetShareholderLists(string s, decimal min, decimal max, DateTime? mindate, DateTime? maxdate)
    => await GetShareholderListz(s, min, max, mindate, maxdate);

    [HttpGet("shareholders-list/l")]
    public async Task<IActionResult> GetShareholderListz(string s, decimal min, decimal max, DateTime? mindate, DateTime? maxdate)
    {
        try
        {
            var shs = await FetchShareholderLists(s, min, max, mindate, maxdate);

            return Ok(new
            {
                data = shs.OrderBy(x => x.FullName).Select(x => new[]
                {
                        string.Join("<br>", new [] { x.AccountNo.ToString(), x.ClearingNo }.Where(a => !string.IsNullOrEmpty(a))),
                    x.FullName,
                        x.Units.ToString("N0"),
                    string.Join("<br>", new [] { x.Address, $"{x.Phone} {x.Mobile} {x.Email}".Replace("  ", "").Trim() }.Where(a => !string.IsNullOrEmpty(a))),
                        x.AccountNo.ToString(),
                    x.Id.ToString()
                    }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
            return StatusCode(StatusCodes.Status500InternalServerError, Clear.Tools.GetAllExceptionMessage(ex));
        }
    }

    [HttpGet("shareholders-list/d")]
    public async Task<IActionResult> DownloadShareholderLists(string s, decimal min, decimal max, DateTime? mindate, DateTime? maxdate)
    {
        try
        {
            var stream = Tools.ExportToExcel(await FetchShareholderLists(s, min, max, mindate, maxdate), "");

            byte[] fileContent = stream.ToArray(); // simpler way of converting to array
            stream.Close();

            return File(fileContent, "application/force-download", $"shareholders-{DateTime.Now:yyyMMddHHmmss}.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
            return StatusCode(StatusCodes.Status500InternalServerError, Clear.Tools.GetAllExceptionMessage(ex));
        }
    }

    [HttpGet("shareholders-list/download-all")]
    public async Task<IActionResult> DownloadAllShareholderLists()
    {
        try
        {
            var stream = Tools.ExportToXml(await FetchShareholderLists("", 0, decimal.MaxValue, pagesize: int.MaxValue));

            byte[] fileContent = stream.ToArray(); // simpler way of converting to array
            stream.Close();

            return File(fileContent, "application/force-download", $"shareholders-{DateTime.Now:yyyMMddHHmmss}.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
            return StatusCode(StatusCodes.Status500InternalServerError, Clear.Tools.GetAllExceptionMessage(ex));
        }
    }

    [HttpGet("shareholder/{accno?}")]
    public async Task<IActionResult> GetShareholderDetails(int accno)
    {
        try
        {
            var user = await _service.Data.Get<User>(x => x.UserName.ToLower() == User.Identity.Name);
            var sh = await _apiClient.GetAsync<RegSH>(
                $"{_apiUrl.GetUnits}/{user.Register.Id}/{accno}", "", Common.ApiKeyHeader);

            return Ok(new RegisterHolderModel(sh));
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
            return StatusCode(StatusCodes.Status500InternalServerError, Clear.Tools.GetAllExceptionMessage(ex));
        }
    }

    [HttpGet("shareholder/{accno?}/download")]
    public async Task<IActionResult> DownloadShareholderDetails(int accno)
    {
        try
        {
            var user = await _service.Data.Get<User>(x => x.UserName.ToLower() == User.Identity.Name);
            var sh = await _apiClient.GetAsync<RegSH>(
                $"{_apiUrl.GetUnits}/{user.Register.Id}/{accno}", "", Common.ApiKeyHeader);

            var regmodel = new RegisterHolderModel(sh);

            var stream = Tools.ExportToXml(regmodel);

            byte[] fileContent = stream.ToArray(); // simpler way of converting to array
            stream.Close();

            return File(fileContent, "application/force-download", $"shareholder-{accno}-{DateTime.Now:yyyMMddHHmmss}.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
            return StatusCode(StatusCodes.Status500InternalServerError, Clear.Tools.GetAllExceptionMessage(ex));
        }
    }

    #region profile

    [Route("profile")]
    public async Task<IActionResult> Profile() => View(
        await _service.Data.Get<User>(x => x.UserName.ToLower() == User.Identity.Name.ToLower()));

    [Route("profile/update")]
    public async Task<IActionResult> UpdateProfile(UserModel model)
    {
        try
        {
            var user = await _service.Data.Get<User>(x =>
                x.UserName.ToLower() == User.Identity.Name.ToLower());

            user.FullName = model.FullName.Trim();
            user.PhoneNumber = model.MobileNo.Trim();

            await _service.Data.UpdateAsync(user);

            TempData["success"] = "Your profile was updated";

            return RedirectToAction("Profile");
        }
        catch (Exception ex)
        {
            TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
        }
        return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
    }

    #endregion
}