using FirstReg.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace FirstReg.Admin.Controllers;

[Authorize]
[Route("form-responses")]
public class FormResponsesController(ILogger<FormResponsesController> logger, Service service) : Controller
{
    private readonly ILogger<FormResponsesController> _logger = logger;

    [HttpGet]
    public async Task<IActionResult> Index()
    => View(await service.Data.Get<FormResponse>());

    private async Task<FormResponse> GetFormResponse(string code)
    => (await service.Data.Get<FormResponse>(x => x.UniqueKey.ToLower() == code.ToLower())) ??
        throw new InvalidOperationException($"Form response with the code {code} was not found");

    [HttpGet("{code}")]
    public async Task<IActionResult> Details(string code)
    {
        try
        {
            return View(await GetFormResponse(code));
        }
        catch (Exception ex)
        {
            TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost("update/{code?}")]
    public async Task<IActionResult> Update(FormResponseUpdateModel model, string code)
    {
        try
        {
            if (model.Id == 0)
                throw new InvalidOperationException("Please select a form response");

            var response = await service.Data.Get<FormResponse>(x => x.Id == model.Id) ??
                throw new InvalidOperationException($"Form response with the code {code} was not found");

            response.Processed = true;
            await service.Data.UpdateAsync(response);

            TempData["success"] = "Successful";
            return RedirectToAction(nameof(Details), new { code = response.UniqueKey });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
            TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
            return View(model);
        }
    }
}