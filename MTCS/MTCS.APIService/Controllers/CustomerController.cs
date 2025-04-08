using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MTCS.Data.Helpers;
using MTCS.Data.Request;
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
        public async Task<IBusinessResult> GetAllCustomer(string? customerId, string? companyName)
        {
            var result = await _customerService.GetAllCustomers(customerId, companyName);
            return result;
        }

        [HttpPost]
        public async Task<IBusinessResult> CreateCustomer([FromBody] CreateCustomerRequest customer)
        {
            var userName = User.GetUserName();
            var result = await _customerService.CreateCustomer(customer, userName);
            return result;
        }
        [HttpPut("{cusId}")]
        public async Task<IBusinessResult> UpdateCustomer(string cusId, [FromBody] UpdateCustomerRequest customer)
        {
            var userName = User.GetUserName();
            var result = await _customerService.UpdateCustomer(cusId, customer, userName);
            return result;
        }
        [HttpDelete("{customerId}")]
        public async Task<IBusinessResult> DeleteCustomer(string customerId)
        {
            var userName = User.GetUserName();
            var result = await _customerService.DeleteCustomer(customerId, userName);
            return result;
        }
    }
}
