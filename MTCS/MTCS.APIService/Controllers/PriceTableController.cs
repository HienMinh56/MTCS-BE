using Microsoft.AspNetCore.Http;
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
        public async Task<IActionResult> GetPriceTables()
        {
            var result = await _priceTableService.GetPriceTables();
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

        [HttpPut]
        public async Task<IActionResult> UpdatePriceTable([FromBody] List<UpdatePriceTableRequest> priceTable)
        {
            var currentUser = HttpContext.User;
            var result = await _priceTableService.UpdatePriceTable(priceTable, currentUser);
            return Ok(result);
        }

        [HttpDelete]
        public async Task<IActionResult> DeletePriceTable(string id)
        {
            var result = await _priceTableService.DeletePriceTable(id);
            return Ok(result);
        }
    }
}
