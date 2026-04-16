using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FirstReg.Admin.Controllers;

[Authorize]
public class BaseController<T> : Controller where T : BaseController<T>
{
    protected readonly ILogger<T> _logger;
    protected readonly Service _service;

    public BaseController(ILogger<T> logger, Service service)
    {
        _logger = logger;
        _service = service;
    }

    protected string GetReferrerUrl()
    {
        var referrer = Request.Headers["Referrer"].ToString();
        return string.IsNullOrEmpty(referrer) ? "/" : referrer;
    }
}
