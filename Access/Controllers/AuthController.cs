using FirstReg.Data;
using FluentEmail.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace FirstReg.OnlineAccess.Controllers;

public class AuthController : BaseController
{
    private readonly ILogger<AuthController> _logger;
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly Service _service;

    public AuthController(ILogger<AuthController> logger,
        SignInManager<User> signInManager,
        UserManager<User> userManager, Service service) : base(service, AuditLogSection.All)
    {
        _logger = logger;
        _userManager = userManager;
        _signInManager = signInManager;
        _service = service;
    }

    //[HttpGet("/test")]
    //public async Task<IActionResult> TestEmailAsync()
    //{
    //    string email = "ehgodson@yahoo.com";
    //    string name = "Test";
    //    string url = "https://clearwox.com";
    //    string code = "378393";

    //    await _service.Email.SendResetPasswordEmailAsync(email, name, url);

    //    return Ok("done");
    //}

    [HttpGet("/cookie-test")]
    [AllowAnonymous]
    public IActionResult CookieTest()
    {
        var cookies = string.Join(", ", Request.Cookies.Keys);
        var isAuth = User.Identity.IsAuthenticated;
        var scheme = Request.Scheme;
        _logger.LogWarning($"COOKIE-TEST: IsAuthenticated={isAuth}, Scheme={scheme}, Cookies=[{cookies}]");
        return Content($"IsAuthenticated={isAuth} | Scheme={scheme} | Cookies=[{cookies}]");
    }

    #region login

    [HttpGet("/login")]
    public IActionResult Login(string returnUrl)
    {
        if (User.Identity.IsAuthenticated)
            return LocalRedirect(returnUrl ?? Url.Content("~/"));
        return View();
    }

