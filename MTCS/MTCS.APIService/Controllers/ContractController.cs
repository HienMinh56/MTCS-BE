using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MTCS.Data.Request;
using MTCS.Service.Services;
using System.Security.Claims;

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

        [HttpPost]
        public async Task<IActionResult> CreateContractWithFile([FromForm] ContractRequest contractRequest, IFormFile file)
        {
            var currentUser = HttpContext.User;
            var result = await _contractService.CreateContract(contractRequest, file, currentUser);
            return Ok(result);
        }

        [HttpPost("contractId")]
        public async Task<IActionResult> SendSignedContract( string contractId, string description, string note, IFormFile file)
        {
            var currentUser = HttpContext.User;
            var result = await _contractService.SendSignedContract(contractId, description, note, file, currentUser);
            return Ok(result);
        }
    }
}
