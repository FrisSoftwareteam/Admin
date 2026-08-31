using FirstReg.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        public async Task<IActionResult> Index()
        {
            var now = Tools.Now;
            var activeShareholders = _service.Data.Count<Shareholder>(x =>
                !x.Hidden && x.ExpiryDate != null && x.ExpiryDate > now);
            var inactiveShareholders = _service.Data.Count<Shareholder>(x =>
                !x.Hidden && x.ExpiryDate != null && x.ExpiryDate <= now);
            var neverShareholders = _service.Data.Count<Shareholder>(x =>
                !x.Hidden && x.ExpiryDate == null);
            var activeBrokers = _service.Data.Count<StockBroker>(x =>
                x.ExpiryDate != null && x.ExpiryDate > now);
            var inactiveBrokers = _service.Data.Count<StockBroker>(x =>
                x.ExpiryDate != null && x.ExpiryDate <= now);
            var neverBrokers = _service.Data.Count<StockBroker>(x =>
                x.ExpiryDate == null);

            var thisMonthStart = new DateTime(now.Year, now.Month, 1);
            var sixMonthStart = thisMonthStart.AddMonths(-5);
            var salesRows = await _service.Data.GetAsQueryable<Payment>()
                .Where(p => p.Status == PaymentStatus.successful
                            && p.Item == PaymentItem.Subscription
                            && p.Updated >= sixMonthStart)
                .GroupBy(p => new { p.Updated.Year, p.Updated.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(p => p.Amount) })
                .ToListAsync();

            var monthlySales = new List<decimal>();
            var monthlySalesLabels = new List<string>();
            for (var i = 5; i >= 0; i--)
            {
                var month = thisMonthStart.AddMonths(-i);
                monthlySalesLabels.Add(month.ToString("MMM"));
                var row = salesRows.FirstOrDefault(x => x.Year == month.Year && x.Month == month.Month);
                monthlySales.Add(row?.Total ?? 0m);
            }

            var thisMonthAmount = monthlySales.Count > 0 ? monthlySales[^1] : 0m;
            var lastMonthAmount = monthlySales.Count > 1 ? monthlySales[^2] : 0m;
            var percentage = lastMonthAmount == 0
                ? (thisMonthAmount > 0 ? 100 : 0)
                : (int)Math.Round((double)((thisMonthAmount - lastMonthAmount) / lastMonthAmount * 100m));

            var recentCerts = await _service.Data.GetAsQueryable<ECertRequest>()
                .Include(x => x.StockBroker)
                    .ThenInclude(x => x.User)
                .OrderByDescending(x => x.Date)
                .Take(5)
                .ToListAsync();
            var recentUsers = await _service.Data.GetAsQueryable<User>()
                .OrderByDescending(x => x.Id)
                .Take(5)
                .ToListAsync();

            return View(new DashboardModel
            {
                Shareholders = _service.Data.Count<User>(x => x.Type == UserType.Shareholder),
                StockBrokers = _service.Data.Count<User>(x => x.Type == UserType.StockBroker),
                CompanySecs = _service.Data.Count<User>(x => x.Type == UserType.CompanySec),
                FRAdmins = _service.Data.Count<User>(x => x.Type == UserType.FRAdmin),
                SystemAdmins = _service.Data.Count<User>(x => x.Type == UserType.SystemAdmin),
                MonthlySales = monthlySales,
                MonthlySalesLabels = monthlySalesLabels,
                CertRequests = recentCerts
                    .Select(x => new DashCert(x.Description, x.StockBroker?.User?.FullName, Clear.Tools.StringUtility.TimeSince(x.Date))).ToList(),
                Users = recentUsers
                    .Select(x => new DashUser(x.FullName, x.Email, x.Type)).ToList(),
                ThisMonth = Tools.Shorten((double)thisMonthAmount),
                Percentage = percentage,
                Active = activeShareholders + activeBrokers,
                Inactive = inactiveShareholders + inactiveBrokers,
                Never = neverShareholders + neverBrokers
            });
        }

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
