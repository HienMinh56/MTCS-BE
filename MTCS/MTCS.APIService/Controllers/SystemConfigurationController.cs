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

        [HttpPut("{configId}")]
        public async Task<IActionResult> UpdateSystemConfiguration(int configId, [FromForm] string configValue)
        {
            var updatedBy = User.Identity?.Name ?? "System";

            var result = await _systemConfigurationServices.UpdateSystemConfigurationAsync(configId, configValue, updatedBy);

            if (result.Status < 0)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSystemConfigurations()
        {
            var result = await _systemConfigurationServices.GetAllSystemConfigurationsAsync();

            if (result.Status < 0)
                return BadRequest(result);

            return Ok(result);
        }
    }
}