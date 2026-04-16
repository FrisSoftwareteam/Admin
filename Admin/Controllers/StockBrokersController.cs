using FirstReg.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FirstReg.Admin.Controllers
{
    [Route("stockbrokers")]
    [Authorize]
    public class StockBrokersController : Controller
    {
        private readonly ILogger<StockBrokersController> _logger;
        private readonly Service _service;

        public StockBrokersController(ILogger<StockBrokersController> logger, Service service)
        {
            _logger = logger;
            _service = service;
        }
        public async Task<IActionResult> Index() => View(await _service.Data.Get<StockBroker>());
    }
}