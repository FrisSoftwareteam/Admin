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
    [Route("sections")]
    [Authorize]
    public class FaqSectionsController : Controller
    {
        private readonly ILogger<FaqSectionsController> _logger;
        private readonly Service _service;

        public FaqSectionsController(ILogger<FaqSectionsController> logger, Service service)
        {
            _logger = logger;
            _service = service;
        }
    }
}
