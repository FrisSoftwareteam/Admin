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
    [Route("tickets")]
    [Authorize()]
    public class TicketsController : Controller
    {
        private readonly ILogger<TicketsController> _logger;
        private readonly Service _service;

        private readonly ImageSize _imgSize = new(300, 300);
        public TicketsController(ILogger<TicketsController> logger, Service service)
        {
            _logger = logger;
            _service = service;
        }

        public async Task<IActionResult> Index() => View(await _service.Data.Find<Ticket>(x => !x.Resolved));

        //[HttpGet("tickets")]
        //public async Task<IActionResult> Tickets()
        //{
        //    try
        //    {
        //        var user = await _service.Data.Get<User>(x => x.UserName.ToLower() == User.Identity.Name.ToLower());
        //        return View(await _service.Data.Find<Ticket>(x => x.UserId == user.Id));
        //    }
        //    catch (Exception ex)
        //    {
        //        TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
        //        return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
        //    }
        //}

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
                TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
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
                TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
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
                //if (tickets.First().UserId != user.Id) throw new InvalidOperationException($"You cannot close this ticket");
                tickets.First().Resolved = true;
                await _service.Data.UpdateAsync(tickets.First());

                try { await _service.Email.SendTicketClosedEmailAsync(tickets.First()); } catch { }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
            return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
            }
        }
    }
}