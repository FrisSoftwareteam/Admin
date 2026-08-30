using FirstReg.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FirstReg.Admin.Controllers
{
    [Authorize]
    [Route("fradmin")]
    public class FrAdminController : Controller
    {
        private readonly ILogger<FrAdminController> _logger;
        private readonly UserManager<User> _userManager;
        private readonly Service _service;
        private readonly IConfiguration _configuration;

        public FrAdminController(ILogger<FrAdminController> logger,
                UserManager<User> userManager,
                Service service, IConfiguration configuration)
        {
            _logger = logger;
            _userManager = userManager;
            _service = service;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index() =>
            View(await _service.Data.Find<User>(x => x.Type == UserType.FRAdmin));

        [HttpPost("create-account")]
        public async Task<IActionResult> CreateAccount(UserModel model)
        {
            try
            {
                var user = new User
                {
                    FullName = model.FullName.Trim(),
                    UserName = model.Email.Trim(),
                    Email = model.Email.Trim(),
                    PhoneNumber = model.MobileNo.Trim(),
                    Type = UserType.FRAdmin
                };

                var result = await _userManager.CreateAsync(user);

                if (result.Succeeded)
                {
                    string code = Clear.Tools.StringUtility.GenerateValidationCode(user.Email, Tools.GetCodeExpiryDate(), Tools.Validatekey);
                    await _service.Email.SendPasswordEmailAsync(user.Email, user.FullName,
                        $"{_configuration.GetValue<string>("accessurl")}/reset-password/{user.Email}/{code}");
                }

                throw new InvalidOperationException(string.Join(",", result.Errors.Select(x => x.Description)));
            }
            catch (Exception ex)
            {
                TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
            }
            return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
        }

        [Route("delete-account/{id}")]
        public async Task<IActionResult> DeleteAccount(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);

                var result = await _userManager.DeleteAsync(user);

                if (result.Succeeded)
                {
                    return RedirectToAction(nameof(Index));
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