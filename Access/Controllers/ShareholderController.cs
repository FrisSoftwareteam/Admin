using Clear;
using FirstReg.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FirstReg.OnlineAccess.Controllers;

[Authorize]
[Route("sh")]
public class ShareholderController(ILogger<ShareholderController> logger, Service service,
    Mongo db, EStockApiUrl apiUrl, Mongo mondgodb, IApiClient apiClient) : BaseController(service, AuditLogSection.Shareholder)
{
    private readonly PaymentSettings _paystackSetting = Tools.PaymentSettings;

    public async Task<IActionResult> Index()
    {
        var user = await service.Data.Get<User>(x => x.UserName.ToLower() == User.Identity.Name.ToLower());
        if (user.Shareholders.Count == 1)
        {
            if (!user.Shareholders.First().Verified)
                return RedirectToAction(nameof(Activate), new { code = user.Shareholders.First().Code });

            if (!user.Shareholders.First().IsSubscribed)
                return RedirectToAction(nameof(Subscribe));

            var sh = await UpdateDetails(user.Shareholders.First());

            return View(nameof(Details), new ShareholderModel(sh));
        }
        return View(new ShareHolderDashboardModel(user));
    }

    private async Task<Shareholder> UpdateDetails(Shareholder sh)
    {
        try
        {
            if (sh.LastUpdate is null || sh.LastUpdate < Tools.Now.Date)
            {
                var regids = (await service.Data.Get<Register>()).Select(x => x.Id).ToList();
                sh = await Tools.UpdateAccountDetails(sh, regids, apiClient, apiUrl, mondgodb);
                if (sh.Verified) await service.Data.UpdateAsync(sh);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update shareholder details from API for {ClearingNo}", sh.ClearingNo);
            TempData["error"] = "Some portfolio data could not be refreshed. Showing last known data.";
        }
        return sh;
    }

    [Route("details/{code}")]
    public async Task<IActionResult> Details(string code)
    {
        try
        {
            var shs = await service.Data.Find<Shareholder>(x => x.Code == code);

            if (!shs.Any()) throw new InvalidOperationException("Shareholder account not found, please try again");

            if (!shs.First().Verified)
                return RedirectToAction(nameof(Activate), new { code });

            if (!shs.First().IsSubscribed)
                return RedirectToAction(nameof(Subscribe));

            var sh = await UpdateDetails(shs.First());

            return View(new ShareholderModel(sh));
        }
        catch (Exception ex)
        {
            TempData["error"] = ex.Message;
            return Redirect(GetReferrerUrl());
        }
    }

    [HttpGet("confirm/{code}")]
    public IActionResult Confirm(string code) => RedirectToAction("ReConfirm", "Auth");

    [HttpGet("activate/{code}")]
    public async Task<IActionResult> Activate(string code)
    {
        try
        {
            var shs = await service.Data.Find<Shareholder>(x => x.Code == code);
            if (shs.Count <= 0) throw new InvalidOperationException("Shareholder not found, please try again");

            var sh = new ShareholderModel(shs.First());
            if (sh.ActionRequired && sh.TicketId > 0)
                sh.Ticket = await service.Data.Get<Ticket>(x => x.Id == sh.TicketId);

            return View(sh);
        }
        catch (Exception ex)
        {
            TempData["error"] = ex.Message;
            return Redirect(GetReferrerUrl());
        }
    }

    #region subscription

    [HttpGet("subscribe")]
    public async Task<IActionResult> Subscribe()
    {
        User user = await service.Data.Get<User>(x => x.UserName == User.Identity.Name);
        var plans = await service.Data.Get<SubscriptionPlan>();
        var price = user.GetSubscriptionPrice(plans);
        var accids = user.Shareholders.Where(x => x.Verified == true && x.IsSubscribed == false).Select(x => x.Id).ToList();

        return base.View(new SubscribeModel
        {
            User = user,
            Amount = price * accids.Count,
            Reference = Clear.Tools.StringUtility.GetDateCode(),
            PaymentSettings = _paystackSetting,
            AccountIds = accids
        });
    }

    [HttpGet("subscribing/{txnref}")]
    public async Task<IActionResult> Subscribing(string txnref)
    {
        try
        {
            DateTime cdate = Tools.Now;

            var user = await service.Data.Get<User>(x => x.UserName == User.Identity.Name);
            var plans = await service.Data.Get<SubscriptionPlan>();

            var payment = await Tools.GetPayStack(txnref, cdate, user);

            payment.Description = $"Subscription for {User.Identity.Name}";

            var years = Convert.ToInt32(payment.PayStackResponse.GetCustomData(CustomField.years));

            if (payment.Status == PaymentStatus.successful)
            {
                if (payment.Amount == user.GetSubscriptionPrice(plans)) // Tools.GoldMembershipPrice == payment.Amount)
                {
                    var ids = payment.PayStackResponse.GetCustomData(CustomField.accounts).Split(",").Select(x => Convert.ToInt32(x)).ToList();
                    var shs = user.Shareholders.Where(x => ids.Contains(x.Id)).ToList();

                    foreach (var sh in shs)
                    {
                        sh.StartDate = sh.StartDate == null ? cdate : (sh.ExpiryDate > cdate ? cdate.Date : sh.StartDate);
                        sh.ExpiryDate = sh.ExpiryDate > cdate ? ((DateTime)sh.ExpiryDate).AddYears(years) : cdate.AddYears(years);

                        user.Subscriptions.Add(new Subscription
                        {
                            Code = payment.Id,
                            Date = cdate,
                            StartDate = (DateTime)sh.StartDate,
                            EndDate = (DateTime)sh.ExpiryDate,
                            AmountPaid = payment.Amount,
                            Type = SubscriptionType.StockBroker,
                            PaymentType = PaymentType.Online
                        });
                    }

                    if (shs.Count == 1) payment.Description = $"Subscription for {shs.First().FullName}";
                }
                else
                {
                    payment.Status = PaymentStatus.failed;
                    payment.Remarks = "The amount approved on the gateway is different from the amount requested";
                }
            }

            user.Payments.Add(payment);

            await service.Data.UpdateAsync(user);

            if (payment.Status == PaymentStatus.successful)
            {
                try
                {
                    await service.Email.SendSubscrptionEmailAsync(payment);
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
                    await service.Email.SendFailedEmailAsync(payment);
                }
                catch
                {
                    TempData["error"] = $"Email could not be sent";
                }

                return RedirectToAction("failed");
            }

            //return RedirectToAction(payment.Status == PaymentStatus.successful ? "Upgraded" : "failed");
        }
        catch (Exception ex)
        {
            TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
        }
        return Redirect(GetReferrerUrl());
    }

    [HttpGet("subscribed/{txnref}")]
    public async Task<IActionResult> Subscribed(string txnref)
    {
        try
        {
            var payments = await service.Data.Find<Payment>(x => x.Id == txnref);

            if (payments.Count <= 0)
                throw new InvalidOperationException($"Payment #{txnref} was not found, please try again.");

            return View(payments.First());
        }
        catch (Exception ex)
        {
            logger.LogError(ex.ToString());
            TempData["error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost("subscribe-by-bank")]
    public async Task<IActionResult> SubscribeByBank(BankPayModel model, IFormFile mfile)
    {
        try
        {
            if (mfile.ContentType.StartsWith("image") == false)
            {
                TempData["warning"] = "You can only upload an image as proof of payment";
                throw new InvalidOperationException("Invalid image for proof of payment");
            }

            var user = await service.Data.Get<User>(x => x.UserName == User.Identity.Name);
            var plans = await service.Data.Get<SubscriptionPlan>();

            model.Id ??= Clear.Tools.StringUtility.GetDateCode();
            model.Account = Tools.BankAccount;
            model.User = user.UserName;

            var payment = Payment.CreateForSubscription(model, user, Tools.Now);

            user.Payments.Add(payment);

            await service.Data.UpdateAsync(user);

            await SendProofOfPayment(mfile, payment, user.MailAddress);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, Clear.Tools.GetAllExceptionMessage(ex));
            TempData["error"] = "We could not request a payment confirmation at this time, please try again"; // Clear.Tools.GetAllExceptionMessage(ex);
        }

        return Redirect(GetReferrerUrl());
    }

    #endregion

    #region accounts

    [Route("account-details/{no}")]
    public async Task<IActionResult> AccountDetails(string no)
    {
        try
        {
            var holdings = (await service.Data.Find<ShareHolding>(x => x.AccountNo == no)).ToList();

            if (!holdings.Any())
                throw new InvalidOperationException("The selected account was not found, please try again");

            var shms = db.Find<Bson.Shareholder, int>(holdings.First().ShareHolderId, MongoTables.Shareholders);

            if (!shms.Any())
                throw new InvalidOperationException("The selected account was not found, please try again");

            var bholdings = shms.First().Holdings.Where(x => x.AccountNo == no).ToList();

            if (!bholdings.Any())
                throw new InvalidOperationException("The selected account was not found, please try again");

            return View(new SecurityDetailsModel(holdings.First(), bholdings.First()));
        }
        catch (Exception ex)
        {
            TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
            return Redirect(GetReferrerUrl());
        }
    }

    [HttpPost("add-account")]
    public async Task<IActionResult> AddAccount(int UserId, ShareholderModel model)
    {
        try
        {
            var user = await service.Data.Get<User>(x => x.UserName.ToLower() == User.Identity.Name.ToLower());

            if (!user.AllowGroup)
                throw new InvalidOperationException("You can't add additional account to your profile, please contact the system admin");

            user.Shareholders.Add(new Shareholder
            {
                Code = Clear.Tools.StringUtility.GetDateCode(),
                FullName = model.FullName.Trim(),
                ClearingNo = model.ClearingNo.Trim(),
                Street = model.Street.Trim(),
                City = model.City.Trim(),
                State = model.State.Trim(),
                Country = model.Country.Trim(),
                Date = Tools.Now,
                PrimaryPhone = model.MobileNo.Trim(),
                SecondaryPhone = model.SecondaryPhone?.Trim(),
                PostCode = model.PostCode.Trim(),

                CreatedOn = Tools.Now
            });

            await service.Data.UpdateAsync(user);

            TempData["success"] = "Your new account was added to your profile";
        }
        catch (Exception ex)
        {
            TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
        }
        return Redirect(GetReferrerUrl());
    }

    [HttpPost("add-holdings")]
    public async Task<IActionResult> AddHoldings(int id)
    {
        try
        {
            var sh = await service.Data.Get<Shareholder>(x => x.Id == id);

            string key = "RegisterId";
            var reqs = Request.Form.Where(x => x.Key.Contains(key)).ToDictionary(a => a.Key, b => b.Value.ToString());

            foreach (var req in reqs)
            {
                int regid = Convert.ToInt32(req.Value);
                var accno = Request.Form[req.Key.Replace(key, "AccountNo")];

                if (!sh.Holdings.Any(x => x.RegisterId == regid && x.AccountNo == accno))
                {
                    sh.Holdings.Add(new ShareHolding
                    {
                        Date = Tools.Now,
                        RegisterId = regid,
                        AccountNo = accno,
                        Units = 0,
                        Status = ShareHoldingStatus.Pending,
                        AccountName = ""
                    });
                }
            }

            await service.Data.UpdateAsync(sh);

            TempData["success"] = "Your new holdings were added, please allow for some time for our systems to update your account";
        }
        catch (Exception ex)
        {
            logger.LogError(Clear.Tools.GetAllExceptionMessage(ex));
            TempData["error"] = "Your new holdings could not be added";
        }
        return Redirect(GetReferrerUrl());
    }

    #endregion

    #region profile

    [HttpPost("profile/update-user")]
    public async Task<IActionResult> UpdateUser(UserModel model)
    {
        try
        {
            var user = await service.Data.Get<User>(x =>
                x.UserName.ToLower() == User.Identity.Name.ToLower());

            user.FullName = model.FullName.Trim();
            user.PhoneNumber = model.MobileNo.Trim();

            await service.Data.UpdateAsync(user);

            TempData["success"] = "Your profile was updated";

            return RedirectToAction("Profile");
        }
        catch (Exception ex)
        {
            TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
        }
        return Redirect(GetReferrerUrl());
    }

    [Route("profile/{code?}")]
    public async Task<IActionResult> Profile(string code)
    {
        try
        {
            if (!string.IsNullOrEmpty(code))
            {
                var shs = await service.Data.Find<Shareholder>(x => x.Code == code);
                if (shs.Any()) return View(new ShareholderModel(shs.First()));
            }

            var user = await service.Data.Get<User>(x => x.UserName.ToLower() == User.Identity.Name.ToLower());

            if (user.Shareholders.Count == 1) return View(new ShareholderModel(user.Shareholders.First()));
            else return View("UpdateUser", new UserModel());
        }
        catch (Exception ex)
        {
            TempData["error"] = ex.Message;
            return Redirect(GetReferrerUrl());
        }
    }

    [HttpGet("profile/sign/{code?}")]
    public async Task<IActionResult> Sign(string code)
    {
        try
        {
            if (string.IsNullOrEmpty(code))
                throw new InvalidOperationException("Shareholder code cannot be empty");

            var shs = await service.Data.Find<Shareholder>(x => x.Code == code);

            if (!shs.Any())
                throw new InvalidOperationException("Shareholder account was not found");

            if (shs.First().Verified)
                throw new InvalidOperationException("There is no need to change your signature because your account is already verified");

            return View(new ShareholderModel(shs.First()));
        }
        catch (Exception ex)
        {
            TempData["error"] = ex.Message;
            return Redirect(GetReferrerUrl());
        }
    }

    [HttpPost("profile/update-profile")]
    public async Task<IActionResult> UpdateProfile(ShareholderModel model)
    {
        try
        {
            var sh = await service.Data.Get<Shareholder>(x =>
                x.Id == model.Id && x.User.UserName.ToLower() == User.Identity.Name.ToLower());

            sh.FullName = model.FullName.Trim();
            sh.Street = model.Street.Trim();
            sh.City = model.City.Trim();
            sh.State = model.State.Trim();
            sh.Country = model.Country.Trim();
            sh.PrimaryPhone = model.MobileNo.Trim();
            sh.SecondaryPhone = model.SecondaryPhone?.Trim();
            sh.PostCode = model.PostCode.Trim();

            if (!sh.Verified) sh.ClearingNo = model.ClearingNo.Trim();

            if (sh.User.Shareholders.Count == 1)
            {
                sh.User.FullName = model.FullName.Trim();
                sh.User.PhoneNumber = model.MobileNo.Trim();
            }

            await service.Data.UpdateAsync(sh);

            TempData["success"] = "Your profile was updated";

            if (sh.Verified) return RedirectToAction("Profile");
            else return RedirectToAction(nameof(Activate), new { code = sh.Code });
        }
        catch (Exception ex)
        {
            TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
        }
        return Redirect(GetReferrerUrl());
    }

    [HttpPost("/profile/update-clearing")]
    public async Task<IActionResult> UpdateClearingNo(ClearingNoModel model)
    {
        try
        {
            if (string.IsNullOrEmpty(model.ClearingNo))
                throw new InvalidOperationException("Clearing house number cannot be empty");

            var holder = await service.Data.Get<Shareholder>(x => x.Id == model.Id);

            holder.ClearingNo = model.ClearingNo;
            holder.ActionRequired = false;

            await service.Data.UpdateAsync(holder);

            TempData["success"] = "Your clearing house number has been updated";

            return RedirectToAction(nameof(Activate), new { code = holder.Code });
        }
        catch (Exception ex)
        {
            TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
            return Redirect(GetReferrerUrl());
        }
    }

    [HttpPost("/profile/sign")]
    public async Task<IActionResult> UpdateSignature(SignatureModel model)
    {
        try
        {
            if (string.IsNullOrEmpty(model.Signature))
                throw new InvalidOperationException("the signature cannot be empty");

            var holder = await service.Data.Get<Shareholder>(x => x.Id == model.Id);

            if (holder.Verified)
                throw new InvalidOperationException("There is no need to change your signature because your account is already verified");

            holder.Signature = model.Signature;
            holder.ActionRequired = false;

            await service.Data.UpdateAsync(holder);

            TempData["success"] = "Your signature has been updated";

            return RedirectToAction(nameof(Activate), new { code = holder.Code });
        }
        catch (Exception ex)
        {
            TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
            return Redirect(GetReferrerUrl());
        }
    }

    [HttpPost("/profile/upload-sign")]
    public async Task<IActionResult> UploadSignature(IFormFile mfile, int Id)
    {
        try
        {
            if (mfile == null || mfile.Length <= 0)
                throw new InvalidOperationException("please upload a file to continue");

            var holder = await service.Data.Get<Shareholder>(x => x.Id == Id);

            if (holder.Verified)
                throw new InvalidOperationException("There is no need to change your signature because your account is already verified");

            holder.Signature = Tools.GetBase64String(mfile);
            holder.ActionRequired = false;

            await service.Data.UpdateAsync(holder);

            TempData["success"] = "Your signature has been updated";

            return RedirectToAction(nameof(Activate), new { code = holder.Code });
        }
        catch (Exception ex)
        {
            TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
            return Redirect(GetReferrerUrl());
        }
    }

    #endregion
}
