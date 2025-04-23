using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MTCS.Data.Helpers;
using MTCS.Data.Request;
using MTCS.Service.Services;

namespace MTCS.APIService.Controllers
{
    [Route("api/price-tables")]
    [ApiController]
    public class PriceTableController : ControllerBase
    {
        private readonly IPriceTableService _priceTableService;

        public PriceTableController(IPriceTableService priceTableService)
        {
            _priceTableService = priceTableService;
        }

        [HttpGet("get-list")]
        public async Task<IActionResult> GetPriceTables([FromQuery] int? version = null)
        {
            var result = await _priceTableService.GetPriceTables(version);
            return Ok(result);
        }

        [HttpGet("price-changes/{version}")]
        public async Task<IActionResult> GetPriceChangesInVersion(int version)
        {
            var result = await _priceTableService.GetPriceChangesInVersion(version);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetPriceTableById(string id)
        {
            var result = await _priceTableService.GetPriceTableById(id);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePriceTable([FromBody] List<CreatePriceTableRequest> priceTable)
        {
            var currentUser = User.GetUserName();
            var result = await _priceTableService.CreatePriceTable(priceTable, currentUser);
            return Ok(result);
        }

        [HttpPost("excel")]
        public async Task<IActionResult> ImportPriceTable(IFormFile excelFile)
        {
            var currentUser = User.GetUserName();
            var result = await _priceTableService.ImportPriceTable(excelFile, currentUser);
            return Ok(result);
        }

        [HttpGet("download-template")]
        public async Task<IActionResult> DownloadTemplate()
        {
            var result = await _priceTableService.DownloadPriceTableTemplate();
            if (result.Status == 200)
            {
                var fileContent = (byte[])result.Data;
                return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "PriceTableTemplate.xlsx");
            }
            return StatusCode(result.Status, result.Message);
        }

        [Authorize]
        [HttpPut]
        public async Task<IActionResult> UpdatePriceTable([FromBody] UpdatePriceTableRequest priceTable)
        {
            var userName = User.GetUserName();
            var result = await _priceTableService.UpdatePriceTable(priceTable, userName);
            return Ok(result);
        }

        [HttpDelete]
        public async Task<IActionResult> DeletePriceTable(string id)
        {
            var result = await _priceTableService.DeletePriceTable(id);
            return Ok(result);
        }

        [HttpGet("calculate-price")]
        public async Task<IActionResult> CalculatePrice(double distance, int containerType, int containerSize)
        {
            var result = await _priceTableService.CalculatePrice(distance, containerType, containerSize);
            return Ok(result);
        }
    }
}
