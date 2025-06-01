using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using MTCS.Common;
using MTCS.Data;
using MTCS.Data.Models;
using MTCS.Data.Request;
using MTCS.Service.Base;

namespace MTCS.Service.Services
{

    public interface IExpenseReportService
    {
        Task<BusinessResult> GetAllExpenseReports(string driverId = null, string orderid = null, string tripId = null, string reportId = null, int? isPay = null);
        Task<BusinessResult> GetExpenseReportById(string id);
        Task<BusinessResult> CreateExpenseReport(CreateExpenseReportRequest expenseReport, IFormFileCollection files, ClaimsPrincipal claims);
        Task<BusinessResult> UpdateExpenseReport(UpdateExpenseReportRequest expenseReport, ClaimsPrincipal claims);
        //Task<BusinessResult> DeleteExpenseReport(string id);
        Task<BusinessResult> ToggleIsPayAsync(string expenId, ClaimsPrincipal claims);

    }
    public class ExpenseReportService : IExpenseReportService
    {
        private readonly UnitOfWork _unitOfWork;
        private readonly IFirebaseStorageService _firebaseStorageService;

        public ExpenseReportService(UnitOfWork unitOfWork, IFirebaseStorageService firebaseStorageService)
        {
            _unitOfWork = unitOfWork;
            _firebaseStorageService = firebaseStorageService;
        }

        public async Task<BusinessResult> GetExpenseReportById(string id)
        {
            var expenseReport = _unitOfWork.ExpenseReportRepository.GetById(id);
            if (expenseReport == null)
            {
                return  new BusinessResult(404, "Expense report not found");
            }
            return new BusinessResult(200, "Success", expenseReport);
        }

        public async Task<BusinessResult> CreateExpenseReport(CreateExpenseReportRequest expenseReport, IFormFileCollection files, ClaimsPrincipal claims)
        {
            try
            {
                var userName = claims.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

                var newExpenseReport = new ExpenseReport
                {
                    ReportId = "EXPEN" + Guid.NewGuid().ToString("N").Substring(0, 10),
                    TripId = expenseReport.TripId,
                    ReportTypeId = expenseReport.ReportTypeId,
                    Cost = expenseReport.Cost,
                    Location = expenseReport.Location,
                    ReportTime = DateTime.Now,
                    ReportBy = userName,
                    IsPay = expenseReport.IsPay,
                    Description = expenseReport.Description,
                };

                if (files != null && files.Count > 0)
                {
                    foreach (var file in files)
                    {
                        var imageUrl = await _firebaseStorageService.UploadImageAsync(file);
                        var reportFile = new ExpenseReportFile
                        {
                            FileId = Guid.NewGuid().ToString(),
                            ReportId = newExpenseReport.ReportId,
                            Description = "Expense Report File",
                            Note = "Uploaded via create",
                            FileUrl = imageUrl,
                            FileName = file.FileName,
                            FileType = file.ContentType,
                            UploadDate = DateTime.UtcNow,
                            UploadBy = userName,
                        };
                        newExpenseReport.ExpenseReportFiles.Add(reportFile);
                    }
                }

                await _unitOfWork.ExpenseReportRepository.CreateAsync(newExpenseReport);
                return new BusinessResult(200, "Create Success", newExpenseReport);
            }
            catch
            {
                return new BusinessResult(500, "Create Failed");
            }
        }

