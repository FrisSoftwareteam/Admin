using FirstReg.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirstReg.Admin.Controllers
{
    [Route("payments")]
    [Authorize()]
    public class PaymentsController : Controller
    {
        private readonly ILogger<PaymentsController> _logger;
        private readonly Service _service;

        public PaymentsController(ILogger<PaymentsController> logger, Service service)
        {
            _logger = logger;
            _service = service;
        }

        public IActionResult Index() => View("List", new string[]
        {
            Url.Action(nameof(GetLists)),
            Url.Action(nameof(Confirm)),
            Url.Action(nameof(Cancel))
        });

        [HttpGet("pending")]
        public IActionResult Pending() => View("List", new string[]
        {
            Url.Action(nameof(GetLists), new { s = PaymentStatus.pending }),
            Url.Action(nameof(Confirm)),
            Url.Action(nameof(Cancel))
        });

        [HttpGet("online")]
        public IActionResult Online() => View("List", new string[]
        {
            Url.Action(nameof(GetLists), new { g = PaymentGateway.PayStack }),
            Url.Action(nameof(Confirm)),
            Url.Action(nameof(Cancel))
        });

        [HttpGet("bank")]
        public IActionResult Bank() => View("List", new string[]
        {
            Url.Action(nameof(GetLists), new { g = PaymentGateway.Bank }),
            Url.Action(nameof(Confirm)),
            Url.Action(nameof(Cancel))
        });

        #region spirit

        [HttpGet("list")]
        public async Task<IActionResult> GetLists(PaymentGateway? g, PaymentStatus? s)
        {
            try
            {
                StringBuilder sb = new($"SELECT * FROM Payments WHERE (Id <> '') ");

                if (g != null) sb.Append($"AND (Gateway = {(int)g}) ");
                if (s != null) sb.Append($"AND (Status = {(int)s}) ");

                var payments = await _service.Data.FromSql<Payment>(sb.ToString());

                return Ok(new
                {
                    data = payments.OrderBy(x => x.Description).Select(x => new[]
                    {
                        x.Id,
                        x.Description,
                        x.Amount.ToString("N2"),
                        x.Status.ToString(),
                        x.Gateway.ToString(),
                        x.Date.ToString("dd-MMM-yy HH:mm"),
                        x.Remarks,
                        x.BankPaymentDetails?.User ?? x.User.FullName,
                        x.User.Email,
                        x.User.PhoneNumber,
                        x.Item.ToString(),
                        x.Currency.ToString(),
                        Clear.Tools.StringUtility.TimeAgo(x.Date),
                        Clear.Tools.StringUtility.TimeSince(x.Date),
                        x.CssColorClass,
                        x.PaidTo,
                        x.PayRef,
                        x.NeedsConfimation ? "1" : "0",
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
                return StatusCode(StatusCodes.Status500InternalServerError, Clear.Tools.GetAllExceptionMessage(ex));
            }
        }

        [HttpPut("confirm/{id}")]
        public async Task<IActionResult> Confirm(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                    throw new ArgumentNullException(nameof(id), "Payment id cannot be empty, please select a payment from the list");

                var payment = await _service.Data.Get<Payment>(x => x.Id == id);

                if (payment == null)
                    throw new InvalidOperationException("Could not find the selected payment");

                if (payment.Gateway != PaymentGateway.Bank)
                    throw new InvalidOperationException("You can only confirm a bank payment");

                if (payment.Status != PaymentStatus.pending)
                    throw new InvalidOperationException($"You can only confirm a pending bank payment, the status of this payment is {payment.Status}");

                var cdate = Tools.Now;

                payment.Status = PaymentStatus.successful;
                payment.Updated = cdate;
                payment.Remarks = "confirmed";

                int years = payment.BankPaymentDetails.Years;

                switch (payment.User.Type)
                {
                    case UserType.StockBroker:
                        payment.User.StockBroker.StartDate = payment.User.StockBroker.ExpiryDate > cdate ? cdate.Date : payment.User.StockBroker.StartDate;
                        payment.User.StockBroker.ExpiryDate = payment.User.StockBroker.ExpiryDate > cdate ? ((DateTime)payment.User.StockBroker.ExpiryDate).AddYears(years) : cdate.AddYears(years);

                        payment.User.Subscriptions.Add(new Subscription
                        {
                            Code = payment.Id,
                            Date = cdate,
                            StartDate = (DateTime)payment.User.StockBroker.StartDate,
                            EndDate = (DateTime)payment.User.StockBroker.ExpiryDate,
                            AmountPaid = payment.Amount,
                            Type = SubscriptionType.StockBroker,
                            PaymentType = PaymentType.Bank,
                            Confirmed = true,
                            AccountId = payment.User.StockBroker.Id
                        });

                        break;
                    case UserType.Shareholder:
                        List<int> ids = new();

                        if (!string.IsNullOrEmpty(payment.BankPaymentDetails.AccountIds))
                            ids = payment.BankPaymentDetails.AccountIds.Split(",").Select(x => Convert.ToInt32(x)).ToList();

                        if (ids.Count == 0 && payment.User.Shareholders.Count == 1)
                            ids.Add(payment.User.Shareholders.First().Id);

                        var shs = payment.User.Shareholders.Where(x => ids.Contains(x.Id)).ToList();

                        foreach (var sh in shs)
                        {
                            sh.StartDate = sh.StartDate == null ? cdate : (sh.ExpiryDate > cdate ? cdate.Date : sh.StartDate);
                            sh.ExpiryDate = sh.ExpiryDate > cdate ? ((DateTime)sh.ExpiryDate).AddYears(years) : cdate.AddYears(years);

                            payment.User.Subscriptions.Add(new Subscription
                            {
                                Code = payment.Id,
                                Date = cdate,
                                StartDate = (DateTime)sh.StartDate,
                                EndDate = (DateTime)sh.ExpiryDate,
                                AmountPaid = payment.Amount,
                                Type = SubscriptionType.IndividualShareholder,
                                PaymentType = PaymentType.Bank,
                                Confirmed = true,
                                AccountId = sh.Id
                            });
                        }

                        break;
                }

                await _service.Data.UpdateAsync(payment);

                return Ok(new
                {
                    payment.Id,
                    Status = payment.Status.ToString(),
                    payment.Remarks,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
                return StatusCode(StatusCodes.Status500InternalServerError, Clear.Tools.GetAllExceptionMessage(ex));
            }
        }

        [HttpPut("cancel/{id}")]
        public async Task<IActionResult> Cancel(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                    throw new ArgumentNullException(nameof(id), "Payment id cannot be empty, please select a payment from the list");

                var payment = await _service.Data.Get<Payment>(x => x.Id == id);

                if (payment == null)
                    throw new InvalidOperationException("Could not find the selected payment");

                if (payment.Gateway != PaymentGateway.Bank)
                    throw new InvalidOperationException("You can only cancel a bank payment");

                if (payment.Status != PaymentStatus.pending)
                    throw new InvalidOperationException($"You can only cancel a pending bank payment, the status of this payment is {payment.Status}");

                var cdate = Tools.Now;

                payment.Status = PaymentStatus.failed;
                payment.Updated = cdate;
                payment.Remarks = "failed";

                await _service.Data.UpdateAsync(payment);

                return Ok(new
                {
                    payment.Id,
                    Status = payment.Status.ToString(),
                    payment.Remarks,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
                return StatusCode(StatusCodes.Status500InternalServerError, Clear.Tools.GetAllExceptionMessage(ex));
            }
        }

        #endregion
    }
}