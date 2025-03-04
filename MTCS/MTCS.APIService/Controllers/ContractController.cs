using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        public async Task<IActionResult> CreateContractWithFile([FromForm] ContractRequest contractRequest, List<IFormFile> files)
        {
            var currentUser = HttpContext.User;
            var result = await _contractService.CreateContract(contractRequest, files, currentUser);
            return Ok(result);
        }

        [HttpPost("{contractId}")]
        public async Task<IActionResult> SendSignedContract(string contractId, string description, string note, List<IFormFile> files)
        {
            var currentUser = HttpContext.User;
            var result = await _contractService.SendSignedContract(contractId, description, note, files, currentUser);
            return Ok(result);
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateContract([FromForm] UpdateContractRequest model)
        {
            var currentUser = HttpContext.User;
            var result = await _contractService.UpdateContractAsync(model, currentUser);

            return Ok(result);
        }
    }
}
