using FirstReg.Mobile.Core.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace FirstReg.Mobile.Controllers;

public class BaseController : ControllerBase
{
    protected readonly ILogger _logger;

    public BaseController(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger(GetType());
    }

    protected IActionResult HandleException(Exception ex)
    {
        _logger.LogError(Clear.Tools.GetAllExceptionMessage(ex));

        return StatusCode(
            StatusCodes.Status500InternalServerError,
            GenericResponse<string>.CreateFailure(ex.Message)
        );
    }

    protected IActionResult Success<T>(T response)
    {
        return Ok(GenericResponse<T>.CreateSuccess(response, "Login was successful"));
    }
}