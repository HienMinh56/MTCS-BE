using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MTCS.Common;
using MTCS.Data.Request;
using MTCS.Service.Services;
using System.Security.Claims;
using System.Text.Json;

namespace MTCS.APIService.Controllers
{
    [Route("api/contract")]
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

        [HttpGet("{contractId}/contract-file")]
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



        [HttpPost("send-contract")]
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
    }
}