        public async Task<BusinessResult> UpdateExpenseReport(UpdateExpenseReportRequest model, ClaimsPrincipal claims)
        {
            try
            {
                var userName = claims.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

                await _unitOfWork.BeginTransactionAsync();

                var existingReport = await _unitOfWork.ExpenseReportRepository.GetByIdAsync(model.ReportId);
                if (existingReport == null)
                    return new BusinessResult(404, "Expense report not found");

                // Cập nhật thông tin cơ bản
                existingReport.ReportTypeId = model.ReportTypeId;
                existingReport.Cost = model.Cost;
                existingReport.Location = model.Location;
                existingReport.IsPay = model.IsPay;
                existingReport.Description = model.Description;

                await _unitOfWork.ExpenseReportRepository.UpdateAsync(existingReport);

                // Xóa file nếu có yêu cầu
                if (model.FileIdsToRemove?.Any() == true)
                {
                    foreach (var fileId in model.FileIdsToRemove)
                    {
                        var file = await _unitOfWork.ExpenseReportFileRepository.GetByIdAsync(fileId);
                        if (file != null)
                        {
                            await _unitOfWork.ExpenseReportFileRepository.RemoveAsync(file);
                        }
                    }
                }

                // Gán cứng giá trị file mới
                if (model.AddedFiles != null && model.AddedFiles.Count > 0)
                {
                    for (int i = 0; i < model.AddedFiles.Count; i++)
                    {
                        var file = model.AddedFiles[i];
                        var fileUrl = await _firebaseStorageService.UploadImageAsync(file);

                        var fileName = Path.GetFileName(file.FileName);
                        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                        var fileType = GetFileTypeFromExtension(fileExtension);

                        var expenseFile = new ExpenseReportFile
                        {
                            FileId = Guid.NewGuid().ToString(),
                            ReportId = existingReport.ReportId,
                            FileName = fileName,
                            FileType = fileType,
                            FileUrl = fileUrl,
                            Description = "Expense Report File",
                            Note = "Uploaded via update",
                            UploadDate = DateTime.UtcNow,
                            UploadBy = userName,
                            ModifiedBy = userName,
                            ModifiedDate = DateOnly.FromDateTime(DateTime.UtcNow)
                        };

                        await _unitOfWork.ExpenseReportFileRepository.CreateAsync(expenseFile);
                    }
                }

                return new BusinessResult(200, "Update Success", existingReport);
            }
            catch (Exception ex)
            {
                return new BusinessResult(500, "Update Failed", ex.Message);
            }
        }

        #region Support Read extension file
        /// <summary>
        /// Get extension file to assign into field File Type
        /// </summary>
        /// <param name="extension"></param>
        /// <returns></returns>
        private string GetFileTypeFromExtension(string extension)
        {
            switch (extension.ToLowerInvariant())
            {
                case ".pdf":
                    return "PDF Document";
                case ".doc":
                case ".docx":
                    return "Word Document";
                case ".xls":
                case ".xlsx":
                    return "Excel Spreadsheet";
                case ".ppt":
                case ".pptx":
                    return "PowerPoint Presentation";
                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".gif":
                    return "Image";
                case ".txt":
                    return "Text Document";
                case ".zip":
                case ".rar":
                    return "Archive";
                default:
                    return "Unknown";
            }
        }

        public Task<BusinessResult> GetAllExpenseReports(string driverId = null, string orderid = null, string tripId = null, string reportId = null, int? isPay = null)
        {
            var expenseReports = _unitOfWork.ExpenseReportRepository.GetExpenseReports(driverId, orderid, tripId, reportId, isPay);
            if (expenseReports == null || !expenseReports.Any())
            {
                return Task.FromResult(new BusinessResult(404, "No expense reports found"));
            }
            return Task.FromResult(new BusinessResult(200, "Success", expenseReports));
        }
        #endregion

        //public Task<BusinessResult> DeleteExpenseReport(string id)
        //{
        //    try
        //    {
        //        var expenseReport = _unitOfWork.ExpenseReportRepository.GetById(id);
        //        if (expenseReport == null)
        //        {
        //            return Task.FromResult(new BusinessResult(404, "Expense report not found"));
        //        }
        //        _unitOfWork.ExpenseReportRepository.Remove(expenseReport);
        //        return Task.FromResult(new BusinessResult(200, "Delete Success"));
        //    }
        //    catch (Exception ex)
        //    {
        //        return Task.FromResult(new BusinessResult(500, "Delete Failed", ex.Message));
        //    }
        //}

        public async Task<BusinessResult> ToggleIsPayAsync(string expenId, ClaimsPrincipal claims)
        {
            try
            {
                var userName = claims.FindFirst(ClaimTypes.Name)?.Value ?? "Staff";

                await _unitOfWork.BeginTransactionAsync();

                var expen = await _unitOfWork.ExpenseReportRepository.GetByIdAsync(expenId);

                if (expen == null)
                    return new BusinessResult(Const.FAIL_READ_CODE, Const.FAIL_READ_MSG); 

                expen.IsPay = expen.IsPay == 0 ? 1 : expen.IsPay;
                await _unitOfWork.ExpenseReportRepository.UpdateAsync(expen);

                await _unitOfWork.CommitTransactionAsync();

                return new BusinessResult(Const.SUCCESS_UPDATE_CODE, Const.SUCCESS_UPDATE_MSG);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(); // Quay lại nếu có lỗi
                return new BusinessResult(Const.FAIL_UPDATE_CODE, Const.FAIL_UPDATE_MSG);
            }
        }
    }
}
