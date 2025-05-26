using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IdentityModel.Tokens.Jwt;
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
    public interface IFuelReportService
    {
        Task<BusinessResult> GetFuelReport(string? reportId , string? tripId , string? driverId );
        Task<BusinessResult> UpdateFuelReport(UpdateFuelReportRequest updateFuel, ClaimsPrincipal claims);
        Task<BusinessResult> CreateFuelReport(CreateFuelReportRequest createFuel, List<IFormFile> files, ClaimsPrincipal claims);
    }
    //public class FuelReportService : IFuelReportService
    //{
    //    private readonly UnitOfWork _unitOfWork;
    //    private readonly IFirebaseStorageService _firebaseStorageService;
    //    private readonly INotificationService _notification;
    //    public FuelReportService(UnitOfWork unitOfWork, IFirebaseStorageService firebaseStorageService, INotificationService notification)
    //    {
    //        _unitOfWork = unitOfWork;
    //        _firebaseStorageService = firebaseStorageService;
    //        _notification = notification;
    //    }

        //#region Create Fuel report
        //public async Task<BusinessResult> CreateFuelReport(CreateFuelReportRequest createFuel, List<IFormFile> files, ClaimsPrincipal claims)
        //{
        //    try
        //    {
        //        var userId = claims.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
        //            ?? claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //        var userName = claims.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
        //        var minStatus = _unitOfWork.DeliveryStatusRepository.Get(s => s.StatusIndex == 0);
        //        var trip = _unitOfWork.TripRepository.Get(t => t.TripId == createFuel.TripId && t.Status != $"{minStatus.StatusId}");
        //        if (trip == null)
        //        {
        //            return new BusinessResult(404, "Cannot find Trip!!!");
        //        }
        //        await _unitOfWork.BeginTransactionAsync();
        //        var fuelReport = new FuelReport
        //        {
        //            ReportId = "FUEL" + Guid.NewGuid().ToString("N").Substring(0, 10),
        //            TripId = createFuel.TripId,
        //            RefuelAmount = createFuel.RefuelAmount,
        //            FuelCost = createFuel.FuelCost,
        //            Location = createFuel.Location,
        //            ReportBy = userName,
        //            ReportTime = DateTime.Now,
        //        };

        //        await _unitOfWork.FuelReportRepository.CreateAsync(fuelReport);

        //        var savedFiles = new List<FuelReportFile>();

        //        for (int i = 0; i < files.Count; i++)
        //        {
        //            var file = files[i];
        //            var fileUrl = await _firebaseStorageService.UploadImageAsync(file);
        //            var fileName = Path.GetFileName(file.FileName);
        //            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        //            string fileType = GetFileTypeFromExtension(fileExtension);

        //            var fuelReportFile = new FuelReportFile
        //            {
        //                FileId = Guid.NewGuid().ToString(),
        //                ReportId = fuelReport.ReportId,
        //                Description = "Fuel Report File",
        //                Note = "Fuel Report File",
        //                FileName = fileName,
        //                FileType = fileType,
        //                UploadDate = DateTime.Now,
        //                UploadBy = userName,
        //                FileUrl = fileUrl,
        //                ModifiedDate = DateOnly.FromDateTime(DateTime.Now),
        //                ModifiedBy = userName,
        //            };


        //            await _unitOfWork.FuelReportFileRepository.CreateAsync(fuelReportFile);
        //            savedFiles.Add(fuelReportFile);
        //        }
                //var order = await _unitOfWork.FuelReportRepository.GetOrderByTripId(createFuel.TripId);
                //var userReceiver = order.CreatedBy;
                //await _notification.SendNotificationAsync(userReceiver, "Fuel Report", $"New Fuel Report from {userName}", userName);

        //        return new BusinessResult(200, "Create Fuel Report Successfully", savedFiles);
        //    }
        //    catch
        //    {
        //        await _unitOfWork.RollbackTransactionAsync();
        //        return new BusinessResult(500, "Internal Server Error");
        //    }


        //}
        //#endregion

        //#region Support Read extension file
        ///// <summary>
        ///// Get extension file to assign into field File Type
        ///// </summary>
        ///// <param name="extension"></param>
        ///// <returns></returns>
        //private string GetFileTypeFromExtension(string extension)
        //{
        //    switch (extension.ToLowerInvariant())
        //    {
        //        case ".pdf":
        //            return "PDF Document";
        //        case ".doc":
        //        case ".docx":
        //            return "Word Document";
        //        case ".xls":
        //        case ".xlsx":
        //            return "Excel Spreadsheet";
        //        case ".ppt":
        //        case ".pptx":
        //            return "PowerPoint Presentation";
        //        case ".jpg":
        //        case ".jpeg":
        //        case ".png":
        //        case ".gif":
        //            return "Image";
        //        case ".txt":
        //            return "Text Document";
        //        case ".zip":
        //        case ".rar":
        //            return "Archive";
        //        default:
        //            return "Unknown";
        //    }
        //}
        //#endregion

        //#region Update Fuel Report
        //public async Task<BusinessResult> UpdateFuelReport(UpdateFuelReportRequest updateFuel, ClaimsPrincipal claims)
        //{
        //    try
        //    {
        //        var userId = claims.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
        //            ?? claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //        var userName = claims.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

        //        await _unitOfWork.BeginTransactionAsync();

        //        var fuelReport = _unitOfWork.FuelReportRepository.Get(f => f.ReportId == updateFuel.ReportId);
        //        if (fuelReport == null)
        //        {
        //            return new BusinessResult(404, "Cannot find Fuel Report!!!");
        //        }

        //        fuelReport.RefuelAmount = updateFuel.RefuelAmount;
        //        fuelReport.FuelCost = updateFuel.FuelCost;
        //        fuelReport.Location = updateFuel.Location;

        //        await _unitOfWork.FuelReportRepository.UpdateAsync(fuelReport);

        //        var savedFiles = new List<FuelReportFile>();

        //        if (updateFuel.FileIdsToRemove != null && updateFuel.FileIdsToRemove.Count > 0)
        //        {
        //            foreach (var fileId in updateFuel.FileIdsToRemove)
        //            {
        //                var file = _unitOfWork.FuelReportFileRepository.Get(f => f.FileId == fileId);
        //                if (file != null)
        //                {
        //                    await _unitOfWork.FuelReportFileRepository.RemoveAsync(file);
        //                }
        //            }
        //        }
        //        if (!updateFuel.AddedFiles.IsNullOrEmpty())
        //        {
        //            for (int i = 0; i < updateFuel.AddedFiles.Count; i++)
        //            {
        //                var file = updateFuel.AddedFiles[i];
        //                var fileUrl = await _firebaseStorageService.UploadImageAsync(file);

        //                var fileName = Path.GetFileName(file.FileName);
        //                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        //                string fileType = GetFileTypeFromExtension(fileExtension);

        //                fuelReport.FuelReportFiles.Add(new FuelReportFile
        //                {
        //                    FileId = Guid.NewGuid().ToString(),
        //                    ReportId = fuelReport.ReportId,
        //                    Description = "Fuel Report File",
        //                    Note = "Fuel Report File",
        //                    FileName = fileName,
        //                    FileType = fileType,
        //                    UploadDate = DateTime.Now,
        //                    UploadBy = userName,
        //                    FileUrl = fileUrl,
        //                    ModifiedDate = DateOnly.FromDateTime(DateTime.Now),
        //                    ModifiedBy = userName,
        //                });
        //            }

        //            await _unitOfWork.FuelReportRepository.UpdateAsync(fuelReport);
        //        }
        //        //var order = await _unitOfWork.FuelReportRepository.GetOrderByTripId(fuelReport.TripId);
        //        //var userReceiver = order.CreatedBy;
        //        //await _notification.SendNotificationAsync(userReceiver, "Fuel Report", $"New Update Fuel Report from {userName}", userName);


        //        return new BusinessResult(200, "Update Fuel Report Successfully", savedFiles);
        //    }
        //    catch
        //    {
        //        await _unitOfWork.RollbackTransactionAsync();
        //        return new BusinessResult(500, "Internal Server Error");
        //    }
        //}
        //#endregion

        //#region Get Fuel Report
        //public async Task<BusinessResult> GetFuelReport(string? reportId, string? tripId , string? driverId)
        //{
        //    try
        //    {
        //        var fuelReports = _unitOfWork.FuelReportRepository.GetFuelReports(reportId, tripId, driverId);


        //        return new BusinessResult(200, "Get Fuel Report Successfully", fuelReports);
        //    }
        //    catch
        //    {
        //        return new BusinessResult(500, "Internal Server Error");
        //    }
        //}
        //#endregion
    }
//}
