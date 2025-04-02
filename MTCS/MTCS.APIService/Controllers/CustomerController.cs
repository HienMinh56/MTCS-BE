using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MTCS.Service.Base;
using MTCS.Service.Services;

namespace MTCS.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerService _customerService;

        public CustomerController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        [HttpGet]
        public async Task<IBusinessResult> GetAllCustomer(string? customerId, string? companyNmae)
        {
            var result = await _customerService.GetAllCustomers(customerId, companyNmae);
            return result;
        }
    }
}
