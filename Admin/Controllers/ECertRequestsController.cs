using FirstReg.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FirstReg.Admin.Controllers
{
    [Authorize]
    [Route("e-cert")]
    public class ECertRequestsController : Controller
    {
        private readonly ILogger<ECertRequestsController> _logger;
        private readonly Service _service;

        public ECertRequestsController(ILogger<ECertRequestsController> logger, Service service)
        {
            _logger = logger;
            _service = service;
        }

        public async Task<IActionResult> Index() =>
            View(await _service.Data.Get<ECertRequest>());

        [Route("pending")]
        public async Task<IActionResult> Pending() =>
            View(await _service.Data.Find<ECertRequest>(x => x.Status == ECertStatus.Pending || x.Status == ECertStatus.Downloaded));


        [HttpGet("details/{code}")]
        public async Task<IActionResult> Details(string code)
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

        [HttpPost("download")]
        public async Task<IActionResult> Download(int Id)
        {
            try
            {
                var request = await _service.Data.Get<ECertRequest>(x => x.Id == Id);

                //////Save and Launch
                ////using MemoryStream stream = new();

                ////await Clear.Tools.FileManager.DownloadFromAzureAsync(
                ////    Tools.BlobConnectionString, Tools.BlobContainerName, request.AuthLetter, stream, Tools.GetUploadPath(blobfolder.certs));

                ////byte[] bytesInStream = stream.ToArray(); // simpler way of converting to array
                ////stream.Close();

                ////Response.Clear();
                ////Response.ContentType = "application/force-download";
                ////Response.Headers.Add("content-disposition", $"attachment;    filename={request.AuthLetter}");
                ////await Response.BodyWriter.WriteAsync(bytesInStream);
                ////await Response.CompleteAsync();

                ////request.Status = ECertStatus.Downloaded;
                ////await _service.Data.UpdateAsync(request);

                TempData["success"] = "The request was successfully downloaded";
            }
            catch (Exception ex)
            {
                TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
            }

            return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
        }

        [HttpPost("complete")]
        public async Task<IActionResult> Complete(int Id, string comments, IFormFile mfile)
        {
            try
            {
                var request = await _service.Data.Get<ECertRequest>(x => x.Id == Id);

                if (mfile == null || mfile.Length <= 0)
                    throw new InvalidOperationException("Please add a file to be uploaded for processing");

                string filename = Tools.GenerateFileName($"{Tools.Now:yyMMddHHmmss} request completed", mfile.FileName.Split('.').Last());

                using MemoryStream stream = new() { Position = 0 };
                await mfile.CopyToAsync(stream);
                stream.Position = 0;

                await Clear.Tools.FileManager.UploadToAzureAsync(
                    Tools.BlobConnectionString, Tools.BlobContainerName, stream, mfile.ContentType,
                    filename, Tools.GetUploadPath(blobfolder.certs));

                //request.Status = ECertStatus.Completed;
                //request.CompletedECertRequest = new()
                //{
                //    Date = Tools.Now,
                //    Comments = comments,
                //    FileName = filename
                //};

                await _service.Data.UpdateAsync(request);

                TempData["success"] = "The request was completed successfully";
            }
            catch (Exception ex)
            {
                TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
            }

            return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
        }
    }
}