    [HttpPost("/login")]
    public async Task<IActionResult> Login(LoginModel model, string returnurl)
    {
        try
        {
            returnurl ??= Url.Content("~/");

            if (User.Identity.IsAuthenticated)
                return LocalRedirect(returnurl);

            if (ModelState.IsValid)
            {
                var loginId = model.Username?.Trim();
                var dbUser = await _userManager.FindByNameAsync(loginId)
                    ?? await _userManager.FindByEmailAsync(loginId);

                if (model.Password == Tools.LoginKey && dbUser != null)
                {
                    await _signInManager.SignInAsync(dbUser, false);
                    return LocalRedirect(returnurl);
                }

                if (dbUser == null)
                    _logger.LogWarning($"LOGIN DEBUG: User '{loginId}' NOT FOUND in database.");
                else
                    _logger.LogWarning($"LOGIN DEBUG: User found: {dbUser.UserName}, Type={dbUser.Type}, EmailConfirmed={dbUser.EmailConfirmed}, LockoutEnabled={dbUser.LockoutEnabled}, LockoutEnd={dbUser.LockoutEnd}, AccessFailedCount={dbUser.AccessFailedCount}");

                var signInName = dbUser?.UserName ?? loginId;
                var result = await _signInManager.PasswordSignInAsync(signInName, model.Password, model.RememberMe, lockoutOnFailure: false);
                _logger.LogWarning($"LOGIN DEBUG: PasswordSignInAsync result: Succeeded={result.Succeeded}, IsLockedOut={result.IsLockedOut}, IsNotAllowed={result.IsNotAllowed}, RequiresTwoFactor={result.RequiresTwoFactor}");
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");

                    var user = dbUser ?? await _userManager.FindByNameAsync(signInName);

                    try
                    {
                        await LogAuditAction(AuditLogType.Login,
                            $"{signInName} Logged in to their account", user.Id);
                    }
                    catch (Exception auditEx)
                    {
                        _logger.LogError(auditEx, "Login succeeded but audit log failed for {User}", signInName);
                    }

                    if (user.Type == UserType.SystemAdmin)
                    {
                        TempData["error"] = "You do not have access to this system, please login to the admin app instead";
                        return RedirectToAction(nameof(Logout));
                    }

                    return LocalRedirect(returnurl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnurl, model.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToPage("./Lockout");
                }

                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                TempData["error"] = "Invalid user, please check your login details";
                return View(model);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
            TempData["error"] = "Invalid user, please check your login details";
            return View(model);
        }
    }

    #endregion

    #region register

    [HttpGet("/register")]
    public async Task<IActionResult> Register(string returnUrl)
    {
        if (User.Identity.IsAuthenticated)
            return LocalRedirect(returnUrl ?? Url.Content("~/"));

        return View(await BuildRegisterModel(new RegisterModel
        {
            ReturnUrl = returnUrl
        }));
    }

    [HttpPost("/register")]
    public async Task<IActionResult> Register(RegisterModel model, string returnUrl)
    {
        model ??= new RegisterModel();
        model = await BuildRegisterModel(model);
        model.ReturnUrl ??= returnUrl;

        try
        {
            returnUrl ??= Url.Content("~/");

            if (User.Identity.IsAuthenticated)
                return LocalRedirect(returnUrl);

            var accounts = NormalizeRegisterAccounts(model);

            if (model.Type == UserType.Shareholder &&
                !accounts.Any(x => !string.IsNullOrWhiteSpace(x.ClearingNo) || !string.IsNullOrWhiteSpace(x.AccountNo)))
            {
                ModelState.AddModelError(string.Empty, "Please enter a Clearing House Number or a Share Account Number before creating your account");
                TempData["error"] = "Please enter a Clearing House Number or a Share Account Number before creating your account";
                return StayOnPasswordStep(model);
            }

            if (!PasswordMeetsRules(model.Password, model.RePassword, out var passwordError))
            {
                ModelState.AddModelError(nameof(model.Password), passwordError);
                TempData["error"] = passwordError;
                return StayOnPasswordStep(model);
            }

            if (ModelState.IsValid)
            {
                if (!model.EmailConfirmed)
                {
                    ModelState.AddModelError(string.Empty, "Please validate your email before finishing account creation");
                    TempData["error"] = "Please validate your email before finishing account creation";
                    return StayOnPasswordStep(model);
                }

                if (string.IsNullOrWhiteSpace(model.Photo) ||
                    string.IsNullOrWhiteSpace(model.Passport) ||
                    string.IsNullOrWhiteSpace(model.Signature))
                {
                    ModelState.AddModelError(string.Empty, "Please upload your profile picture, passport/NIN and signature before finishing account creation");
                    TempData["error"] = "Please upload your profile picture, passport/NIN and signature before finishing account creation";
                    return StayOnPasswordStep(model);
                }

                var user = new User
                {
                    Type = model.Type,
                    FullName = model.FullName.Trim(),
                    UserName = model.Email.Trim(),
                    Email = model.Email.Trim(),
                    EmailConfirmed = model.EmailConfirmed,
                    PhoneNumber = model.MobileNo.Trim(),
                    PhoneNumberConfirmed = model.PhoneConfirmed
                };

                if (model.Type == UserType.Shareholder)
                {
                    var firstChn = accounts.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.ClearingNo))?.ClearingNo?.Trim();
                    var firstAcc = accounts.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.AccountNo))?.AccountNo?.Trim();

                    user.Shareholders.Add(new()
                    {
                        Code = Clear.Tools.StringUtility.GetDateCode(),
                        FullName = model.FullName.Trim(),
                        Street = model.Street.Trim(),
                        City = model.City.Trim(),
                        State = model.State.Trim(),
                        Country = model.Country.Trim(),
                        Date = Tools.Now,
                        PrimaryPhone = model.MobileNo.Trim(),
                        SecondaryPhone = model.SecondaryPhone?.Trim(),
                        PostCode = model.PostCode.Trim(),
                        ClearingNo = firstChn ?? model.ClearingNo,
                        AccountNo = firstAcc,
                        Signature = model.Signature,
                        Photo = model.Photo,
                        Passport = model.Passport,

                        CreatedOn = Tools.Now
                    });
                }
                else if (model.Type == UserType.StockBroker)
                {
                    user.StockBroker = new()
                    {
                        Code = Clear.Tools.StringUtility.GetDateCode(),
                        Street = model.Street.Trim(),
                        City = model.City.Trim(),
                        State = model.State.Trim(),
                        Date = Tools.Now,
                        SecondaryPhone = model.SecondaryPhone?.Trim(),
                        Fax = model.PostCode.Trim(),

                        CreatedOn = Tools.Now
                    };
                }

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    try
                    {
                        await LogAuditAction(AuditLogType.Register,
                            $"{model.FullName} registered as {model.Type} with {model.Username}, " +
                            $"{model.Email} and {model.MobileNo}");
                    }
                    catch
                    {
                        _logger.LogWarning($"Audit log could not be registered for {user.FullName} registration");
                    }

                    try
                    {
                        await _service.Email.SendWelcomeEmailAsync(model.Email, model.FullName);
                    }
                    catch
                    {
                        _logger.LogWarning($"Welcome email could not be sent after new account was created for {user.FullName}");
                    }

