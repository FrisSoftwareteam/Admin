using FirstReg.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace FirstReg.Admin.Controllers
{
    [Route("sysadmin")]
    [Authorize]
    public class SysAdminController : Controller
    {
        private readonly ILogger<SysAdminController> _logger;
        private readonly UserManager<User> _userManager;
        private readonly Service _service;
        private readonly IConfiguration _configuration;

        public SysAdminController(ILogger<SysAdminController> logger,
             UserManager<User> userManager,
            Service service, IConfiguration configuration)
        {
            _logger = logger;
            _userManager = userManager;
            _service = service;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index() =>
            View(await _service.Data.Find<User>(x => x.Type == UserType.SystemAdmin && x.Id > 2));

        [HttpPost("create-account")]
        public async Task<IActionResult> CreateAccount(UserModel model)
        {
            try
            {
                var user = new User
                {
                    FullName = model.FullName.Trim(),
                    UserName = model.Username.Trim(),
                    Email = model.Email.Trim(),
                    PhoneNumber = model.MobileNo.Trim(),
                    Type = UserType.SystemAdmin
                };

                var result = await _userManager.CreateAsync(user);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, Tools.AdminRole);
                    string code = Clear.Tools.StringUtility.GenerateValidationCode(user.Email, Tools.GetCodeExpiryDate(), Tools.Validatekey);
                    string callbackUrl = Url.Action(nameof(AuthController.Reset), "Auth", values: new { email = user.Email, key = code }, protocol: Request.Scheme);
                    await _service.Email.SendPasswordEmailAsync(user.Email, user.FullName, HtmlEncoder.Default.Encode(callbackUrl));
                }

                throw new InvalidOperationException(string.Join(",", result.Errors.Select(x => x.Description)));
            }
            catch (Exception ex)
            {
                TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
            }
            return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
        }

        [HttpPost("switch-role")]
        public async Task<IActionResult> SwitchRole(int id, Roles role, bool status)
        {
            try
            {
                var user = await _service.Data.Get<User>(x => x.Id == id);

                if (user.Type != UserType.SystemAdmin)
                    throw new InvalidOperationException("User must be a system admin to be assigned a role");

                StringBuilder sb = new($"DELETE FROM AccessRoles WHERE (UserId = {id}) AND (Role = {(int)role})");
                if (status) sb.Append($"INSERT INTO AccessRoles (UserId, Role) VALUES ({id}, {(int)role})");

                await _service.Data.ExecuteSql(sb.ToString());

                user = await _service.Data.Get<User>(x => x.Id == id);

                return Ok(new { message = "Status updated", roles = user.RolesString });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
                return StatusCode(StatusCodes.Status500InternalServerError, Clear.Tools.GetAllExceptionMessage(ex));
            }
        }
    }
}
