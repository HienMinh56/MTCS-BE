using Google.Cloud.Firestore;
using MTCS.Common;
using MTCS.Data;
using MTCS.Data.Models;
using MTCS.Data.Request;
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
        Task<BusinessResult> CreateCustomer(CreateCustomerRequest customer, string userName);
        Task<BusinessResult> UpdateCustomer(string cusId, UpdateCustomerRequest customer, string userName);
        Task<BusinessResult> DeleteCustomer(string customerId, string userName);
    }

    public class CustomerService : ICustomerService
    {
        private readonly UnitOfWork _unitOfWork;

        public CustomerService()
        {
            _unitOfWork ??= new UnitOfWork();
        }

        public async Task<BusinessResult> CreateCustomer(CreateCustomerRequest customer, string userName)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var existingCustomer =  _unitOfWork.CustomerRepository.Get(c => c.Email == customer.Email || c.PhoneNumber == customer.PhoneNumber || c.TaxNumber == customer.TaxNumber);
                if (existingCustomer != null)
                {
                    return new BusinessResult(400, "Email, Phone number, Tax number had used");
                }
                var cusId = await _unitOfWork.CustomerRepository.GenerateCustomerIdAsync();
                var newCustomer = new Customer
                {
                    CustomerId = cusId,
                    CompanyName = customer.CompanyName,
                    Email = customer.Email,
                    PhoneNumber = customer.PhoneNumber,
                    TaxNumber = customer.TaxNumber,
                    Address = customer.Address,
                    CreatedDate = DateTime.Now,
                    CreatedBy = userName
                };

                await _unitOfWork.CustomerRepository.CreateAsync(newCustomer);
                return new BusinessResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, newCustomer);

            }
            catch (Exception ex)
            {
                return new BusinessResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }

        public async Task<BusinessResult> DeleteCustomer(string customerId, string userName)
        {
            try
            {
                var customer = _unitOfWork.CustomerRepository.Get(c => c.CustomerId == customerId);
                if (customer == null)
                {
                    return new BusinessResult(404, "Customer not found");
                }
                customer.DeletedDate = DateTime.Now;
                customer.DeletedBy = userName;
                _unitOfWork.CustomerRepository.Update(customer);
                return new BusinessResult(Const.SUCCESS_DELETE_CODE, Const.SUCCESS_DELETE_MSG, customer);
            }
            catch (Exception ex)
            {
                return new BusinessResult(Const.ERROR_EXCEPTION, ex.Message);
            } 
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

        public async Task<BusinessResult> UpdateCustomer(string cusId, UpdateCustomerRequest customer, string userName)
        {
            try
            {
                var existingCustomer = _unitOfWork.CustomerRepository.Get(c => c.CustomerId == cusId);
                if (existingCustomer == null)
                {
                    return new BusinessResult(400, "Customer not found");
                }

                await _unitOfWork.BeginTransactionAsync();

                existingCustomer.CompanyName = customer.CompanyName;
                existingCustomer.Email = customer.Email;
                existingCustomer.PhoneNumber = customer.PhoneNumber;
                existingCustomer.TaxNumber = customer.TaxNumber;
                existingCustomer.Address = customer.Address;
                existingCustomer.ModifiedDate = DateTime.Now;
                existingCustomer.ModifiedBy = userName;
                _unitOfWork.CustomerRepository.Update(existingCustomer);
                return new BusinessResult(Const.SUCCESS_UPDATE_CODE, Const.SUCCESS_UPDATE_MSG, existingCustomer);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new BusinessResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }
    }
}
