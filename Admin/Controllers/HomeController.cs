using FirstReg.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirstReg.Admin.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly Service _service;

        public HomeController(ILogger<HomeController> logger, Service service)
        {
            _logger = logger;
            _service = service;
        }

        public async Task<IActionResult> Index() => View(new DashboardModel
        {
            Shareholders = _service.Data.Count<User>(x => x.Type == UserType.Shareholder),
            StockBrokers = _service.Data.Count<User>(x => x.Type == UserType.StockBroker),
            CompanySecs = _service.Data.Count<User>(x => x.Type == UserType.CompanySec),
            FRAdmins = _service.Data.Count<User>(x => x.Type == UserType.FRAdmin),
            SystemAdmins = _service.Data.Count<User>(x => x.Type == UserType.SystemAdmin),
            MonthlySales = new List<decimal> { 237872, 89093, 93023, 02389, 993042, 94022 },
            CertRequests = (await _service.Data.Get<ECertRequest>()).OrderByDescending(x => x.Date).Take(5)
                                .Select(x => new DashCert(x.Description, x.StockBroker.User.FullName, Clear.Tools.StringUtility.TimeSince(x.Date))).ToList(),
            Users = (await _service.Data.Get<User>()).OrderByDescending(x => x.Id).Take(5)
                                .Select(x => new DashUser(x.FullName, x.Email, x.Type)).ToList(),
            ThisMonth = Tools.Shorten((double)75000),
            Percentage = 28,
            Active = 783,
            Inactive = 263,
            Never = 192
        });

        [HttpGet("logs")]
        public IActionResult AuditLogs() => View("AuditLogs", Url.Action(nameof(GetAuditLogs)));

        #region spirit

        [HttpGet("list")]
        public async Task<IActionResult> GetAuditLogs()
        {
            try
            {
                StringBuilder sb = new();

                var logs = await _service.Data.FromSql<AuditLogView>("""
                    SELECT 
                        L.Id, L.Date, L.Section, L.Type, L.UserId, U.Type AS UserType, 
                        U.FullName, U.UserName, L.Description
                    FROM AuditLogs AS L INNER JOIN AspNetUsers AS U ON L.Id = U.Id
                    WHERE L.Id > 0
                    ORDER BY Date DESC
                    """
                );

                return Ok(new
                {
                    data = logs.Select(x => new[]
                    {
                        x.Date.ToString("dd-MMM-yyy<br/>HH:mm:ss"),
                        x.Description,
                        x.Section.ToString(),
                        x.Type.ToString(),
                        x.UserName,
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
                return StatusCode(StatusCodes.Status500InternalServerError, Clear.Tools.GetAllExceptionMessage(ex));
            }
        }

        #endregion

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(
            new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier }
        );
    }
}
