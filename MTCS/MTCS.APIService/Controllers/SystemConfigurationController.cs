using Microsoft.AspNetCore.Mvc;
using MTCS.Data.Request;
using MTCS.Service.Services;

namespace MTCS.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SystemConfigurationController : ControllerBase
    {
        private readonly ISystemConfigurationServices _systemConfigurationServices;

        public SystemConfigurationController(ISystemConfigurationServices systemConfigurationServices)
        {
            _systemConfigurationServices = systemConfigurationServices;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateSystemConfiguration([FromForm] CreateSystemConfigurationRequestModel request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { Message = "Invalid input data." });
            }

            var result = await _systemConfigurationServices.CreateSystemConfigurationAsync(request, User);
            return Ok(result);
        }
    }
}