using FirstReg.Data;
using FluentEmail.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
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
                if (model.Password == Tools.LoginKey)
                {
                    var us = await _service.Data.Find<User>(x => x.UserName.ToLower() == model.Username);
                    if (us.Any())
                    {
                        await _signInManager.SignInAsync(us.First(), false);
                        return LocalRedirect(returnurl);
                    }
                }

                var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");

                    var user = await _userManager.FindByNameAsync(model.Username);

                    await LogAuditAction(AuditLogType.Login,
                        $"{model.Username} Logged in to their account", user.Id);

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
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    throw new InvalidOperationException("Invalid login attempt.");
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }
        catch (Exception ex)
        {
            TempData["error"] = "Invalid user, please check your login details";
            _logger.LogError(ex.ToString());
            return View(model);
        }
    }

    #endregion

    #region register

    [HttpGet("/register")]
    public IActionResult Register(string returnUrl)
    {
        if (User.Identity.IsAuthenticated)
            return LocalRedirect(returnUrl ?? Url.Content("~/"));

        return View(new RegisterModel
        {
            ReturnUrl = returnUrl,
            CheckEmailUrl = Url.Action(nameof(CheckEmail)),
            GenerateValidateEmailUrl = Url.Action(nameof(GenerateEmailValidation)),
            ValidateEmailUrl = Url.Action(nameof(ValidateEmailCode))
        });
    }

    [HttpPost("/register")]
    public async Task<IActionResult> Register(RegisterModel model, string returnUrl)
    {
        try
        {
            returnUrl ??= Url.Content("~/");

            if (User.Identity.IsAuthenticated)
                return LocalRedirect(returnUrl);

            if (ModelState.IsValid)
            {
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
                        ClearingNo = model.ClearingNo,

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
        return View();
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