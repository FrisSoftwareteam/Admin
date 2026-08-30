using Clear;
using FirstReg.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FirstReg.OnlineAccess.Controllers;

[Authorize]
[Route("sh")]
public class ShareholderController(ILogger<ShareholderController> logger, Service service)
    : BaseController(service, AuditLogSection.Shareholder)
{
    private readonly PaymentSettings _paystackSetting = Tools.PaymentSettings;

    public async Task<IActionResult> Index()
    {
        try
        {
            var user = await service.Data.Get<User>(x => x.UserName.ToLower() == User.Identity.Name.ToLower());
            if (user == null)
            {
                TempData["error"] = "Your account could not be loaded. Please sign in again.";
                return RedirectToAction("Logout", "Auth");
            }

            var visible = user.VisibleShareholders.ToList();
            if (visible.Count == 0)
            {
                TempData["error"] = "No shareholder profile is currently available for this account.";
                return View(new ShareHolderDashboardModel(user));
            }

            if (visible.Count == 1)
            {
                if (!visible.First().Verified)
                    return RedirectToAction(nameof(Activate), new { code = visible.First().Code });

                if (!visible.First().IsSubscribed)
                    return RedirectToAction(nameof(Subscribe));

                var sh = await UpdateDetails(visible.First());
                sh = await service.Data.GetAsQueryable<Shareholder>()
                    .Include(x => x.User)
                    .Include(x => x.Holdings)
                    .ThenInclude(x => x.Register)
                    .FirstOrDefaultAsync(x => x.Id == sh.Id) ?? sh;
                ViewBag.Registers = await LoadCertificateRegisters();

                return View(nameof(Details), new ShareholderModel(sh));
            }

            foreach (var holder in visible.Where(x => x.Verified && x.IsSubscribed))
                await UpdateDetails(holder);

            return View(new ShareHolderDashboardModel(user));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Shareholder dashboard failed for {User}", User.Identity?.Name);
            TempData["error"] = ex.Message;
            return RedirectToAction("Error", "Home");
        }
    }

    private async Task<Shareholder> UpdateDetails(Shareholder sh)
    {
        try
        {
            if (sh.Hidden)
                return sh;
            sh = await service.Data.GetAsQueryable<Shareholder>()
                .Include(x => x.Holdings)
                .FirstOrDefaultAsync(x => x.Id == sh.Id) ?? sh;
            var regids = (await service.Data.Get<Register>()).Select(x => x.Id).ToList();
            sh = await Tools.UpdateAccountDetailsFromStaging(sh, regids, service.Data);
            if (sh.Verified) await service.Data.UpdateAsync(sh);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update shareholder details from staging for {ClearingNo}", sh.ClearingNo);
            TempData["error"] = "Some portfolio data could not be refreshed. Showing last known data.";
        }
        return sh;
    }

    private async Task<List<Register>> LoadCertificateRegisters()
    {
        var allowed = Tools.CertificateRegisterIds;
        return await service.Data.GetAsQueryable<Register>()
            .AsNoTracking()
            .Where(x => allowed.Contains(x.Id))
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    [Route("details/{code}")]
    public async Task<IActionResult> Details(string code)
    {
        try
        {
            var shs = await service.Data.Find<Shareholder>(x => x.Code == code && !x.Hidden);

            if (!shs.Any()) throw new InvalidOperationException("Shareholder account not found, please try again");

            if (!shs.First().Verified)
                return RedirectToAction(nameof(Activate), new { code });

            if (!shs.First().IsSubscribed)
                return RedirectToAction(nameof(Subscribe));

            var sh = await UpdateDetails(shs.First());
            sh = await service.Data.GetAsQueryable<Shareholder>()
                .Include(x => x.User)
                .Include(x => x.Holdings)
                .ThenInclude(x => x.Register)
                .FirstOrDefaultAsync(x => x.Id == sh.Id) ?? sh;
            ViewBag.Registers = await LoadCertificateRegisters();

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
            var shs = await service.Data.Find<Shareholder>(x => x.Code == code && !x.Hidden);
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
        var accids = user.VisibleShareholders.Where(x => x.Verified == true && x.IsSubscribed == false).Select(x => x.Id).ToList();
        var amount = SubscriptionAmountFor(user.VisibleShareholders.Where(x => accids.Contains(x.Id)), plans);

        return base.View(new SubscribeModel
        {
            User = user,
            Amount = amount,
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
                var ids = (payment.PayStackResponse.GetCustomData(CustomField.accounts) ?? "")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => Convert.ToInt32(x.Trim()))
                    .ToList();
                var shs = user.Shareholders.Where(x => ids.Contains(x.Id)).ToList();
                var expected = SubscriptionAmountFor(shs, plans);

                if (shs.Count > 0 && Math.Abs(payment.Amount - expected) < 0.01m)
                {
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
                            Type = sh.IsCompany ? SubscriptionType.CorporateShareholder : SubscriptionType.IndividualShareholder,
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
            var user = await service.Data.Get<User>(x => x.UserName.ToLower() == User.Identity.Name.ToLower());
            var holderIds = user.VisibleShareholders.Select(x => x.Id).ToList();

            var holding = await service.Data.GetAsQueryable<ShareHolding>()
                .Include(x => x.Shareholder)
                .Include(x => x.Register)
                .Where(x => !x.Hidden && x.AccountNo == no && holderIds.Contains(x.ShareHolderId))
                .OrderByDescending(x => x.Units)
                .FirstOrDefaultAsync();

            if (holding == null)
                throw new InvalidOperationException("The selected account was not found, please try again");

            return View(new SecurityDetailsModel(holding));
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
            var user = await service.Data.Get<User>(x => x.UserName.ToLower() == User.Identity.Name.ToLower());
            if (user == null || !user.VisibleShareholders.Any(x => x.Id == id))
                throw new InvalidOperationException("Shareholder account not found");

            var sh = await service.Data.GetAsQueryable<Shareholder>()
                .Include(x => x.Holdings)
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new InvalidOperationException("Shareholder account not found");

            string key = "RegisterId";
            var reqs = Request.Form.Where(x => x.Key.Contains(key)).ToDictionary(a => a.Key, b => b.Value.ToString());
            var added = 0;
            var rejected = 0;

            foreach (var req in reqs)
            {
                int regid = Convert.ToInt32(req.Value);
                if (!Tools.IsCertificateRegister(regid))
                    continue;
                var accno = Request.Form[req.Key.Replace(key, "AccountNo")].ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(accno) || !int.TryParse(accno, out var accNo))
                    continue;

                var staging = await service.Data.GetAsQueryable<ShareholderStaging>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.RegisterCode == regid && x.AccountNumber == accNo);

                if (staging != null && !Tools.StagingRowBelongsToShareholder(sh, staging))
                {
                    rejected++;
                    continue;
                }

                var existingHidden = sh.Holdings.FirstOrDefault(x =>
                    x.Hidden && x.RegisterId == regid && x.AccountNo == accNo.ToString());
                if (existingHidden != null)
                {
                    rejected++;
                    continue;
                }

                if (!sh.Holdings.Any(x => x.RegisterId == regid && x.AccountNo == accNo.ToString()))
                {
                    sh.Holdings.Add(new ShareHolding
                    {
                        Date = Tools.Now,
                        RegisterId = regid,
                        AccountNo = accNo.ToString(),
                        Units = 0,
                        Status = ShareHoldingStatus.Pending,
                        AccountName = staging?.Names ?? ""
                    });
                    added++;
                }
            }

            if (added > 0)
            {
                var regids = (await service.Data.Get<Register>()).Select(x => x.Id).ToList();
                sh = await Tools.UpdateAccountDetailsFromStaging(sh, regids, service.Data);
                await service.Data.UpdateAsync(sh);
                TempData["success"] = "Your new holdings were added, please allow for some time for our systems to update your account";
            }

            if (rejected > 0)
                TempData["error"] = "One or more accounts do not belong to this shareholder and were not added.";
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
                var shs = await service.Data.Find<Shareholder>(x => x.Code == code && !x.Hidden);
                if (shs.Any()) return View(new ShareholderModel(shs.First()));
            }

            var user = await service.Data.Get<User>(x => x.UserName.ToLower() == User.Identity.Name.ToLower());

            if (user.VisibleShareholders.Count() == 1) return View(new ShareholderModel(user.VisibleShareholders.First()));
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

    static decimal SubscriptionAmountFor(IEnumerable<Shareholder> shareholders, List<SubscriptionPlan> plans)
    {
        decimal amount = 0;
        foreach (var sh in shareholders)
        {
            var type = sh.IsCompany ? SubscriptionType.CorporateShareholder : SubscriptionType.IndividualShareholder;
            amount += plans.First(x => x.Id == type).Price;
        }
        return amount;
    }

    #endregion
}
