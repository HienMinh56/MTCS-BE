using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MTCS.Data.Request;
using MTCS.Service.Service;

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
            var result = await _contractService.CreateContract(contractRequest, file, "Admin");
            return Ok(result);
        }

        [HttpPost("contractId")]
        public async Task<IActionResult> SendSignedContract( string contractId, string description, string note, IFormFile file)
        {
            var result = await _contractService.SendSignedContract(contractId, description, note, file, "Admin");
            return Ok(result);
        }
    }
}
