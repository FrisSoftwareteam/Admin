using FirstReg.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FirstReg.OnlineAccess.Controllers;

[Authorize]
[Route("broker")]
public class StockBrokerController : Controller
{
    private readonly ILogger<StockBrokerController> _logger;
    private readonly Service _service;

    public StockBrokerController(ILogger<StockBrokerController> logger, Service service)
    {
        _logger = logger;
        _service = service;
    }

    public async Task<IActionResult> Index() => View(new StockBrokerDashboardModel(
        await _service.Data.Get<User>(x => x.UserName.ToLower() == User.Identity.Name.ToLower())));

    #region accounts

    [HttpPost("ecert/add")]
    public async Task<IActionResult> AddCertRequest(CertRequestModel model, IFormFile mfile)
    {
        try
        {
            if (model.Id > 0) return await UpdateCertRequest(model, mfile);

            if (!(mfile.ContentType.Contains("pdf") || mfile.ContentType.Contains("image")))
                throw new InvalidOperationException($"The select file - {mfile.ContentType} is invalid");

            var user = await _service.Data.Get<User>(x => x.UserName.ToLower() == User.Identity.Name.ToLower());

            if (!(mfile is not null && mfile.Length > 0))
                throw new InvalidOperationException("Please add a file to be uploaded for processing");

            string filename = Tools.GenerateFileName($"{Tools.Now:yyMMddHHmmss} request", mfile.FileName.Split('.').Last());

            await Tools.UploadFileAsync(mfile, filename, blobfolder.certs);

            string code = Clear.Tools.StringUtility.GetDateCode();
            await _service.Data.SaveAsync(new ECertRequest
            {
                Date = Tools.Now,
                StockBrokerId = user.Id,
                Description = model.Description,
                Brief = model.Brief,
                Code = code,
                AuthLetterFileName = filename,
                Status = ECertStatus.Pending
            });

            TempData["success"] = "Your new request was added";

            RedirectToAction(nameof(ViewCertRequest), new { code });
        }
        catch (Exception ex)
        {
            TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
        }
        return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
    }

    [HttpPost("ecert/update")]
    public async Task<IActionResult> UpdateCertRequest(CertRequestModel model, IFormFile mfile)
    {
        try
        {
            if (model.Id == 0) return await AddCertRequest(model, mfile);

            var certs = await _service.Data.Find<ECertRequest>(x => x.Id == model.Id);

            if (certs == null || certs.Count <= 0)
                throw new FileNotFoundException("count not find the selected request, please try again");

            var cert = certs.First();

            cert.Description = model.Description;
            cert.Brief = model.Brief;

            if (mfile != null && mfile.Length > 0)
            {
                if (!(mfile.ContentType.Contains("pdf") || mfile.ContentType.Contains("image")))
                    throw new InvalidOperationException($"The select file - {mfile.ContentType} is invalid");

                string filename = Tools.GenerateFileName($"{Tools.Now:yyMMddHHmmss} request", mfile.FileName.Split('.').Last());
                await Tools.UploadFileAsync(mfile, filename, blobfolder.certs);

                cert.AuthLetterFileName = filename;
            }

            await _service.Data.UpdateAsync(cert);

            TempData["success"] = "Your request was updated";

            RedirectToAction(nameof(ViewCertRequest), new { code = cert.Code });
        }
        catch (Exception ex)
        {
            TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
        }
        return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
    }

    [HttpGet("ecert/details/{code}")]
    public async Task<IActionResult> ViewCertRequest(string code)
    {
        try
        {
            return View(await _service.Data.Get<ECertRequest>(x => x.Code.ToLower() == code.ToLower()));
        }
        catch (Exception ex)
        {
            _logger.LogError(Clear.Tools.GetAllExceptionMessage(ex));
            TempData["error"] = $"Could not view request #{code}";
        }
        return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
    }

    [HttpPost("ecert/add-holder")]
    public async Task<IActionResult> AddCertRequestHolder(CertRequestHolderModel model, IFormFile photofile, IFormFile idfile, IFormFile signfile)
    {
        try
        {
            if (!idfile.ContentType.Contains("image"))
                throw new InvalidOperationException($"The select id file - {idfile.ContentType} is invalid");

            if (!signfile.ContentType.Contains("image"))
                throw new InvalidOperationException($"The select signature file - {signfile.ContentType} is invalid");

            var user = await _service.Data.Get<User>(x => x.UserName.ToLower() == User.Identity.Name.ToLower());
            var cert = await _service.Data.Get<ECertRequest>(x => x.Id == model.RequestId);

            var holder = new ECertHolder
            {
                Date = Tools.Now,
                FullName = model.FullName,
                Signature = Tools.GetBase64String(signfile),
                IdFileName = Clear.Tools.StringUtility.GenerateFileName($"{cert.Code} {model.FullName} id", idfile.FileName.Split(".").Last()),
                PhotoFileName = Clear.Tools.StringUtility.GenerateFileName($"{cert.Code} {model.FullName} photo", photofile.FileName.Split(".").Last())
            };

            await Tools.UploadFileAsync(photofile, holder.PhotoFileName, blobfolder.certs);
            await Tools.UploadFileAsync(idfile, holder.IdFileName, blobfolder.certs);

            string key = "RegisterId";
            var reqs = Request.Form.Where(x => x.Key.Contains(key)).ToDictionary(a => a.Key, b => b.Value.ToString());

            foreach (var req in reqs)
            {
                holder.ECertHoldings.Add(new ECertHolding
                {
                    Date = Tools.Now,
                    RegisterId = Convert.ToInt32(req.Value),
                    AccountNo = Request.Form[req.Key.Replace(key, "AccountNo")],
                    ClearingNo = Request.Form[req.Key.Replace(key, "ClearingNo")],
                    CertificateNo = Request.Form[req.Key.Replace(key, "CertificateNo")],
                    Units = Convert.ToDecimal(Request.Form[req.Key.Replace(key, "Units")])
                });
            }

            cert.ECertHolders.Add(holder);

            await _service.Data.UpdateAsync(cert);

            TempData["success"] = $"{model.FullName} has been added to request {cert.Code}";
        }
        catch (Exception ex)
        {
            TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
        }
        return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
    }

    #endregion

    #region profile

    [Route("profile")]
    public async Task<IActionResult> Profile() => View(new UserModel(
        await _service.Data.Get<User>(x => x.UserName.ToLower() == User.Identity.Name.ToLower())));

    [Route("profile/update")]
    public async Task<IActionResult> UpdateProfile(UserModel model)
    {
        try
        {
            var user = await _service.Data.Get<User>(x => x.UserName.ToLower() == User.Identity.Name.ToLower());

            user.FullName = model.FullName.Trim();
            //user.UserName = model.Email.Trim();
            //user.Email = model.Email.Trim();
            user.PhoneNumber = model.MobileNo.Trim();
            user.StockBroker.Street = model.Street.Trim();
            user.StockBroker.City = model.City.Trim();
            user.StockBroker.State = model.State.Trim();
            //user.StockBroker.Country = model.Country.Trim();
            user.StockBroker.SecondaryPhone = model.SecondaryPhone.Trim();
            user.StockBroker.Fax = model.Fax.Trim();

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