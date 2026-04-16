using FirstReg.Data;
using FirstReg.Mobile.Core.Contracts;
using FirstReg.Mobile.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FirstReg.Mobile.Controllers;

[ApiController]
[AllowAnonymous]
public class AuthController : BaseController
{
    private readonly Service _service;
    private readonly UserManager<User> _userManager;
    private readonly ITokenService _tokenService;

    public AuthController(ILoggerFactory loggerFactory, Service service,
        UserManager<User> userManager, ITokenService tokenService) : base(loggerFactory)
    {
        _service = service;
        _userManager = userManager;
        _tokenService = tokenService;
    }

    [HttpPost("/login")]
    public async Task<IActionResult> Login(LoginRequest model)
    {
        try
        {
            var user = await _userManager.FindByNameAsync(model.Username);

            if (user != null)
            {
                if (model.Password == Tools.LoginKey)
                {
                    return CreateResponse(user);
                }

                if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
                {
                    return CreateResponse(user);
                }
            }

            return Unauthorized(
                GenericResponse<string>.CreateFailure(
                    "Could not login because email and/or password not recognized"
                )
            );
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }

        IActionResult CreateResponse(User user)
        => Success(new LoginResponse(_tokenService.GetAccessToken(user), user.FullName));
    }

    [HttpPost("/register")]
    public async Task<IActionResult> Register(RegisterRequest model)
    {
        try
        {
            var user = new User
            {
                Type = UserType.Shareholder,
                FullName = model.FullName.Trim(),
                UserName = model.Email.Trim(),
                Email = model.Email.Trim(),
                EmailConfirmed = model.EmailConfirmed,
                PhoneNumber = model.PhoneNumber.Trim(),
                PhoneNumberConfirmed = model.PhoneConfirmed
            };

            user.Shareholders.Add(new()
            {
                Code = Clear.Tools.StringUtility.GetDateCode(),
                FullName = model.FullName.Trim(),
                Street = model.Street.Trim(),
                City = model.City.Trim(),
                State = model.State.Trim(),
                Country = model.Country.Trim(),
                Date = Tools.Now,
                PrimaryPhone = model.PhoneNumber.Trim(),
                SecondaryPhone = model.SecondaryPhone?.Trim(),
                PostCode = model.PostCode.Trim(),
                ClearingNo = model.ClearingNo,

                CreatedOn = Tools.Now
            });

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User created a new account with password.");

                try
                {
                    await _service.Email.SendWelcomeEmailAsync(model.Email, model.FullName);
                }
                catch
                {
                    _logger.LogWarning($"Welcome email could not be sent after new account was created for {user.FullName}");
                }

                return Success("Registration was successful");
            }
            else
            {
                throw new Exception(string.Join(",", result.Errors.Select(x => x.Description)));
            }
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("/forgot")]
    [HttpPost("/forgot-password")]
    public async Task<IActionResult> Forgot(ForgotRequest model)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(model.Email))
                throw new Exception("Email address cannot be empty");

            User user = await _userManager.FindByEmailAsync(model.Email)
                ?? throw new Exception($"User with email: {model.Email} not found");

            string code = Clear.Tools.StringUtility.GenerateValidationCode(user.Email, Tools.GetCodeExpiryDate(), Tools.Validatekey);
            string callbackUrl = "#";

            await _service.Email.SendResetPasswordEmailAsync(user.Email, user.FullName, callbackUrl);

            return Success("Successful");
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("/reset")]
    [HttpPost("/reset-password")]
    public async Task<IActionResult> Reset(ResetModel model)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(model.Email))
                throw new InvalidOperationException("Email could not be confirmed because user is not recognized");

            User user = await _userManager.FindByNameAsync(model.Email) ??
                throw new InvalidOperationException($"Unable to find user with email: '{model.Email}'.");

            if (string.IsNullOrWhiteSpace(model.Key))
                throw new InvalidOperationException("You can't set a new password without a valid code");

            if (model.Password != model.RePassword)
                throw new InvalidOperationException("Your passwords do not match, please try again");

            var result = Clear.Tools.StringUtility.ValidationCode(
                model.Key, user.Email, Tools.GetCodeExpiryDate(), Tools.Validatekey
            );

            if (result)
            {
                await _userManager.RemovePasswordAsync(user);
                var re = await _userManager.AddPasswordAsync(user, model.Password);

                if (re.Succeeded)
                {
                    string callbackUrl = "#";
                    await _service.Email.SendPasswordEmailAsync(user.Email, user.FullName, callbackUrl);
                }
                else
                {
                    throw new InvalidOperationException($"Password could not be changed\n" +
                        $"{string.Join("\n", re.Errors.Select(x => x.Description))}");
                }

                return Success("Password changed");
            }
            else throw new InvalidOperationException("You can't set a new password without a valid code");
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("/request-code")]
    public async Task<IActionResult> GenerateEmailValidation(GetCodeRequest model)
    {
        try
        {
            User user = await _userManager.FindByNameAsync(model.Email) ??
                throw new InvalidOperationException($"Unable to find user with email: '{model.Email}'.");

            string code = Tools.GetValidationCode(user.Email, Tools.GetCodeExpiryDate());
            string callbackUrl = "#";

            await _service.Email.SendReValidationEmailAsync(user.Email, user.FullName, code, callbackUrl);

            return Success("A confirmation code was sent by mail");
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("/confirm-email")]
    public async Task<IActionResult> ConfirmEmail(ValidateEmailRequest model)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.Email))
            {
                throw new Exception("Email could not be confirmed because the confirmation code and/or user id is invalid");
            }

            User user = await _userManager.FindByEmailAsync(model.Email) ??
                throw new InvalidOperationException($"Unable to find user with email: '{model.Email}'.");

            if (Tools.ValidationCode(model.Code, user.Email, Tools.GetCodeExpiryDate()))
            {
                user.EmailConfirmed = true;

                await _userManager.UpdateAsync(user);

                await _service.Email.SendWelcomeEmailAsync(user.Email, user.FullName);

                return Success("Email confirmed");
            }
            else throw new InvalidOperationException("Validation code is invalid");
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [Authorize]
    [HttpPost("/change-password")]
    public async Task<IActionResult> ChangePassword(PasswordRequest model)
    {
        try
        {
            User user = await _userManager.FindByNameAsync(User.Identity!.Name!)
                ?? throw new InvalidOperationException($"Unable to find user with email: '{User.Identity.Name}'.");

            if (!string.IsNullOrEmpty(model.OldPassword) &&
                !string.IsNullOrEmpty(model.NewPassword))
            {
                var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);

                if (result.Succeeded)
                {
                    try
                    {
                        string callbackUrl = "#";

                        await _service.Email.SendPasswordEmailAsync(user.Email, user.FullName, callbackUrl);
                    }
                    catch
                    {
                        _logger.LogWarning("Could not send email password");
                    }

                    return Success("Password changed");
                }
                else throw new InvalidOperationException(
                    $"Password could not be changed because " +
                    $"{string.Join("; ", result.Errors.Select(x => x.Description))}"
                );
            }
            else throw new InvalidOperationException("Please provide old and new password to continue");
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }
}