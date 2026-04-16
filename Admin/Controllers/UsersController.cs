using FirstReg.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirstReg.Admin.Controllers
{
    [Route("users")]
    [Authorize]
    public class UsersController : Controller
    {
        private readonly ILogger<UsersController> _logger;
        private readonly Service _service;

        public UsersController(ILogger<UsersController> logger,
          Service service)
        {
            _logger = logger;
            _service = service;
        }

        public IActionResult Index() => View("Index", Url.Action(nameof(GetShareholderLists)));

        #region spirit

        [HttpGet("list")]
        public async Task<IActionResult> GetShareholderLists(UserType? t)
        {
            try
            {
                StringBuilder sb = new($"SELECT * FROM AspNetUsers WHERE (Id > 0) ");

                if (t != null) sb.Append($"AND (Type = {(int)t}) ");

                var users = await _service.Data.FromSql<User>(sb.ToString());

                return Ok(new
                {
                    data = users.OrderBy(x => x.FullName).Select(x => new[]
                    {
                        x.FullName,
                        x.Email,
                        x.PhoneNumber,
                        x.Type.ToString(),
                        x.Id.ToString()
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
                return StatusCode(StatusCodes.Status500InternalServerError, Clear.Tools.GetAllExceptionMessage(ex));
            }
        }

        [HttpGet("send-confirm")]
        public async Task<IActionResult> SendConfirmEmail(UserType? t)
        {
            StringBuilder builder = new StringBuilder();

            try
            {
                var users = await _service.Data.Find<User>(x => !x.EmailConfirmed);

                if (users.Any())
                {
                    builder.AppendLine($"Sending mail to {users.Count} users.");
                    DateTime date = Convert.ToDateTime(Tools.Now.ToString("dd/MMM/yyy HH:mm")).AddHours(1);

                    foreach (var user in users)
                    {
                        try
                        {
                            string code = Tools.GetValidationCode(user.Email, date);
                            await _service.Email.SendValidationEmailAsync(user.Email, user.FullName, code);

                            builder.AppendLine($"{user.Email}: successful");
                        }
                        catch (Exception ex)
                        {
                            builder.AppendLine($"{user.Email}: failed - {ex.Message}");
                        }
                    }
                }

                builder.AppendLine($"Complete");
            }
            catch (Exception ex)
            {
                builder.AppendLine($"Error occured: {ex.Message}");

                _logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
                return StatusCode(StatusCodes.Status500InternalServerError, builder.ToString());
            }

            return Ok(builder.ToString());
        }

        #endregion
    }
}