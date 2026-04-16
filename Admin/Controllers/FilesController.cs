using FirstReg.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace FirstReg.Admin.Controllers
{
    [Route("files")]
    [Authorize(Roles = Tools.AdminRole)]
    public class FilesController : Controller
    {
        private readonly ILogger<FilesController> _logger;
        private readonly Service _service;

        public FilesController(ILogger<FilesController> logger, Service service)
        {
            _logger = logger;
            _service = service;
        }

        public async Task<IActionResult> Index() => View(await _service.Data.Get<Faq>());
    }
}
