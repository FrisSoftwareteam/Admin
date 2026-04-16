using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FirstReg.Mobile.Controllers;

[ApiController]
[AllowAnonymous]
public class HomeController : ControllerBase
{
    [HttpGet("/")]
    public IActionResult Index() => Ok("FRSL Mobile Api");
}