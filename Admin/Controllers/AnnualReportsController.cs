using FirstReg.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FirstReg.Admin.Controllers
{
    [Route("annual-reports")]
    [Authorize(Roles = Tools.AdminRole)]
    public class AnnualReportsController : BaseController<AnnualReportsController>
    {
        public AnnualReportsController(ILogger<AnnualReportsController> logger, Service service) : base(logger, service) { }

        public async Task<IActionResult> Index() => View(await _service.Data.Get<AnnualReport>());

        [HttpPost("update")]
        public async Task<IActionResult> Update(AnnualReport model, IFormFile mfile, int id)
        {
            try
            {
                var code = Clear.Tools.StringUtility.GenerateUrlKey(DateTime.Now.ToString());

                if (model.Id > 0)
                {
                    var report = await _service.Data.Get<AnnualReport>(x => x.Id == model.Id);

                    report.Description = model.Description;

                    if (mfile != null && mfile.Length > 0)
                    {
                        var oldFilename = report.FileName;

                        report.FileName = Tools.GenerateFileName(model.Description, mfile.FileName.Split('.').Last());
                        await Tools.UploadFileAsync(mfile, report.FileName, blobfolder.reports);

                        Clear.Tools.FileManager.DeleteFromAzure(
                            Tools.BlobConnectionString, Tools.BlobContainerName, oldFilename,
                            Tools.GetUploadPath(blobfolder.posts));
                    }

                    await _service.Data.UpdateAsync(report);
                }
                else
                {
                    if (mfile == null || mfile.Length <= 0)
                        throw new ArgumentNullException("Please upload an image for this post");

                    model.FileName = Tools.GenerateFileName(model.Description, mfile.FileName.Split('.').Last());
                    await Tools.UploadFileAsync(mfile, model.FileName, blobfolder.reports);

                    await _service.Data.SaveAsync(model);
                }

                TempData["success"] = "Successful";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
                _logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
            }

            return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
        }

        [Route("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (id == 0)
                    throw new InvalidOperationException("You have not selected any report to be deleted");

                var reports = await _service.Data.Find<AnnualReport>(x => x.Id == id);

                if (reports.Count <= 0)
                    return NotFound("The report does not exist");

                var report = reports.First();

                await _service.Data.DeleteAsync(report);

                Clear.Tools.FileManager.DeleteFromAzure(
                    Tools.BlobConnectionString, Tools.BlobContainerName, report.FileName,
                    Tools.GetUploadPath(blobfolder.posts));

                TempData["success"] = "The report was successfully deleted";
            }
            catch (Exception ex)
            {
                TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
            }

            return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
        }
    }
}