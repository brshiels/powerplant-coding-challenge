using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using powerplant.Dtos;
using powerplant.Utils;
using System.Collections.Generic;
using System.Text.Json;

namespace powerplant.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PowerplantController : ControllerBase
    {
        private readonly ILogger<PowerplantController> _logger;

        public PowerplantController(ILogger<PowerplantController> logger)
        {
            _logger = logger;
        }

        [HttpPost("productionplan")]
        public ActionResult<IEnumerable<Reply>> Post(Request request)
        {
            _logger.LogDebug("[productionplan] begin");
            _logger.LogDebug("[request] = {}", JsonSerializer.Serialize(request));

            IEnumerable<Reply> replies;
            FuelUtil fuelUtil = new FuelUtil();
            replies = fuelUtil.ComputeFuel(request);

            _logger.LogDebug("[reply] = {}", JsonSerializer.Serialize(replies));
            _logger.LogDebug("[productionplan] end");

            return Ok(replies);
        }
    }
}