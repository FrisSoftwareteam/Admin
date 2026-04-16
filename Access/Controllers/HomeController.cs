using FirstReg.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace FirstReg.OnlineAccess.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly Service _service;
    private readonly PaymentSettings _paystackSetting = Tools.PaymentSettings;

    public HomeController(ILogger<HomeController> logger, Service services)
    {
        _logger = logger;
        _service = services;
    }

    public async Task<IActionResult> Index() => RedirectToAction("Index",
        (await _service.Data.Get<User>(x => x.UserName.ToLower() == User.Identity.Name.ToLower())).Type.ToString());


    #region subscription

    [HttpGet("subscribe")]
    public async Task<IActionResult> Subscribe()
    {
        try
        {
            User user = await _service.Data.Get<User>(x => x.UserName == User.Identity.Name);

            if (user.Type == UserType.Shareholder)
                return RedirectToAction("Subscribe", user.Type.ToString());

            var plans = await _service.Data.Get<SubscriptionPlan>();

            return base.View(new SubscribeModel
            {
                User = user,
                Amount = user.GetSubscriptionPrice(plans),
                Reference = Clear.Tools.StringUtility.GetDateCode(),
                PaymentSettings = _paystackSetting,
                AccountIds = new() { user.StockBroker.Id }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
            TempData["error"] = ex.Message;
            return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
        }
    }

    [HttpGet("subscribing/{txnref}")]
    public async Task<IActionResult> Subscribing(string txnref)
    {
        try
        {
            DateTime cdate = Tools.Now;

            var user = await _service.Data.Get<User>(x => x.UserName == User.Identity.Name);

            if (user.Type == UserType.Shareholder)
                return RedirectToAction("Subscribing", user.Type.ToString(), new { txnref });

            var plans = await _service.Data.Get<SubscriptionPlan>();

            var payment = await Tools.GetPayStack(txnref, cdate, user);
            payment.Description = $"New subscription for {User.Identity.Name}";
            var years = Convert.ToInt32(payment.PayStackResponse.GetCustomData(CustomField.years));

            if (payment.Status == PaymentStatus.successful)
            {
                if (payment.Amount == user.GetSubscriptionPrice(plans))
                {
                    switch (user.Type)
                    {
                        case UserType.StockBroker:
                            user.StockBroker.StartDate = user.StockBroker.ExpiryDate > cdate ? cdate.Date : user.StockBroker.StartDate;
                            user.StockBroker.ExpiryDate = user.StockBroker.ExpiryDate > cdate ? ((DateTime)user.StockBroker.ExpiryDate).AddYears(years) : cdate.AddYears(years);

                            user.Subscriptions.Add(new Subscription
                            {
                                Code = payment.Id,
                                Date = cdate,
                                StartDate = (DateTime)user.StockBroker.StartDate,
                                EndDate = (DateTime)user.StockBroker.ExpiryDate,
                                AmountPaid = payment.Amount,
                                Type = SubscriptionType.StockBroker,
                                PaymentType = PaymentType.Online
                            });

                            break;
                    }
                }
                else
                {
                    payment.Status = PaymentStatus.failed;
                    payment.Remarks = "The amount approved on the gateway is different from the amount requested";
                }
            }

            user.Payments.Add(payment);
            await _service.Data.UpdateAsync(user);

            if (payment.Status == PaymentStatus.successful)
            {
                try
                {
                    await _service.Email.SendSubscrptionEmailAsync(payment);
                }
                catch
                {
                    TempData["error"] = $"Email could not be sent";
                }

                return RedirectToAction(nameof(Subscribed), new { txnref });
            }
            else
            {
                try
                {
                    await _service.Email.SendFailedEmailAsync(payment);
                }
                catch
                {
                    TempData["error"] = $"Email could not be sent";
                }

                return RedirectToAction("failed");
            }
        }
        catch (Exception ex)
        {
            TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
        }
        return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
    }

    [HttpGet("subscribed/{txnref}")]
    public async Task<IActionResult> Subscribed(string txnref)
    {
        try
        {
            var payments = await _service.Data.Find<Payment>(x => x.Id == txnref);

            if (payments.Count <= 0)
                throw new InvalidOperationException($"Payment #{txnref} was not found, please try again.");

            return View(payments.First());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
            TempData["error"] = ex.Message;
            return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
        }
    }

    [HttpGet("subscriptions")]
    public async Task<IActionResult> Subscriptions()
    {
        try
        {
            User user = await _service.Data.Get<User>(x => x.UserName == User.Identity.Name);
            return View(await _service.Data.Find<Payment>(
                x => x.UserId == user.Id && x.Item == PaymentItem.Subscription));
        }
        catch
        {
            return View(new List<Payment>());
        }
    }

    [HttpGet("payments")]
    public async Task<IActionResult> Payments()
    {
        try
        {
            User user = await _service.Data.Get<User>(x => x.UserName == User.Identity.Name);
            return View(await _service.Data.Get<Payment>(x => x.UserId == user.Id));
        }
        catch
        {
            return View(new List<Payment>());
        }
    }

    #endregion

    #region registers

    [Route("/registers")]
    public async Task<IActionResult> Registers()
    {
        try
        {
            var user = await _service.Data.Get<User>(x => x.UserName.ToLower() == User.Identity.Name);

            switch (user.Type)
            {
                case UserType.Shareholder:
                    if (!user.Shareholders.Any(x => x.Verified && x.IsSubscribed))
                        return RedirectToAction(nameof(ShareholderController.Subscribe));
                    break;
                case UserType.StockBroker:
                    if (!user.StockBroker.IsSubscribed)
                        return RedirectToAction(nameof(Subscribe));
                    break;
            }

            return View(await _service.Data.Get<Register>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
            TempData["error"] = ex.Message;
            return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
        }
    }

    [Route("/registers/list")]
    public async Task<IActionResult> RegisterList() => Ok(
        (await _service.Data.Get<Register>()).Select(x => new { x.Id, x.Code, x.Name, x.Symbol }));

    [Route("/register/{code}")]
    public async Task<IActionResult> Register(int code)
    {
        try
        {
            var registers = await _service.Data.Find<Register>(x => x.Id == code);

            if (registers.Count <= 0)
                throw new InvalidOperationException("The selected register was not found, please try again");

            return View(new ComapnyModel(registers.First()));
        }
        catch (Exception ex)
        {
            TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
            return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
        }
    }

    #endregion

    #region shareholders

    [Route("/shareholders")]
    public async Task<IActionResult> Shareholders()
    {
        try
        {
            var user = await _service.Data.Get<User>(x => x.UserName.ToLower() == User.Identity.Name);
            if (user.Type != UserType.FRAdmin)
            {
                TempData["error"] = "You are not authorized to see this page";
                return RedirectToAction(nameof(Index));
            }

            return View(await _service.Data.Get<Shareholder>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
            TempData["error"] = ex.Message;
            return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
        }
    }

    [Route("/shareholders/{code}")]
    public async Task<IActionResult> Shareholder(int code)
    {
        try
        {
            var shs = await _service.Data.Find<Shareholder>(x => x.Id == code);

            if (shs.Count <= 0)
                throw new InvalidOperationException("The selected register was not found, please try again");

            return View(shs.First());
        }
        catch (Exception ex)
        {
            TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
            return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
        }
    }

    #endregion

    #region profile

    [Route("profile")]
    public async Task<IActionResult> Profile() => RedirectToAction("Profile",
        (await _service.Data.Get<User>(x => x.UserName == User.Identity.Name)).Type.ToString());

    [HttpGet("password")]
    public IActionResult Password() => View();

    #endregion

    #region support

    [HttpGet("tickets")]
    public async Task<IActionResult> Tickets()
    {
        try
        {
            var user = await _service.Data.Get<User>(x => x.UserName.ToLower() == User.Identity.Name.ToLower());
            return View(await _service.Data.Find<Ticket>(x => x.UserId == user.Id));
        }
        catch (Exception ex)
        {
            TempData["error"] = ex.Message;
            return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
        }
    }

    [HttpPost("tickets")]
    public async Task<IActionResult> AddTickets(TicketModel model)
    {
        try
        {
            var user = await _service.Data.Get<User>(x => x.UserName.ToLower() == User.Identity.Name.ToLower());

            Ticket ticket = new()
            {
                Code = Clear.Tools.StringUtility.GetDateCode(),
                Subject = model.Subject,
                UserId = user.Id,
                Date = Tools.Now
            };

            ticket.Messages.Add(new()
            {
                Body = Clear.Tools.StringUtility.CreateParagraphsFromReturns(model.Message),
                Code = ticket.Code,
                Date = ticket.Date,
                UserId = user.Id
            });

            await _service.Data.SaveAsync(ticket);

            try { await _service.Email.SendTicketEmailAsync(user.Email, user.FullName, ticket); } catch { }

            return RedirectToAction(nameof(Ticket), new { code = ticket.Code });
        }
        catch (Exception ex)
        {
            TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
            return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
        }
    }

    [HttpPost("tickets/add-message")]
    public async Task<IActionResult> AddMessage(MessageModel model)
    {
        try
        {
            var user = await _service.Data.Get<User>(x => x.UserName.ToLower() == User.Identity.Name.ToLower());
            var tickets = await _service.Data.Find<Ticket>(x => x.Id == model.TicketId);

            if (!tickets.Any())
                throw new InvalidOperationException($"The selected ticket with the code was not found");

            if (tickets.First().Resolved)
                throw new InvalidOperationException($"You can't add any more message to ticket: {tickets.First().Code} because it's closed.");

            tickets.First().Messages.Add(new()
            {
                Body = Clear.Tools.StringUtility.CreateParagraphsFromReturns(model.Message),
                Code = Clear.Tools.StringUtility.GetDateCode(),
                Date = Tools.Now,
                UserId = user.Id
            });

            await _service.Data.UpdateAsync(tickets.First());

            try { await _service.Email.SendTicketReplyEmailAsync(tickets.First()); } catch { }
        }
        catch (Exception ex)
        {
            TempData["error"] = ex.Message;
        }
        return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
    }

    [HttpGet("ticket/{code}")]
    public async Task<IActionResult> Ticket(string code)
    {
        try
        {
            var tickets = await _service.Data.Find<Ticket>(x => x.Code == code);
            if (!tickets.Any()) throw new InvalidOperationException($"The ticket with the reference - {code} was not found");
            await _service.Data.ExecuteSql($"UPDATE Messages SET IsRead = 1 WHERE TicketId = {tickets.First().Id}");
            return View(tickets.First());
        }
        catch (Exception ex)
        {
            TempData["error"] = ex.Message;
            return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
        }
    }

    [Route("ticket/{code}/close")]
    public async Task<IActionResult> CloseTicket(string code)
    {
        try
        {
            var user = await _service.Data.Get<User>(x => x.UserName.ToLower() == User.Identity.Name.ToLower());
            var tickets = await _service.Data.Find<Ticket>(x => x.Code == code);
            if (!tickets.Any()) throw new InvalidOperationException($"The ticket with the reference - {code} was not found");
            if (tickets.First().UserId != user.Id) throw new InvalidOperationException($"You cannot close this ticket");
            tickets.First().Resolved = true;
            await _service.Data.UpdateAsync(tickets.First());

            try { await _service.Email.SendTicketClosedEmailAsync(tickets.First()); } catch { }
        }
        catch (Exception ex)
        {
            TempData["error"] = ex.Message;
        }
        return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
    }

    #endregion

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