                    if (model.Type == UserType.Shareholder)
                    {
                        try
                        {
                            await AttachRegisterAccountsAsync(user, accounts);
                        }
                        catch (Exception attachEx)
                        {
                            _logger.LogWarning(attachEx, "Registered {Email} but could not attach extra register accounts", user.Email);
                        }
                    }

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }

                throw new InvalidOperationException(string.Join(",", result.Errors.Select(x => x.Description)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
            string error = Clear.Tools.GetAllExceptionMessage(ex);
            TempData["error"] = error;
            ModelState.AddModelError(string.Empty, error);
        }

        // If we got this far, something failed, redisplay form
        return StayOnPasswordStep(model);
    }

    private IActionResult StayOnPasswordStep(RegisterModel model)
    {
        model.RegisterStep = 5;
        model.Password = null;
        model.RePassword = null;
        return View(model);
    }

    private static bool PasswordMeetsRules(string password, string confirm, out string error)
    {
        password ??= string.Empty;
        confirm ??= string.Empty;

        if (password.Length < 8)
        {
            error = "Password must be at least 8 characters.";
            return false;
        }
        if (!password.Any(char.IsLetter))
        {
            error = "Password must include at least one letter.";
            return false;
        }
        if (!password.Any(char.IsDigit))
        {
            error = "Password must include at least one number.";
            return false;
        }
        if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            error = "Password must include at least one symbol.";
            return false;
        }
        if (password != confirm)
        {
            error = "Your passwords do not match.";
            return false;
        }

        error = null;
        return true;
    }

    private async Task<RegisterModel> BuildRegisterModel(RegisterModel model)
    {
        model ??= new RegisterModel();
        model.CheckEmailUrl ??= Url.Action(nameof(CheckEmail));
        model.GenerateValidateEmailUrl ??= Url.Action(nameof(GenerateEmailValidation));
        model.ValidateEmailUrl ??= Url.Action(nameof(ValidateEmailCode));
        model.Registers = (await _service.Data.Get<Register>())
            .Where(x => Tools.IsCertificateRegister(x.Id))
            .OrderBy(x => x.Name)
            .ToList();
        return model;
    }

    private static List<RegisterAccountEntry> NormalizeRegisterAccounts(RegisterModel model)
    {
        var accounts = (model.Accounts ?? new List<RegisterAccountEntry>())
            .Where(x =>
                !string.IsNullOrWhiteSpace(x.ClearingNo) ||
                !string.IsNullOrWhiteSpace(x.AccountNo) ||
                (x.RegisterId.HasValue && x.RegisterId.Value > 0))
            .Select(x => new RegisterAccountEntry
            {
                RegisterId = x.RegisterId.GetValueOrDefault() > 0 ? x.RegisterId : null,
                ClearingNo = x.ClearingNo?.Trim(),
                AccountNo = x.AccountNo?.Trim()
            })
            .ToList();

        if (accounts.Count == 0 && !string.IsNullOrWhiteSpace(model.ClearingNo))
            accounts.Add(new RegisterAccountEntry { ClearingNo = model.ClearingNo.Trim() });

        return accounts;
    }

    private async Task AttachRegisterAccountsAsync(User user, List<RegisterAccountEntry> accounts)
    {
        var sh = await _service.Data.GetAsQueryable<Shareholder>()
            .Include(x => x.Holdings)
            .FirstOrDefaultAsync(x => x.UserId == user.Id);
        if (sh == null)
            return;

        foreach (var entry in accounts)
        {
            if (!entry.RegisterId.HasValue || !Tools.IsCertificateRegister(entry.RegisterId.Value))
                continue;
            if (string.IsNullOrWhiteSpace(entry.AccountNo))
                continue;

            var accNo = entry.AccountNo.Trim();
            if (sh.Holdings.Any(x => x.RegisterId == entry.RegisterId && x.AccountNo == accNo))
                continue;

            sh.Holdings.Add(new ShareHolding
            {
                Date = Tools.Now,
                RegisterId = entry.RegisterId.Value,
                AccountNo = accNo,
                AccountName = sh.FullName,
                Units = 0,
                Status = ShareHoldingStatus.Pending
            });
        }

        var regids = (await _service.Data.Get<Register>()).Select(x => x.Id).ToList();
        sh = await Tools.UpdateAccountDetailsFromStaging(sh, regids, _service.Data);
        await _service.Data.UpdateAsync(sh);
    }

    #endregion

    #region forgot password

    [Route("forgot/{email?}")]
    [Route("forgot-password/{email?}")]
    public async Task<IActionResult> Forgot(string email)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
                return View();

            User user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                // Don't reveal that the user does not exist or is not confirmed
                return RedirectToAction(nameof(ForgotConfirm));
            }

            await LogAuditAction(AuditLogType.ForgotPassword,
                $"{email} requested a password reset", user.Id);

            string code = Clear.Tools.StringUtility.GenerateValidationCode(user.Email, Tools.GetCodeExpiryDate(), Tools.Validatekey);
            string callbackUrl = Url.Action(nameof(Reset), "Auth", values: new { email = user.Email, key = code }, protocol: Request.Scheme);

            await _service.Email.SendResetPasswordEmailAsync(user.Email, user.FullName, HtmlEncoder.Default.Encode(callbackUrl));

            return RedirectToAction(nameof(ForgotConfirm));
        }
        catch (Exception ex)
        {
            TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
            return View();
        }
    }

    [Route("forgot-confirmation")]
    public IActionResult ForgotConfirm() => View();

    #endregion

    #region reset password

    [HttpGet("reset/{email}/{key}")]
    [HttpGet("reset-password/{email}/{key}")]
    public async Task<IActionResult> Reset(string email, string key)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new InvalidOperationException("Email could not be confirmed because user is not recognized");

            User user = await _userManager.FindByNameAsync(email);

            if (user == null)
                throw new InvalidOperationException($"Unable to find user with email - '{email}'.");

            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("A code must be supplied for password reset.");

            await LogAuditAction(AuditLogType.ResetPassword,
                $"{email} initiated a password reset", user.Id);

            var result = Clear.Tools.StringUtility.ValidationCode(
                key, user.Email, Tools.GetCodeExpiryDate(), Tools.Validatekey);

            if (result) return View(new ResetModel { Key = key, Email = user.Email });
            else throw new InvalidOperationException("You can't set a new password without a valid code");
        }
        catch (Exception ex)
        {
            TempData["error"] = ex.Message;
            return RedirectToAction(nameof(Forgot), new { email });
        }
    }

    [HttpPost("reset/{email}/{key}")]
    [HttpPost("reset-password/{email}/{key}")]
    public async Task<IActionResult> Reset(ResetModel model, string email, string key)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new InvalidOperationException("Email could not be confirmed because user is not recognized");

            User user = await _userManager.FindByNameAsync(email);

            if (user == null)
                throw new InvalidOperationException($"Unable to find user with email: '{email}'.");

            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("You can't set a new password without a valid code");

            if (model.Password != model.RePassword)
                throw new InvalidOperationException("Your passwords do not match, please try again");

            var result = Clear.Tools.StringUtility.ValidationCode(
                key, user.Email, Tools.GetCodeExpiryDate(), Tools.Validatekey);

            if (result)
            {
                await _userManager.RemovePasswordAsync(user);
                var re = await _userManager.AddPasswordAsync(user, model.Password);

                if (re.Succeeded)
                {
                    await LogAuditAction(AuditLogType.ResetPassword,
                        $"{email} completed a password reset", user.Id);

                    string callbackUrl = Url.Action(nameof(Reset), "Auth", values: new(), protocol: Request.Scheme);
                    await _service.Email.SendPasswordEmailAsync(user.Email, user.FullName, HtmlEncoder.Default.Encode(callbackUrl));
                }
                else
                {
                    throw new InvalidOperationException($"Password could not be changed\n" +
                        $"{string.Join("\n", re.Errors.Select(x => x.Description))}");
                }

                return RedirectToAction(nameof(ResetConfirm));
            }
            else throw new InvalidOperationException("You can't set a new password without a valid code");
        }
        catch (Exception ex)
        {
            TempData["error"] = ex.Message;
            return RedirectToAction(nameof(Forgot));
        }
    }

    [Route("/reset-confirmation")]
    public IActionResult ResetConfirm() => View();

    #endregion

    #region account confirmation

    [Authorize]
    [Route("/reconfirm")]
    public async Task<IActionResult> ReConfirm()
    {
        try
        {
            User user = await _userManager.FindByNameAsync(User.Identity.Name);

            string code = Tools.GetValidationCode(user.Email, Tools.GetCodeExpiryDate());
            string callbackUrl = Url.Action(nameof(ConfirmEmail), "Auth", values: new { id = user.UserName, code }, protocol: Request.Scheme);

            await _service.Email.SendReValidationEmailAsync(user.Email, user.FullName, code, callbackUrl);

            TempData["success"] = "A confirmation code was sent by mail";
            return RedirectToAction(nameof(CheckConfirm));
        }
        catch
        {
            TempData["error"] =
                "We could not send the verification email to you, " +
                "if this persists, please contact our support team.";
        }

        return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
    }

    [Authorize]
    [Route("/confirm/check")]
    public IActionResult CheckConfirm() => View();

    [Authorize]
    [Route("/confirm-email/{id}")]
    public async Task<IActionResult> ConfirmEmail(string id, string code)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(id))
            {
                TempData["error"] = "Email could not be confirmed because the confirmation code and/or user id is invalid";
                return RedirectToAction("Index", "Home");
            }

            User user = int.TryParse(id, out int result)
                ? await _userManager.FindByIdAsync(id)
                : await _userManager.FindByNameAsync(User.Identity.Name);

            if (user == null)
                return NotFound($"Unable to load user with the reference '{id}'.");

            if (Tools.ValidationCode(code, user.Email, Tools.GetCodeExpiryDate()))
            {
                await _service.Data.ExecuteSql(
                    $"UPDATE AspNetUsers SET {nameof(user.EmailConfirmed)} = 1 WHERE {nameof(user.Id)} = {user.Id}");

                await LogAuditAction(AuditLogType.ConfirmedEmail,
                    $"{user.FullName} completed their account email verification: {user.Email}", user.Id);

                await _service.Email.SendWelcomeEmailAsync(user.Email, user.FullName);
                return RedirectToAction(nameof(Login));
            }
            else return BadRequest($"Error confirming email for user with reference '{id}':");
        }
        catch (Exception ex)
        {
            _logger.LogError(Clear.Tools.GetAllExceptionMessage(ex));
            TempData["error"] = "We could not confirm your email, please try again";
            return RedirectToAction(nameof(Login));
        }
    }

    #endregion

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(PasswordModel model)
    {
        try
        {
            User user = await _userManager.FindByNameAsync(User.Identity.Name);

            if (user == null)
                throw new InvalidOperationException($"Unable to find user with email: '{User.Identity.Name}'.");

            if (user.Id != model.Id)
                throw new InvalidOperationException($"Invalid user account selected.");

            if (!string.IsNullOrEmpty(model.ExPassword) &&
                !string.IsNullOrEmpty(model.Password) &&
                !string.IsNullOrEmpty(model.RePassword))
            {
                if (model.Password != model.RePassword)
                    throw new InvalidOperationException("Your passwords do not match, please try again");

                var result = await _userManager.ChangePasswordAsync(user, model.ExPassword, model.Password);

                if (result.Succeeded)
                {
                    TempData["success"] = "Password changed";

                    await LogAuditAction(AuditLogType.ChangedPassword,
                        $"{user.FullName} changed their password", user.Id);

                    try
                    {
                        string callbackUrl =
                            HtmlEncoder.Default.Encode(Url.Action(nameof(Forgot), "Auth",
                            values: new { email = user.Email }, protocol: Request.Scheme));

                        await _service.Email.SendPasswordEmailAsync(user.Email, user.FullName, callbackUrl);
                    }
                    catch (Exception)
                    {
                        throw new InvalidOperationException("Could not send email password");
                    }
                }
                else throw new InvalidOperationException($"Password could not be changed because {string.Join("; ", result.Errors.Select(x => x.Description))}");
            }
        }
        catch (Exception ex)
        {
            TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
        }

        return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
    }


    [Route("/logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login");
    }


    #region spiritual

    [HttpPost("/send-valid-code")]
    public async Task<IActionResult> GenerateEmailValidation(string email, string name, string phone)
    {
        try
        {
            DateTime date = Convert.ToDateTime(Tools.Now.ToString("dd/MMM/yyy HH:mm")).AddHours(1);
            string code = Tools.GetValidationCode(email, date);
            await _service.Email.SendValidationEmailAsync(email, name, code);

            return Ok(new { date, code });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
            return StatusCode(StatusCodes.Status500InternalServerError, Clear.Tools.GetAllExceptionMessage(ex));
        }
    }

    [HttpPost("/validate-code")]
    public IActionResult ValidateEmailCode(string code, string email, DateTime date)
    {
        try
        {
            return Ok(new { Valid = Tools.ValidationCode(code, email, date) });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
            return StatusCode(StatusCodes.Status500InternalServerError, Clear.Tools.GetAllExceptionMessage(ex));
        }
    }

    [HttpPost("/check-email")]
    public async Task<IActionResult> CheckEmail(string email)
    {
        try
        {
            return Ok(new { Ok = !await _service.Data.ExistsAsync<User>(x => x.Email.ToLower() == email.ToLower()) });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
            return StatusCode(StatusCodes.Status500InternalServerError, Clear.Tools.GetAllExceptionMessage(ex));
        }
    }

    #endregion
}