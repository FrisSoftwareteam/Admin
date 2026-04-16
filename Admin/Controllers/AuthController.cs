using FirstReg.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace FirstReg.Admin.Controllers
{
    public class AuthController : Controller
    {
        private readonly ILogger<AuthController> _logger;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly Service _service;
        private readonly string _accessUrl;

        public AuthController(ILogger<AuthController> logger,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            Service service, IConfiguration configuration)
        {
            _logger = logger;
            _userManager = userManager;
            _signInManager = signInManager;
            _service = service;
            _accessUrl = configuration.GetValue<string>(Common.AccessSettingName);
        }

        [HttpGet("/login")]
        public IActionResult Login() => View();

        [HttpPost("/login")]
        public async Task<IActionResult> Login(LoginModel model, string returnurl)
        {
            try
            {
                if (User.Identity.IsAuthenticated)
                    return RedirectToAction("Home", "Index");

                returnurl ??= Url.Content("~/");

                if (ModelState.IsValid)
                {
                    // This doesn't count login failures towards account lockout
                    // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                    var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, model.RememberMe, lockoutOnFailure: false);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User logged in.");

                        var user = await _userManager.FindByNameAsync(model.Username);
                        if (user.Type != UserType.SystemAdmin)
                        {
                            TempData["error"] = "You do not have access to this system, please login with an admin account";
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
                        return View(model);
                    }
                }

                // If we got this far, something failed, redisplay form
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
                _logger.LogError(ex.ToString());
                return View(model);
            }
        }

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

                string code = Clear.Tools.StringUtility.GenerateValidationCode(user.Email, Tools.GetCodeExpiryDate(), Tools.Validatekey);
                string callbackUrl = user.Type == UserType.SystemAdmin
                    ? HtmlEncoder.Default.Encode(Url.Action(nameof(Reset), "Auth", values: new { email = user.Email, key = code }, protocol: Request.Scheme))
                    : $"{_accessUrl}/reset-password/{user.Email}/{code}";

                await _service.Email.SendResetPasswordEmailAsync(user.Email, user.FullName, callbackUrl);

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
                    throw new InvalidOperationException("Email could not be confirmed bacause user is not recognized");

                User user = await _userManager.FindByEmailAsync(email);

                if (user == null)
                    throw new InvalidOperationException($"Unable to find user with email - '{email}'.");

                if (string.IsNullOrWhiteSpace(key))
                    throw new InvalidOperationException("A code must be supplied for password reset.");

                var result = Clear.Tools.StringUtility.ValidationCode(
                    key, user.Email, Tools.GetCodeExpiryDate(), Tools.Validatekey);

                if (result) return View(new ResetModel { Key = key, Email = user.Email });
                else throw new InvalidOperationException("You can't set a new password without a valid code");
            }
            catch (Exception ex)
            {
                TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
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

                User user = await _userManager.FindByEmailAsync(email);

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
                    if (!string.IsNullOrEmpty(user.PasswordHash))
                    {
                        await _userManager.RemovePasswordAsync(user);
                    }

                    var re = await _userManager.AddPasswordAsync(user, model.Password);

                    if (re.Succeeded)
                    {
                        string callbackUrl = HtmlEncoder.Default.Encode(Url.Action(nameof(Forgot), "Auth", values: new { email = user.Email }, protocol: Request.Scheme));
                        await _service.Email.SendPasswordEmailAsync(user.Email, user.FullName, callbackUrl);

                        return RedirectToAction(nameof(ResetConfirm), new { email, key });
                    }
                    else
                    {
                        throw new InvalidOperationException($"Password could not be changed\n" +
                            $"{string.Join("\n", re.Errors.Select(x => x.Description))}");
                    }
                }
                else throw new InvalidOperationException("You can't set a new password without a valid code");
            }
            catch (Exception ex)
            {
                TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
                return RedirectToAction(nameof(Forgot));
            }
        }

        [Route("/reset-confirmation")]
        public IActionResult ResetConfirm() => View();

        #endregion

        [Authorize]
        [HttpPost("update-profile")]
        public async Task<IActionResult> UpdateProfile(ProfileModel model)
        {
            try
            {
                User user = await _userManager.FindByNameAsync(User.Identity.Name);

                if (user == null)
                    throw new InvalidOperationException($"Unable to find user with email: '{User.Identity.Name}'.");

                if (user.Id != model.Id)
                    throw new InvalidOperationException($"Invalid user account selected.");

                user.FullName = model.FullName;
                user.PhoneNumber = model.MobileNo;

                await _service.Data.UpdateAsync(user);

                TempData["success"] = "Profile updated";

                if (!string.IsNullOrEmpty(model.ExPassword) &&
                    !string.IsNullOrEmpty(model.NewPassword) &&
                    !string.IsNullOrEmpty(model.RePassword))
                {
                    if (model.NewPassword != model.RePassword)
                        throw new InvalidOperationException("Your passwords do not match, please try again");

                    var result = await _userManager.ChangePasswordAsync(user, model.ExPassword, model.NewPassword);

                    if (result.Succeeded)
                    {
                        TempData["success"] += " and password changed";

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

        [HttpGet("/logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }
    }
}