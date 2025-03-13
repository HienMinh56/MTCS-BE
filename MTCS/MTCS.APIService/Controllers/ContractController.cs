using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MTCS.Common;
using MTCS.Data.Request;
using MTCS.Service.Services;
using System.Security.Claims;
using System.Text.Json;

namespace MTCS.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContractController : ControllerBase
    {
        private readonly IContractService _contractService;

        public ContractController(IContractService contractService)
        {
            _contractService = contractService;
        }


        [HttpGet]
        public async Task<IActionResult> GetContracts()
        {
            var currentUser = HttpContext.User;
            var result = await _contractService.GetContract();
            return Ok(result);
        }

        [HttpGet("{contractId}")]
        public async Task<IActionResult> GetContract(string contractId)
        {
            var result = await _contractService.GetContract(contractId);
            return Ok(result);
        }

        [HttpGet("{contractId}/contractFile")]
        public async Task<IActionResult> GetContractFile(string contractId)
        {
            var result = await _contractService.GetContractFiles(contractId);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateContractWithFile([FromForm] ContractRequest contractRequest,[FromForm] List<string> descriptions,[FromForm] List<string> notes,[FromForm] List<IFormFile> files)
        {
            var currentUser = HttpContext.User;

            if (files.Count != descriptions.Count || files.Count != notes.Count)
            {
                return BadRequest("Số lượng files, descriptions và notes phải bằng nhau.");
            }

            var result = await _contractService.CreateContract(contractRequest, files, descriptions, notes, currentUser);
            return Ok(result);
        }



        [HttpPost("sendContract")]
        public async Task<IActionResult> SendSignedContract([FromForm] string contractId,[FromForm] List<string> descriptions,[FromForm] List<string> notes,[FromForm] List<IFormFile> files)
        {
            var currentUser = HttpContext.User;

            
            var result = await _contractService.SendSignedContract(contractId, descriptions, notes, files, currentUser);
            return Ok(result);
        }


        [HttpPut]
        public async Task<IActionResult> UpdateContract([FromForm] UpdateContractRequest model)
        {
            var currentUser = HttpContext.User;

            
            var result = await _contractService.UpdateContractAsync(model, currentUser);
            return Ok(result);
        }

        [HttpPost("download-selected-zip")]
        public async Task<IActionResult> DownloadSelectedFilesAsZip([FromBody] List<string> fileIds)
        {
            var result = await _contractService.DownloadSelectedFilesAsZip(fileIds);

            if (result.Status != 400)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            if (result.Data is string zipBase64) // Dữ liệu trả về là Base64 string
            {
                var zipBytes = Convert.FromBase64String(zipBase64);
                var zipFileName = $"ContractFile.zip"; // 🔹 Đặt tên ở API

                return File(zipBytes, "application/zip", zipFileName);
            }

            return BadRequest(new { success = false, message = "Invalid data format" });
        }



    }
}
