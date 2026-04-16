using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.ExtendedProperties;
using FirstReg.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FirstReg.Admin.Controllers
{
    [Authorize]
    [Route("shareholders")]
    public class ShareHoldersController : Controller
    {
        private readonly ILogger<ShareHoldersController> _logger;
        private readonly Service _service;
        private readonly UserManager<User> _userManager;
        private readonly EStockApiUrl _apiUrl;
        private readonly Mongo _mondgodb;
        private readonly IApiClient _apiClient;

        public ShareHoldersController(ILogger<ShareHoldersController> logger, Service service,
            UserManager<User> userManager, Mongo mondgodb, IApiClient apiClient, EStockApiUrl apiUrl)
        {
            _logger = logger;
            _service = service;
            _userManager = userManager;
            _mondgodb = mondgodb;
            _apiClient = apiClient;
            _apiUrl = apiUrl;
        }

        public IActionResult Index() => View("List", new string[]
        {
            Url.Action(nameof(GetLists)),
            Url.Action(nameof(SwitchGroup)),
        });

        [HttpGet("pending")]
        public IActionResult Pending() => View("List", new string[]
        {
            Url.Action(nameof(GetLists), new { v = false }),
            Url.Action(nameof(SwitchGroup)),
        });

        [Route("expired")]
        public IActionResult Expired() => View("List", new string[]
        {
            Url.Action(nameof(GetLists), new { s = false }),
            Url.Action(nameof(SwitchGroup)),
        });

        [Route("active")]
        public IActionResult Active() => View("List", new string[]
        {
            Url.Action(nameof(GetLists), new { v = true, s = true }),
            Url.Action(nameof(SwitchGroup)),
        });

        [Route("details/{code}")]
        public async Task<IActionResult> Details(string code)
        {
            try
            {
                var hs = await _service.Data.Find<Shareholder>(x => x.Code.ToLower() == code.ToLower());

                if (hs.Count <= 0)
                    throw new InvalidOperationException("Shareholder was not found, please try again.");

                return View(hs.First());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                TempData["error"] = $"Could not retrieve shareholder details: {Clear.Tools.GetAllExceptionMessage(ex)}";
                return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
            }
        }

        [HttpPost("delete-account/{code}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount(string code)
        {
            try
            {
                var hs = await _service.Data.GetAsQueryable<Shareholder>()
                    .Include(x => x.Holdings)
                    .Include(x => x.User)
                    .Where(x => x.Code.ToLower() == code.ToLower())
                    .ToListAsync();

                if (!hs.Any())
                    throw new InvalidOperationException("Shareholder was not found, please try again.");

                var shareholder = hs.First();
                var user = shareholder.User;
                var email = user?.Email;
                var fullName = user?.FullName ?? shareholder.FullName;

                if (!string.IsNullOrWhiteSpace(email))
                {
                    try
                    {
                        await _service.Email.SendAccountDeletedEmailAsync(email, fullName);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogWarning(emailEx, "Account deleted email could not be sent to {Email}", email);
                        TempData["warning"] = "Account deleted, but the notification email could not be sent.";
                    }
                }

                foreach (var holding in shareholder.Holdings.ToList())
                    await _service.Data.DeleteAsync(holding);

                await _service.Data.DeleteAsync(shareholder);

                if (user != null)
                {
                    var hasOtherAccounts = await _service.Data.ExistsAsync<Shareholder>(x =>
                        x.UserId == user.Id && x.Id != shareholder.Id);

                    if (!hasOtherAccounts)
                    {
                        var result = await _userManager.DeleteAsync(user);

                        if (!result.Succeeded)
                            TempData["warning"] = "Shareholder account deleted, but the linked user record could not be removed.";
                    }
                }

                TempData["success"] = $"Shareholder account {code} was deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                TempData["error"] = $"Could not delete shareholder account: {Clear.Tools.GetAllExceptionMessage(ex)}";
                return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
            }
        }

        [Route("create-new")]
        public async Task<IActionResult> Create(ShareHolderModel model)
        {
            try
            {
                var user = new User
                {
                    Type = model.Type,
                    FullName = model.FullName.Trim(),
                    UserName = model.Email.Trim(),
                    Email = model.Email.Trim(),
                    EmailConfirmed = true,
                    PhoneNumber = model.MobileNo.Trim(),
                    PhoneNumberConfirmed = true
                };

                string code = Clear.Tools.StringUtility.GetDateCode();

                user.Shareholders.Add(new()
                {
                    Code = code,
                    FullName = model.FullName.Trim(),
                    Street = model.Street.Trim(),
                    City = model.City.Trim(),
                    State = model.State.Trim(),
                    Country = model.Country.Trim(),
                    Date = Tools.Now,
                    PrimaryPhone = model.MobileNo.Trim(),
                    SecondaryPhone = model.SecondaryPhone?.Trim(),
                    PostCode = model.PostCode.Trim(),
                    ClearingNo = model.ClearingNo,

                    CreatedOn = Tools.Now,

                    Verified = true,
                    VerifiedBy = User.Identity.Name,
                    VerifiedOn = Tools.Now
                });

                var result = await _userManager.CreateAsync(user);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account without password.");
                    TempData["success"] = "Shareholder was successfully created";

                    try
                    {
                        await _service.Email.SendWelcomeEmailAsync(model.Email, model.FullName);
                    }
                    catch
                    {
                        _logger.LogWarning($"Welcome email could not be sent after new account was created for {user.FullName}");
                        TempData["warning"] = $"A welcome email could not be sent to {model.Email}";
                    }

                    return RedirectToAction(nameof(Details), new { code });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                TempData["error"] = $"Could not retrieve shareholder details: {Clear.Tools.GetAllExceptionMessage(ex)}";
            }
            return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
        }

        [Route("subscribe")]
        public async Task<IActionResult> Subscribe(SubscribeModel model)
        {
            try
            {
                var sh = await _service.Data.Get<Shareholder>(x => x.Id == model.ShareholderId);

                var payment = Payment.CreateForSubscription(new BankPayModel
                {
                    Id = Clear.Tools.StringUtility.GetDateCode(),
                    Amount = model.AmountPaid,
                    Date = model.PaymentDate,
                    Years = model.Years,
                    AccountIds = sh.Id.ToString(),
                    Payee = sh.FullName,
                    Reference = model.PayRef,
                }, sh.User, Tools.Now);

                payment.Status = PaymentStatus.successful;
                payment.Updated = Tools.Now;
                payment.Remarks = "confirmed";

                sh.StartDate = sh.StartDate == null ? model.StartDate : (sh.ExpiryDate > model.StartDate ? model.StartDate.Date : sh.StartDate);
                sh.ExpiryDate = sh.ExpiryDate > model.StartDate ? ((DateTime)sh.ExpiryDate).AddYears(model.Years) : model.StartDate.AddYears(model.Years);

                sh.User.Payments.Add(payment);
                sh.User.Subscriptions.Add(new Subscription
                {
                    Code = payment.Id,
                    Date = Tools.Now,
                    StartDate = (DateTime)sh.StartDate,
                    EndDate = (DateTime)sh.ExpiryDate,
                    AmountPaid = payment.Amount,
                    Type = SubscriptionType.IndividualShareholder,
                    PaymentType = PaymentType.Bank
                });

                await _service.Data.UpdateAsync(sh);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                TempData["error"] = $"Could not add subscription: {Clear.Tools.GetAllExceptionMessage(ex)}";
            }

            return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
        }

        [HttpPost("reject")]
        public async Task<IActionResult> Reject(ShareholdersRejectModel model)
        {
            try
            {
                var user = await _service.Data.Get<User>(x => x.UserName.ToLower() == User.Identity.Name.ToLower());

                Shareholder sh = await _service.Data.Get<Shareholder>(x => x.Id == model.Id);

                if (sh.Verified) throw new InvalidOperationException(
                    $"Account cannot be rejected because it's already verified by {sh.VerifiedBy} on {sh.VerifiedOn:dd/MMM/yyy}.");

                Ticket ticket = sh.TicketId > 0
                    ? await _service.Data.Get<Ticket>(x => x.Id == sh.TicketId)
                    : new Ticket
                    {
                        Code = Clear.Tools.StringUtility.GetDateCode(),
                        Subject = $"{sh.FullName} Account Activation",
                        UserId = (int)sh.UserId,
                        Date = Tools.Now
                    };

                ticket.Messages.Add(new()
                {
                    Body = Clear.Tools.StringUtility.CreateParagraphsFromReturns(model.Comments),
                    Code = ticket.Code,
                    Date = ticket.Date,
                    UserId = user.Id
                });

                if (sh.TicketId > 0)
                    await _service.Data.UpdateAsync(ticket);
                else
                {
                    await _service.Data.SaveAsync(ticket);
                    sh.TicketId = ticket.Id;
                }

                try { await _service.Email.SendTicketEmailAsync(user.Email, user.FullName, ticket); } catch { }

                switch (model.Issue)
                {
                    case ShareholderActivationIssue.Signature:
                        sh.Signature = null;
                        break;
                    case ShareholderActivationIssue.ClearingNo:
                        sh.ClearingNo = null;
                        break;
                }

                sh.ActionRequired = true;

                await _service.Data.UpdateAsync(sh);

                return RedirectToAction(nameof(Pending));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                TempData["error"] = $"Could not retrieve shareholder details: {Clear.Tools.GetAllExceptionMessage(ex)}";
            }
            return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
        }

        [HttpPost("activate/{code}")]
        public async Task<IActionResult> Activate(string code, bool IsCompany)
        {
            try
            {
                var hs = await _service.Data.Find<Shareholder>(x => x.Code.ToLower() == code.ToLower());

                if (!hs.Any())
                    throw new InvalidOperationException("Shareholder was not found, please try again.");

                Shareholder sh = hs.First();

                if (!sh.User.EmailConfirmed)
                    throw new InvalidOperationException("Account cannot be activated because user email has not been confirmed, " +
                        "please advice shareholder to validate their email address at-least.");

                if (string.IsNullOrEmpty(sh.Signature))
                    throw new InvalidOperationException("Account cannot be activated because there is no valid signature; " +
                        "Signature must be verified to activate this account.");

                sh.IsCompany = IsCompany;

                sh.Verified = true;
                sh.VerifiedBy = User.Identity.Name;
                sh.VerifiedOn = Tools.Now;

                await _service.Data.UpdateAsync(sh);

                TempData["success"] = $"Account was successfully verified";

                try
                {
                    var regids = (await _service.Data.Get<Register>()).Select(x => x.Id).ToList();

                    // call api
                    sh = await Tools.UpdateAccountDetails(sh, regids, _apiClient, _apiUrl, _mondgodb);
                    await _service.Data.UpdateAsync(sh);
                }
                catch (Exception vex)
                {
                    TempData["error"] = $"Could not retrieve shareholder details from the API:\n{Clear.Tools.GetAllExceptionMessage(vex)}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                TempData["error"] = $"Could not retrieve shareholder details: {Clear.Tools.GetAllExceptionMessage(ex)}";
            }
            return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
        }

        [HttpPost("update/chn/{code}")]
        public async Task<IActionResult> UpdateCHN(string code, string chn)
        {
            try
            {
                var hs = await _service.Data.Find<Shareholder>(x => x.Code.ToLower() == code.ToLower());

                if (!hs.Any())
                    throw new InvalidOperationException("Shareholder was not found, please try again.");

                Shareholder sh = hs.First();

                sh.ClearingNo = chn;
                await _service.Data.UpdateAsync(sh);

                TempData["success"] = $"Clearing number was successfully updated";

                try
                {
                    var regids = (await _service.Data.FromSql<RegisterIdModel>("SELECT Id FROM Registers"));
                    //var regids = (await _service.Data.Get<Register>()).Select(x => x.Id).ToList();

                    // call api
                    sh = await Tools.UpdateAccountDetails(sh, regids.Select(x => x.Id).ToList(), _apiClient, _apiUrl, _mondgodb);
                    await _service.Data.UpdateAsync(sh);
                }
                catch (Exception vex)
                {
                    TempData["error"] = $"Could not retrieve shareholder details from the API:\n{Clear.Tools.GetAllExceptionMessage(vex)}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                TempData["error"] = $"Could not update shareholder details: {Clear.Tools.GetAllExceptionMessage(ex)}";
            }
            return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
        }

        [HttpPost("holding/verify")]
        public async Task<IActionResult> VerifyHolding(int id)
        {
            try
            {
                var hs = await _service.Data.Find<ShareHolding>(x => x.Id == id);

                if (!hs.Any())
                    throw new InvalidOperationException("Shareholder account was not found, please try again.");

                ShareHolding sh = hs.First();

                if (!sh.Shareholder.Verified)
                    throw new InvalidOperationException($"Cannot continue because the shareholder has not been verified.");

                sh.Status = ShareHoldingStatus.Verified;

                await _service.Data.UpdateAsync(sh);

                var regids = (await _service.Data.FromSql<RegisterIdModel>("SELECT Id FROM Registers"));
                    
                // call api
                var model = await Tools.UpdateAccountDetails(sh.Shareholder, regids.Select(x => x.Id).ToList(), _apiClient, _apiUrl, _mondgodb);
                sh.Shareholder.LastUpdate = Tools.Now;

                await _service.Data.UpdateAsync(sh);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                TempData["error"] = $"Could not retrieve shareholder details: {Clear.Tools.GetAllExceptionMessage(ex)}";
            }
            return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
        }

        [HttpPost("holding/delete")]
        [HttpGet("holding/delete/{id}")]
        public async Task<IActionResult> DeleteHolding(int id)
        {
            try
            {
                var hs = await _service.Data.Find<ShareHolding>(x => x.Id == id);

                if (!hs.Any())
                    throw new InvalidOperationException("Shareholder account was not found, please try again.");

                await _service.Data.DeleteAsync(hs.First());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                TempData["error"] = $"Could not retrieve shareholder details: {Clear.Tools.GetAllExceptionMessage(ex)}";
            }
            return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
        }

        #region spirit

        [HttpPost("switch-group")]
        public async Task<ActionResult> SwitchGroup(int id, bool status)
        {
            try
            {
                var sh = await _service.Data.Get<Shareholder>(x => x.Id == id);
                sh.User.AllowGroup = status;
                await _service.Data.UpdateAsync(sh);

                return Ok("Status updated");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
                return StatusCode(StatusCodes.Status500InternalServerError, Clear.Tools.GetAllExceptionMessage(ex));
            }
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetLists(bool? v, bool? s)
        {
            try
            {
                IQueryable<Shareholder> query = _service.Data.GetAsQueryable<Shareholder>()
                    .AsNoTracking()
                    .Include(x => x.User);

                var draw = int.TryParse(Request.Query["draw"], out var drawValue) ? drawValue : 1;
                var start = int.TryParse(Request.Query["start"], out var startValue) ? startValue : 0;
                var length = int.TryParse(Request.Query["length"], out var lengthValue) ? lengthValue : 25;
                var search = Request.Query["search[value]"].ToString().Trim();

                if (v != null)
                {
                    query = query.Where(x => x.Verified == v.Value);
                }

                if (s == true)
                    query = query.Where(x => x.ExpiryDate > Tools.Now);
                else if (s == false)
                    query = query.Where(x => x.ExpiryDate == null || x.ExpiryDate < Tools.Now);

                var recordsTotal = await query.CountAsync();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(x =>
                        x.FullName.Contains(search) ||
                        x.Code.Contains(search) ||
                        x.User.Email.Contains(search) ||
                        (x.PrimaryPhone != null && x.PrimaryPhone.Contains(search)) ||
                        (x.SecondaryPhone != null && x.SecondaryPhone.Contains(search)));
                }

                var recordsFiltered = await query.CountAsync();

                var shs = await query
                    .OrderBy(x => x.FullName)
                    .Skip(start)
                    .Take(length)
                    .Select(x => new
                    {
                        x.FullName,
                        x.Code,
                        Email = x.User.Email,
                        x.PrimaryPhone,
                        x.SecondaryPhone,
                        x.Verified,
                        IsSubscribed = x.ExpiryDate != null && x.ExpiryDate > Tools.Now,
                        x.Id
                    })
                    .ToListAsync();

                return Ok(new
                {
                    draw,
                    recordsTotal,
                    recordsFiltered,
                    data = shs.Select(x => new[]
                    {
                        $"{x.FullName}<br>{x.Code}",
                        $"{x.Email}<br>{$"{x.PrimaryPhone} {x.SecondaryPhone}".Trim()}".Trim(),
                        x.Verified ? "verified" : "pending",
                        x.IsSubscribed ? "active" : "expired",
                        x.Id.ToString(),
                        Clear.Tools.StringUtility.SQLSerialize(x.Verified),
                        Clear.Tools.StringUtility.SQLSerialize(x.IsSubscribed),
                        Url.Action(nameof(Details), new { code = x.Code })
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
                return StatusCode(StatusCodes.Status500InternalServerError, Clear.Tools.GetAllExceptionMessage(ex));
            }
        }

        [HttpGet("list/{code}")]
        public async Task<IActionResult> GetDetails(string code)
        {
            try
            {
                var hs = await _service.Data.Find<Shareholder>(x => x.Code.ToLower() == code.ToLower());

                if (hs.Count <= 0)
                    return NotFound("Shareholder was not found, please try again.");

                var h = hs.First();

                return Ok(new
                {
                    h.Id,
                    h.UserId,
                    h.FullName,
                    h.Code,
                    h.Country,
                    h.User.Email,
                    h.User.PhoneNumber,
                    h.User.UserName,
                    h.ClearingNo,
                    h.Street,
                    h.City,
                    h.CreatedOn,
                    h.Address,
                    h.DaysLeft,
                    h.DaysSpent,
                    h.ExpiryDate,
                    h.IsCompany,
                    h.IsSubscribed,
                    h.State,
                    h.PrimaryPhone,
                    h.SecondaryPhone,
                    h.PostCode,
                    h.Date,
                    h.StartDate,
                    h.Signature,
                    h.Verified,
                    h.VerifiedOn,
                    h.VerifiedBy,
                    h.LegacyAccId,
                    h.LegacyId,
                    h.LegacyUsername,
                    h.MAccessPin,
                    h.CardId,
                    h.Downloaded,
                    h.Percentage,
                    h.Portfolio,
                    h.SecurityCount,
                    h.TotalDays,
                    h.TotalUnit,
                    Holdings = h.Holdings.Select(x => new
                    { x.AccountNo, x.Id, x.AccountName, x.Register.Name, x.RegisterId, x.Units, x.Status, x.Date })
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
