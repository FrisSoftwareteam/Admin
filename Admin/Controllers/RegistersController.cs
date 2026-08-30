using FirstReg.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace FirstReg.Admin.Controllers
{
    [Route("registers")]
    [Authorize]
    public class RegistersController : Controller
    {
        private readonly ILogger<RegistersController> _logger;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly Service _service;
        private readonly IConfiguration _configuration;

        public RegistersController(ILogger<RegistersController> logger,
              SignInManager<User> signInManager,
            UserManager<User> userManager,
            Service service, IConfiguration configuration)
        {
            _logger = logger;
            _userManager = userManager;
            _signInManager = signInManager;
            _service = service;
            _configuration = configuration;
        }
        public async Task<IActionResult> Index() => View(await _service.Data.Get<Register>());

        [HttpGet("{code}")]
        public async Task<IActionResult> Details(int code)
        {
            try
            {
                return View(await _service.Data.Get<Register>(x => x.Id == code));
            }
            catch (Exception ex)
            {
                TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
                return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
            }
        }

        [HttpPost("create-account")]
        public async Task<IActionResult> CreateAccount(int id, string email, string phone)
        {
            try
            {
                var registers = await _service.Data.Find<Register>(x => x.Id == id);

                if (!registers.Any())
                    throw new InvalidOperationException("Register not found, please select a register first");

                var register = registers.First();

                register.Name = register.Name.Trim();
                register.Email = email.Trim();
                register.Phone = phone.Trim();

                var user = new User
                {
                    FullName = register.Name,
                    UserName = email.Trim(),
                    Email = email.Trim(),
                    PhoneNumber = phone.Trim(),
                    Type = UserType.CompanySec
                };

                user.Register = register;

                var result = await _userManager.CreateAsync(user);

                if (result.Succeeded)
                {
                    string message = $"The account was successfully created for {register.Name}";

                    try
                    {
                        string code = Clear.Tools.StringUtility.GenerateValidationCode(user.Email, Tools.GetCodeExpiryDate(), Tools.Validatekey);
                        await _service.Email.SendResetPasswordEmailAsync(user.Email, user.FullName,
                            $"{_configuration.GetValue<string>("accessurl")}/reset/{user.Email}/{code}");
                    }
                    catch (Exception eex)
                    {
                        message = $"{message}, but a password email could not be sent because {eex.Message}";
                    }

                    TempData["success"] = message;
                }

                throw new InvalidOperationException(string.Join(",", result.Errors.Select(x => x.Description)));
            }
            catch (Exception ex)
            {
                TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
            }
            return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
        }
    }
}