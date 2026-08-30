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
    [Authorize]
    [Route("holdings")]
    public class ShareHoldingsController : Controller
    {
        private readonly ILogger<ShareHoldingsController> _logger;
        private readonly Service _service;

        public ShareHoldingsController(ILogger<ShareHoldingsController> logger, Service service)
        {
            _logger = logger;
            _service = service;
        }

        public async Task<IActionResult> Index() =>
            View(await _service.Data.Get<ShareHolding>());

        [Route("pending")]
        public async Task<IActionResult> Pending() =>
            View(await _service.Data.Find<ShareHolding>(x => x.Status == ShareHoldingStatus.Pending));

        [HttpPost("approve")]
        public async Task<IActionResult> Approve(int Id, int Value, int Units)
        {
            try
            {
                var shareHolding = await _service.Data.Get<ShareHolding>(x => x.Id == Id);

                //shareHolding.AccountName = Name;
                shareHolding.Value = Value;
                shareHolding.Units = Units;
                shareHolding.Status = ShareHoldingStatus.Verified;

                await _service.Data.UpdateAsync(shareHolding);

                TempData["success"] = "Account was approved";
            }
            catch (Exception ex)
            {
                TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
            }

            return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
        }
    }
}
