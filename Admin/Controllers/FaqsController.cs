using FirstReg.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace FirstReg.Admin.Controllers
{
    [Route("faqs")]
    [Authorize]
    public class FaqsController : Controller
    {
        private readonly ILogger<FaqsController> _logger;
        private readonly Service _service;

        public FaqsController(ILogger<FaqsController> logger, Service service)
        {
            _logger = logger;
            _service = service;
        }

        #region faqs

        public async Task<IActionResult> Index() => View(await _service.Data.Get<Faq>());

        [HttpPost("update/{id?}")]
        public async Task<IActionResult> Update(FaqPageModel model, int id)
        {
            try
            {
                if (model.Id > 0)
                {
                    var faq = await _service.Data.Get<Faq>(x => x.Id == model.Id);

                    faq.Question = model.Question;
                    faq.SectionId = model.SectionId;
                    faq.Html = model.Html;

                    await _service.Data.UpdateAsync(faq);
                }
                else
                    await _service.Data.SaveAsync<Faq>(model);

                TempData["success"] = "Successful";
            }
            catch (Exception ex)
            {
                TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
                _logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
            }

            return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
        }

        [Route("delete/{id}")]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            try
            {
                if (id == 0)
                    throw new InvalidOperationException("You have not selected any faq to be deleted");

                var faqs = await _service.Data.Find<Faq>(x => x.Id == id);

                if (faqs.Count <= 0)
                    return NotFound("The faq does not exist");

                var faq = faqs.First();

                await _service.Data.DeleteAsync(faq);

                TempData["success"] = "The post was successfully deleted";
            }
            catch (Exception ex)
            {
                TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
            }

            return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
        }

        #endregion

        #region sections

        [Route("sections")]
        public async Task<IActionResult> Sections() => View(await _service.Data.Get<FaqSection>());

        [HttpPost("sections/update")]
        public async Task<IActionResult> UpdateSection(FaqSection model)
        {
            try
            {
                if (model.Id > 0) await _service.Data.UpdateAsync(model);
                else await _service.Data.SaveAsync(model);

                TempData["success"] = "Successful";
            }
            catch (Exception ex)
            {
                TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
            }

            return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
        }

        [Route("delete/{id}")]
        public async Task<IActionResult> DeleteSection(int id)
        {
            try
            {
                if (id == 0)
                    throw new InvalidOperationException("You have not selected any section to be deleted");

                var sections = await _service.Data.Find<FaqSection>(x => x.Id == id);

                if (sections.Count <= 0)
                    return NotFound("The section does not exist");

                var section = sections.First();

                await _service.Data.DeleteAsync<FaqSection>(section);

                TempData["success"] = "Successful";
            }
            catch (Exception ex)
            {
                TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
            }

            return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
        }

        #endregion
    }
}