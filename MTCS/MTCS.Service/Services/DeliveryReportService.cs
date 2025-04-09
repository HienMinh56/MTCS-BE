using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using MTCS.Data;
using MTCS.Data.Models;
using MTCS.Data.Repository;
using MTCS.Data.Request;
using MTCS.Service.Base;

namespace MTCS.Service.Services
{
    public interface IDeliveryReportService
    {
        Task<BusinessResult> GetDeliveryReport(string? reportId, string? tripId, string? driverId);
        Task<BusinessResult> CreateDeliveryReport(CreateDeliveryReportRequest deliveryReport, List<IFormFile> files, ClaimsPrincipal claims);
        Task<BusinessResult> UpdateDeliveryReport(UpdateDeliveryReportRequest updateDelivery, ClaimsPrincipal claims);
    }
    public class DeliveryReportService : IDeliveryReportService
    {
        private readonly UnitOfWork _unitOfWork;
        private readonly IFirebaseStorageService _firebaseStorageService;
        private readonly INotificationService _notification;

        public DeliveryReportService(UnitOfWork unitOfWork, IFirebaseStorageService firebaseStorageService, INotificationService notification)
        {
            _unitOfWork = unitOfWork;
            _firebaseStorageService = firebaseStorageService;
            _notification = notification;
        }
        #region Get Delivery Report
        public async Task<BusinessResult> GetDeliveryReport(string? reportId, string? tripId, string? driverId)
        {
            try
            {
                var deliveryReports = _unitOfWork.DeliveryReportRepository.GetDeliveryReports(reportId, tripId, driverId);
                return new BusinessResult(200, "Get Delivery Report Success", deliveryReports);
            }
            catch
            {
                return new BusinessResult(500, "Get Delivery Report Failed");
            }
        }
        #endregion

        #region Create Delivery Report
        public async Task<BusinessResult> CreateDeliveryReport(CreateDeliveryReportRequest deliveryReport, List<IFormFile> files, ClaimsPrincipal claims)
        {
            try
            {
                var userId = claims.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                    ?? claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = claims.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                await _unitOfWork.BeginTransactionAsync();

                var trip = _unitOfWork.TripRepository.Get(t => t.TripId == deliveryReport.TripId);
                if (trip == null)
                {
                    return new BusinessResult(404, "Trip not found or trip cannot create delivery report");
                }
                var deliveryReportModel = new DeliveryReport
                {
                    ReportId = Guid.NewGuid().ToString(),
                    TripId = deliveryReport.TripId,
                    Notes = deliveryReport.Notes,
                    ReportBy = userName,
                    ReportTime = DateTime.Now
                };
                await _unitOfWork.DeliveryReportRepository.CreateAsync(deliveryReportModel);
                var savedFiles = new List<DeliveryReportsFile>();

                for (int i = 0; i < files.Count; i++)
                {
                    var file = files[i];
                    var fileUrl = await _firebaseStorageService.UploadImageAsync(file);
                    var fileName = Path.GetFileName(file.FileName);
                    var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    string fileType = GetFileTypeFromExtension(fileExtension);

                    var deliveryReportFile = new DeliveryReportsFile
                    {
                        FileId = Guid.NewGuid().ToString(),
                        ReportId = deliveryReportModel.ReportId,
                        Description = "Delivery Report File",
                        Note = "Delivery Report File",
                        FileName = fileName,
                        FileType = fileType,
                        UploadDate = DateTime.Now,
                        UploadBy = userName,
                        FileUrl = fileUrl,
                        ModifiedDate = DateOnly.FromDateTime(DateTime.Now),
                        ModifiedBy = userName,
                    };

                    await _unitOfWork.DeliveryReportFileRepository.CreateAsync(deliveryReportFile);
                    savedFiles.Add(deliveryReportFile);
                }
                //var order = await _unitOfWork.FuelReportRepository.GetOrderByTripId(deliveryReport.TripId);
                //var userReceiver = order.CreatedBy;
                //await _notification.SendNotificationAsync(userReceiver, "Delivery Report", $"New Delivery Report from {userName}", userName);

                return new BusinessResult(200, "Create Fuel Report Successfully", savedFiles);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new BusinessResult(500, "Create Delivery Report Failed");
            }
        }
        #endregion

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
        #endregion

        #region Update Delivery Report
        public async Task<BusinessResult> UpdateDeliveryReport(UpdateDeliveryReportRequest updateDelivery, ClaimsPrincipal claims)
        {
            try
            {
                var userId = claims.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                    ?? claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = claims.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                var deliveryReportModel = _unitOfWork.DeliveryReportRepository.Get(d => d.ReportId == updateDelivery.ReportId);
                if (deliveryReportModel == null)
                {
                    return new BusinessResult(404, "Delivery Report not found");
                }

                await _unitOfWork.BeginTransactionAsync();
                deliveryReportModel.Notes = updateDelivery.Note;
                _unitOfWork.DeliveryReportRepository.Update(deliveryReportModel);

                var savedFiles = new List<FuelReportFile>();

                if (updateDelivery.FileIdsToRemove != null && updateDelivery.FileIdsToRemove.Count > 0)
                {
                    foreach (var fileId in updateDelivery.FileIdsToRemove)
                    {
                        var file = _unitOfWork.DeliveryReportFileRepository.Get(f => f.FileId == fileId);
                        if (file != null)
                        {
                            await _unitOfWork.DeliveryReportFileRepository.RemoveAsync(file);
                        }
                    }
                }
                if (!updateDelivery.AddedFiles.IsNullOrEmpty())
                {
                    for (int i = 0; i < updateDelivery.AddedFiles.Count; i++)
                    {
                        var file = updateDelivery.AddedFiles[i];
                        var fileUrl = await _firebaseStorageService.UploadImageAsync(file);

                        var fileName = Path.GetFileName(file.FileName);
                        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                        string fileType = GetFileTypeFromExtension(fileExtension);

                        deliveryReportModel.DeliveryReportsFiles.Add(new DeliveryReportsFile
                        {
                            FileId = Guid.NewGuid().ToString(),
                            ReportId = deliveryReportModel.ReportId,
                            Description = "Delivery Report File",
                            Note = "Delivery Report File",
                            FileName = fileName,
                            FileType = fileType,
                            UploadDate = DateTime.Now,
                            UploadBy = userName,
                            FileUrl = fileUrl,
                            ModifiedDate = DateOnly.FromDateTime(DateTime.Now),
                            ModifiedBy = userName,
                        });
                    }

                    await _unitOfWork.DeliveryReportRepository.UpdateAsync(deliveryReportModel);
                }
                //var order = await _unitOfWork.FuelReportRepository.GetOrderByTripId(deliveryReportModel.TripId);
                //var userReceiver = order.CreatedBy;
                //await _notification.SendNotificationAsync(userReceiver, "Delivery Report", $"New Update Delivery Report from {userName}", userName);


                return new BusinessResult(200, "Update Fuel Report Successfully", savedFiles);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new BusinessResult(500, "Update Delivery Report Failed");
            }
        }
        #endregion
    }
}
