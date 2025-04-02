using Google.Cloud.Firestore;
using MTCS.Common;
using MTCS.Data;
using MTCS.Data.Models;
using MTCS.Service.Base;
using MTCS.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCS.Service.Services
{
    public interface ICustomerService
    {
        Task<IBusinessResult> GetAllCustomers(string? customerId, string? companyName);
    }

    public class CustomerService : ICustomerService
    {
        private readonly UnitOfWork _unitOfWork;

        public CustomerService()
        {
            _unitOfWork ??= new UnitOfWork();
        }

        public async Task<IBusinessResult> GetAllCustomers(string? customerId, string? companyName)
        {
            try
            {
                var customers = await _unitOfWork.CustomerRepository.GetAllCustomer(customerId, companyName);
                if (customers == null || !customers.Any())
                {
                    return new BusinessResult(Const.WARNING_NO_DATA_CODE, Const.WARNING_NO_DATA_MSG, new List<IncidentReport>());
                }
                return new BusinessResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, customers);
            }
            catch (Exception ex)
            {
                return new BusinessResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }
    }
}
