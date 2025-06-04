using Microsoft.AspNetCore.Mvc;
using MTCS.Data.Request;
using MTCS.Service.Services;

namespace MTCS.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExpenseReportTypeController : ControllerBase
    {
        private readonly IExpenseReportTypeService _expenseReportTypeService;
        public ExpenseReportTypeController(IExpenseReportTypeService expenseReportTypeService)
        {
            _expenseReportTypeService = expenseReportTypeService;
        }

        [HttpGet("GetAllExpenseReportTypes")]

        public async Task<IActionResult> GetAllExpenseReportTypes()
        {
            var result = await _expenseReportTypeService.GetAllExpenseReportTypes();
            if (result.Status == 200)
            {
                return Ok(result);
            }
            return NotFound(result);
        }

        [HttpGet("GetExpenseReportTypeById/{id}")]
        public async Task<IActionResult> GetExpenseReportTypeById(string id)
        {
            var result = await _expenseReportTypeService.GetExpenseReportTypeById(id);
            if (result.Status == 200)
            {
                return Ok(result);
            }
            return NotFound(result);
        }
        [HttpPost("CreateExpenseReportType")]
        public async Task<IActionResult> CreateExpenseReportType([FromBody] CreateExpenseReportTypeRequest expenseReportType)
        {
            var user = HttpContext.User;
            var result = await _expenseReportTypeService.CreateExpenseReportType(expenseReportType, user);
            if (result.Status == 200)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }
        [HttpPut("UpdateExpenseReportType")]
        public async Task<IActionResult> UpdateExpenseReportType([FromBody] UpdateExpenseReportTypeRequest expenseReportType)
        {
            var user = HttpContext.User;
            var result = await _expenseReportTypeService.UpdateExpenseReportType(expenseReportType, user);
            if (result.Status == 200)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }
        [HttpDelete("DeleteExpenseReportType/{id}")]
        public async Task<IActionResult> DeleteExpenseReportType(string id)
        {
            var user = HttpContext.User;
            var result = await _expenseReportTypeService.DeleteExpenseReportType(id, user);
            if (result.Status == 200)
            {
                return Ok(result);
            }
            return NotFound(result);
        }
    }
}
