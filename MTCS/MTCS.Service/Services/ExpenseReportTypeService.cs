using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using MTCS.Data;
using MTCS.Data.Request;
using MTCS.Service.Base;

namespace MTCS.Service.Services
{
    public interface IExpenseReportTypeService
    {
        Task<BusinessResult> GetAllExpenseReportTypes();
        Task<BusinessResult> GetExpenseReportTypeById(string id);
        Task<BusinessResult> CreateExpenseReportType(CreateExpenseReportTypeRequest expenseReportType, ClaimsPrincipal claims);
        Task<BusinessResult> UpdateExpenseReportType(UpdateExpenseReportTypeRequest expenseReportType, ClaimsPrincipal claims);
        Task<BusinessResult> DeleteExpenseReportType(string id, ClaimsPrincipal claims);
    }

    public class ExpenseReportTypeService : IExpenseReportTypeService
    {
        private readonly UnitOfWork _unitOfWork;
        public ExpenseReportTypeService(UnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<BusinessResult> CreateExpenseReportType(CreateExpenseReportTypeRequest expenseReportType, ClaimsPrincipal claims)
        {
            try
            {
                var userName = claims.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                var existingReportType = _unitOfWork.ExpenseReportTypeRepository.Get(e => e.ReportTypeId == expenseReportType.ReportTypeId);
                if (existingReportType != null)
                {
                    return new BusinessResult(400, "Expense report type id already exists");
                }
                var newExpenseReportType = new Data.Models.ExpenseReportType
                {
                    ReportTypeId = expenseReportType.ReportTypeId,
                    ReportType = expenseReportType.ReportType,
                    IsActive = expenseReportType.IsActive ?? 1,
                    CreatedBy = userName,
                    CreatedDate = DateTime.Now
                };
                await _unitOfWork.ExpenseReportTypeRepository.CreateAsync(newExpenseReportType);
                return new BusinessResult(200, "Success", newExpenseReportType);
            }
            catch (Exception ex)
            {
                return new BusinessResult(500, "Error", ex.Message);
            }
        }

        public async Task<BusinessResult> DeleteExpenseReportType(string id, ClaimsPrincipal claims)
        {
            try
            {
                var userName = claims.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                var expenseReportType = _unitOfWork.ExpenseReportTypeRepository.Get(e => e.ReportTypeId == id);
                if (expenseReportType == null)
                {
                    return new BusinessResult(404, "Expense report type not found");
                }
                expenseReportType.IsActive = 0;
                expenseReportType.ModifiedDate = DateTime.Now;
                expenseReportType.ModifiedBy = userName;
                await _unitOfWork.ExpenseReportTypeRepository.UpdateAsync(expenseReportType);
                return new BusinessResult(200, "Success", expenseReportType);
            }
            catch (Exception ex)
            {
                return new BusinessResult(500, "Error", ex.Message);
            }
        }
        public async Task<BusinessResult> GetAllExpenseReportTypes()
        {
            var expenseReportTypes = _unitOfWork.ExpenseReportTypeRepository.GetAll();
            if (expenseReportTypes == null || !expenseReportTypes.Any())
            {
                return new BusinessResult(404, "No expense report types found");
            }
            return new BusinessResult(200, "Success", expenseReportTypes);
        }

        public async Task<BusinessResult> GetExpenseReportTypeById(string id)
        {
            var expenseReportType = _unitOfWork.ExpenseReportTypeRepository.Get(e => e.ReportTypeId == id);
            if (expenseReportType == null)
            {
                return new BusinessResult(404, "Expense report type not found");
            }
            return new BusinessResult(200, "Success", expenseReportType);
        }

        public async Task<BusinessResult> UpdateExpenseReportType(UpdateExpenseReportTypeRequest expenseReportType, ClaimsPrincipal claims)
        {
            try
            {
                var existingExpenseReportType = _unitOfWork.ExpenseReportTypeRepository.Get(e => e.ReportTypeId == expenseReportType.ReportTypeId);
                if (existingExpenseReportType == null)
                {
                    return new BusinessResult(404, "Expense report type not found");
                }
                existingExpenseReportType.ReportType = expenseReportType.ReportType;
                existingExpenseReportType.IsActive = expenseReportType.IsActive;
                existingExpenseReportType.ModifiedDate = DateTime.Now;
                _unitOfWork.ExpenseReportTypeRepository.Update(existingExpenseReportType);
                return new BusinessResult(200, "Success", existingExpenseReportType);
            }
            catch (Exception ex)
            {
                return new BusinessResult(500, "Error", ex.Message);
            }
        }
    }
}
