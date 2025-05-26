using Google.Cloud.Firestore;
using MTCS.Common;
using MTCS.Data;
using MTCS.Data.DTOs;
using MTCS.Data.Enums;
using MTCS.Data.Models;
using MTCS.Data.Request;
using MTCS.Data.Response;
using MTCS.Service.Base;
using MTCS.Service.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace MTCS.Service.Services
{
    public interface IIncidentReportsService
    {
        Task<IBusinessResult> GetAllIncidentReports(string? driverId, string? tripId, string? reportId);
        Task<IBusinessResult> CreateIncidentReport(CreateIncidentReportRequest request, ClaimsPrincipal claims);
        //Task<IBusinessResult> UpdateIncidentReport(UpdateIncidentReportRequest request, ClaimsPrincipal claims);
        Task<IBusinessResult> AddBillIncidentReport(AddIncidentReportImageRequest request, ClaimsPrincipal claims);
        Task<IBusinessResult> AddExchangeShipIncidentReport(AddIncidentReportImageRequest request, ClaimsPrincipal claims);
        Task<IBusinessResult> UpdateIncidentReportFileInfo(List<IncidentReportsFileUpdateRequest> requests, ClaimsPrincipal claims);
        Task<IBusinessResult> UpdateIncidentReportMO(UpdateIncidentReportMORequest updateIncidentReportMO, ClaimsPrincipal claims);
        Task<IBusinessResult> ResolvedReport(ResolvedIncidentReportRequest incidentReportRequest, ClaimsPrincipal claims);
        Task<IBusinessResult> DeleteIncidentReportById(string reportId);
        Task<ApiResponse<List<IncidentReportAdminDTO>>> GetIncidentReportsByVehicleAsync(string vehicleId, int vehicleType);
    }

    public class IncidentReportsService : IIncidentReportsService
    {
        private readonly UnitOfWork _unitOfWork;
        private readonly IFirebaseStorageService _firebaseStorageService;
        private readonly INotificationService _notification;
        private readonly FirestoreDb _firestoreDb;
        private readonly ITripService _tripService;
        public IncidentReportsService(IFirebaseStorageService firebaseStorageService, FirestoreDb firestoreDb, INotificationService notification, ITripService tripService)
        {
            _unitOfWork ??= new UnitOfWork();
            _firebaseStorageService = firebaseStorageService;
            _notification = notification;
            _tripService = tripService;
        }

        #region GetFileTypeFromExtension
        // Helper method to determine file type from extension
        private string GetFileTypeFromExtension(string extension)
        {
            switch (extension.ToLowerInvariant())
            {
                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".webp":
                case ".gif":
                    return "Image";
                default:
                    return "Unknown";
            }
        }
        #endregion

        #region Get Incident Report
        public async Task<IBusinessResult> GetAllIncidentReports(string? driverId, string? tripId, string? reportId)
        {
            try
            {
                var incidents = await _unitOfWork.IncidentReportsRepository.GetAllIncidentReport(driverId, tripId, reportId);
                if (incidents == null)
                {
                    return new BusinessResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, new List<IncidentReport>());
                }
                return new BusinessResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, incidents);
            }
            catch (Exception ex)
            {
                return new BusinessResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }
        #endregion

        #region Create Incident Report with Incident Image
        public async Task<IBusinessResult> CreateIncidentReport(CreateIncidentReportRequest request, ClaimsPrincipal claims)
        {
            var userId = claims.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = claims.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

            var reportId = await _unitOfWork.IncidentReportsRepository.GetNextIncidentCodeAsync();

            await _unitOfWork.IncidentReportsRepository.CreateAsync(new IncidentReport
            {
                ReportId = reportId,
                TripId = request.TripId,
                ReportedBy = userName,
                IncidentType = request.IncidentType,
                Description = request.Description,
                IncidentTime = DateTime.Now,
                Location = request.Location,
                Type = request.Type,
                VehicleType = request.VehicleType, // 1 : Tractor, 2: Trailer
                Status = request.Status,
                CreatedDate = DateTime.Now
            });

            try
            {
                if (request.Image is not null)
                {
                    string FileId;
                    List<IncidentReportsFile> images;
                    int iId = 1;
                    for (int i = 0; i < request.Image.Count; i++)
                    {
                        var image = request.Image[i];
                        var FileExtension = Path.GetExtension(image.FileName).ToLowerInvariant();
                        images = await _unitOfWork.IncidentReportsFileRepository.GetImagesOfIncidentReport();
                        iId = images.Count + 1;
                        if (_unitOfWork.IncidentReportsFileRepository.Get(i => i.FileId == $"{Const.INCIDENTREPORTIMAGE}{iId.ToString("D6")}") is not null)
                        {
                            iId = await _unitOfWork.IncidentReportsFileRepository.FindEmptyPositionWithBinarySearch(images, 1, iId, Const.INCIDENTREPORTIMAGE, Const.INCIDENTREPORTIMAGE_INDEX);
                        }

                        FileId = $"{Const.INCIDENTREPORTIMAGE}{Guid.NewGuid().ToString()}";
                        await _unitOfWork.IncidentReportsFileRepository.CreateAsync(new IncidentReportsFile
                        {
                            FileId = FileId,
                            ReportId = reportId,
                            FileName = Path.GetFileName(image.FileName),
                            FileType = GetFileTypeFromExtension(FileExtension),
                            FileUrl = await _firebaseStorageService.UploadImageAsync(image),
                            UploadDate = DateTime.Now,
                            UploadBy = userName,
                            Type = 1 // Incident Image
                        });
                    }
                }
                var trip = _unitOfWork.TripRepository.Get(t => t.TripId == request.TripId);
                var existingTrip = _unitOfWork.TripRepository.Get(t => t.TripId == request.TripId);
                var order = _unitOfWork.OrderRepository.Get(i => i.OrderId == existingTrip.OrderId);
                var owner = order.CreatedBy;
                if (request.Type == 1)
                {
                    trip.Status = "delaying";
                    await _unitOfWork.TripRepository.UpdateAsync(trip);

                    var tripStatusHistory = new TripStatusHistory
                    {
                        HistoryId = Guid.NewGuid().ToString(),
                        TripId = request.TripId,
                        StatusId = "delaying",
                        StartTime = DateTime.Now
                    };
                    await _unitOfWork.TripStatusHistoryRepository.CreateAsync(tripStatusHistory);
                    // Gửi thông báo sau khi cập nhật thành công
                    await _notification.SendNotificationAsync(owner, "Báo cáo sự cố đã được tạo", $"Báo cáo sự cố vừa được tạo cho chuyến {existingTrip.TripId} bởi {userName}.", userName);
                }
                else if (request.Type == 2)
                {
                    trip.Status = "canceled";
                    trip.EndTime = DateTime.Now;
                    if (request.VehicleType == 1)
                    {
                        var tractor = _unitOfWork.TractorRepository.Get(t => t.TractorId == trip.TractorId);
                        tractor.Status = VehicleStatus.Inactive.ToString();
                        await _unitOfWork.TractorRepository.UpdateAsync(tractor);
                    }
                    if (request.VehicleType == 2)
                    {
                        var trailer = _unitOfWork.TrailerRepository.Get(r => r.TrailerId == trip.TrailerId);
                        trailer.Status = VehicleStatus.Inactive.ToString();
                        await _unitOfWork.TrailerRepository.UpdateAsync(trailer);
                    }
                    await _unitOfWork.TripRepository.UpdateAsync(trip);

                    var tripStatusHistory = new TripStatusHistory
                    {
                        HistoryId = Guid.NewGuid().ToString(),
                        TripId = request.TripId,
                        StatusId = "canceled",
                        StartTime = DateTime.Now
                    };
                    await _unitOfWork.TripStatusHistoryRepository.CreateAsync(tripStatusHistory);
                    // Gửi thông báo sau khi cập nhật thành công
                    await _notification.SendNotificationAsync(owner, "Báo cáo sự cố đã được tạo", $"Báo cáo sự cố vừa được tạo cho chuyến {existingTrip.TripId} bởi {userName}.", userName);
                }
                else if (request.Type == 3)
                {
                    trip.Status = "delaying";
                    await _unitOfWork.TripRepository.UpdateAsync(trip);

                    var tripStatusHistory = new TripStatusHistory
                    {
                        HistoryId = Guid.NewGuid().ToString(),
                        TripId = request.TripId,
                        StatusId = "delaying",
                        StartTime = DateTime.Now
                    };
                    await _unitOfWork.TripStatusHistoryRepository.CreateAsync(tripStatusHistory);
                    // Gửi thông báo sau khi cập nhật thành công
                    await _notification.SendNotificationAsync(owner, "Báo cáo sự cố đã được tạo", $"Báo cáo sự cố vừa được tạo cho chuyến {existingTrip.TripId} bởi {userName}.", userName);
                }
            }
            catch (Exception ex)
            {
                await DeleteIncidentReportById(reportId);
                return new BusinessResult(Const.FAIL_CREATE_CODE, Const.FAIL_CREATE_MSG, ex.Message);
            }

            var result = await _unitOfWork.IncidentReportsRepository.GetImagesByReportId(reportId);

            if (result is not null)
            {
                return new BusinessResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, result);
            }
            else
            {
                return new BusinessResult(Const.FAIL_CREATE_CODE, Const.FAIL_CREATE_MSG, result);
            }
        }
        #endregion

        #region Add Bill Image for Incident Report
        public async Task<IBusinessResult> AddBillIncidentReport(AddIncidentReportImageRequest request, ClaimsPrincipal claims)
        {
            var userId = claims.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = claims.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

            string FileId;
            List<IncidentReportsFile> images;
            int iId = 1;
            for (int i = 0; i < request.Image.Count; i++)
            {
                var image = request.Image[i];
                var FileExtension = Path.GetExtension(image.FileName).ToLowerInvariant();
                images = await _unitOfWork.IncidentReportsFileRepository.GetImagesOfIncidentReport();
                iId = images.Count + 1;
                if (_unitOfWork.IncidentReportsFileRepository.Get(i => i.FileId == $"{Const.INCIDENTREPORTIMAGE}{iId.ToString("D6")}") is not null)
                {
                    iId = await _unitOfWork.IncidentReportsFileRepository.FindEmptyPositionWithBinarySearch(images, 1, iId, Const.INCIDENTREPORTIMAGE, Const.INCIDENTREPORTIMAGE_INDEX);
                }

                FileId = $"{Const.INCIDENTREPORTIMAGE}{Guid.NewGuid().ToString()}";
                await _unitOfWork.IncidentReportsFileRepository.CreateAsync(new IncidentReportsFile
                {
                    FileId = FileId,
                    ReportId = request.ReportId,
                    FileName = Path.GetFileName(image.FileName),
                    FileType = GetFileTypeFromExtension(FileExtension),
                    FileUrl = await _firebaseStorageService.UploadImageAsync(image),
                    UploadDate = DateTime.Now,
                    UploadBy = userName,
                    Type = 2 // Bill Image
                });
            }

            var result = await _unitOfWork.IncidentReportsRepository.GetImagesByReportId(request.ReportId);
            if (result is null)
            {
                return new BusinessResult(Const.FAIL_CREATE_CODE, Const.FAIL_CREATE_MSG, new IncidentReport());
            }

            if (result is not null)
            {
                return new BusinessResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, result);
            }
            else
            {
                return new BusinessResult(Const.FAIL_CREATE_CODE, Const.FAIL_CREATE_MSG, result);
            }
        }
        #endregion

        #region Add Exchange Image for Incident Report
        public async Task<IBusinessResult> AddExchangeShipIncidentReport(AddIncidentReportImageRequest request, ClaimsPrincipal claims)
        {
            var userId = claims.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = claims.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

            string FileId;
            List<IncidentReportsFile> images;
            int iId = 1;
            for (int i = 0; i < request.Image.Count; i++)
            {
                var image = request.Image[i];
                var FileExtension = Path.GetExtension(image.FileName).ToLowerInvariant();
                images = await _unitOfWork.IncidentReportsFileRepository.GetImagesOfIncidentReport();
                iId = images.Count + 1;
                if (_unitOfWork.IncidentReportsFileRepository.Get(i => i.FileId == $"{Const.INCIDENTREPORTIMAGE}{iId.ToString("D6")}") is not null)
                {
                    iId = await _unitOfWork.IncidentReportsFileRepository.FindEmptyPositionWithBinarySearch(images, 1, iId, Const.INCIDENTREPORTIMAGE, Const.INCIDENTREPORTIMAGE_INDEX);
                }

                FileId = $"{Const.INCIDENTREPORTIMAGE}{Guid.NewGuid().ToString()}";
                await _unitOfWork.IncidentReportsFileRepository.CreateAsync(new IncidentReportsFile
                {
                    FileId = FileId,
                    ReportId = request.ReportId,
                    FileName = Path.GetFileName(image.FileName),
                    FileType = GetFileTypeFromExtension(FileExtension),
                    FileUrl = await _firebaseStorageService.UploadImageAsync(image),
                    UploadDate = DateTime.Now,
                    UploadBy = userName,
                    Type = 3 // Bill Image
                });
            }

            var result = await _unitOfWork.IncidentReportsRepository.GetImagesByReportId(request.ReportId);
            if (result is null)
            {
                return new BusinessResult(Const.FAIL_CREATE_CODE, Const.FAIL_CREATE_MSG, new IncidentReport());
            }

            if (result is not null)
            {
                return new BusinessResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, result);
            }
            else
            {
                return new BusinessResult(Const.FAIL_CREATE_CODE, Const.FAIL_CREATE_MSG, result);
            }
        }
        #endregion

        //#region Update Incident Report
        //public async Task<IBusinessResult> UpdateIncidentReport(UpdateIncidentReportRequest request, ClaimsPrincipal claims)
        //{
        //    var userId = claims.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //    var userName = claims.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

        //    var incident = await _unitOfWork.IncidentReportsRepository.GetIncidentReportDetails(request.ReportId);
        //    if (incident is null)
        //    {
        //        return new BusinessResult(Const.WARNING_NO_DATA_CODE, Const.WARNING_NO_DATA_MSG, new IncidentReport());
        //    }
        //    else
        //    {
        //        incident.ReportId = request.ReportId is null ? incident.ReportId : request.ReportId;
        //        incident.TripId = request.TripId is null ? incident.TripId : request.TripId;
        //        incident.ReportedBy = userName;
        //        incident.IncidentType = request.IncidentType is null ? incident.IncidentType : request.IncidentType;
        //        incident.Description = request.Description is null ? incident.Description : request.Description;
        //        incident.Location = request.Location is null ? incident.Location : request.Location;
        //        incident.Type = request.Type is null ? incident.Type : request.Type;
        //        incident.VehicleType = request.VehicleType is null ? incident.VehicleType : request.VehicleType;
        //        incident.Status = request.Status is null ? incident.Status : request.Status;
        //        incident.HandledBy = request.HandledBy is null ? incident.HandledBy : request.HandledBy;
        //        incident.HandledTime = request.HandledTime != default ? request.HandledTime : incident.HandledTime;
        //        incident.ResolutionDetails = request.ResolutionDetails != default ? request.ResolutionDetails : incident.ResolutionDetails;
        //    }

        //    // Xử lý xóa ảnh bị loại bỏ
        //    if (request.RemovedImage is not [])
        //    {
        //        IncidentReportsFile? image;
        //        foreach (var url in request.RemovedImage)
        //        {
        //            if ((image = await _unitOfWork.IncidentReportsFileRepository.GetImageByUrl(url)) is not null && image.ReportId == incident.ReportId)
        //            {
        //                await _firebaseStorageService.DeleteImageAsync(_firebaseStorageService.ExtractImageNameFromUrl(url));
        //                await _unitOfWork.IncidentReportsFileRepository.RemoveAsync(image);
        //            }
        //        }
        //    }

        //    // Xử lý thêm ảnh mới
        //    if (request.AddedImage is not null)
        //    {
        //        string FileId;
        //        List<IncidentReportsFile> images;
        //        int id;
        //        for (int i = 0; i < request.AddedImage.Count; i++)
        //        {
        //            var image = request.AddedImage[i];
        //            var imageType = request.ImageType[i];
        //            var FileExtension = Path.GetExtension(image.FileName).ToLowerInvariant();
        //            images = await _unitOfWork.IncidentReportsFileRepository.GetImagesOfIncidentReport();
        //            id = images.Count + 1;
        //            if (_unitOfWork.IncidentReportsFileRepository.Get(i => i.FileId == $"{Const.INCIDENTREPORTIMAGE}{id.ToString("D6")}") is not null)
        //            {
        //                id = await _unitOfWork.IncidentReportsFileRepository.FindEmptyPositionWithBinarySearch(images, 1, id, Const.INCIDENTREPORTIMAGE, Const.INCIDENTREPORTIMAGE_INDEX);
        //            }
        //            FileId = $"{Const.INCIDENTREPORTIMAGE}{Guid.NewGuid().ToString()}";
        //            await _unitOfWork.IncidentReportsFileRepository.CreateAsync(new IncidentReportsFile
        //            {
        //                FileId = FileId,
        //                ReportId = incident.ReportId,
        //                FileName = Path.GetFileName(image.FileName),
        //                FileType = GetFileTypeFromExtension(FileExtension),
        //                FileUrl = await _firebaseStorageService.UploadImageAsync(image),
        //                UploadDate = DateTime.Now,
        //                UploadBy = userName,
        //                Type = imageType // Set the ImageType
        //            });
        //        }
        //    }

        //    var result = await _unitOfWork.IncidentReportsRepository.UpdateAsync(incident);
        //    var data = await _unitOfWork.IncidentReportsRepository.GetImagesByReportId(incident.ReportId);
        //    var order = await _unitOfWork.FuelReportRepository.GetOrderByTripId(incident.TripId);
        //    var owner = order.CreatedBy;
        //    if (result > 0)
        //    {
        //        // Gửi thông báo sau khi cập nhật thành công
        //        await _notification.SendNotificationAsync(owner, "Incident Report Updated", $"New Incident report Updated by {data.ReportedBy}.", data.ReportedBy);
        //        return new BusinessResult(Const.SUCCESS_UPDATE_CODE, Const.SUCCESS_UPDATE_MSG, data);
        //    }
        //    else
        //    {
        //        return new BusinessResult(Const.FAIL_UPDATE_CODE, Const.FAIL_UPDATE_MSG, data);
        //    }
        //}
        //#endregion

        #region Add Image Detail Report
        public async Task<IBusinessResult> UpdateIncidentReportFileInfo(List<IncidentReportsFileUpdateRequest> requests, ClaimsPrincipal claims)
        {
            var userId = claims.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = claims.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

            try
            {
                foreach (var request in requests)
                {
                    var existingFile = _unitOfWork.IncidentReportsFileRepository.Get(i => i.FileId == request.FileId);
                    if (existingFile == null)
                    {
                        return new BusinessResult(Const.WARNING_NO_DATA_CODE, Const.WARNING_NO_DATA_MSG, new IncidentReportsFile());
                    }

                    existingFile.Description = request.Description;
                    existingFile.Note = request.Note;
                    existingFile.UploadBy = userName;
                    existingFile.Type = request.Type;
                    existingFile.UploadDate = DateTime.Now;

                    await _unitOfWork.IncidentReportsFileRepository.UpdateAsync(existingFile);
                }

                return new BusinessResult(Const.SUCCESS_UPDATE_CODE, Const.SUCCESS_UPDATE_MSG, requests);
            }
            catch (Exception ex)
            {
                return new BusinessResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }
        #endregion

        #region Delete Incident Report
        public async Task<IBusinessResult> DeleteIncidentReportById(string reportId)
        {
            try
            {
                var incident = _unitOfWork.IncidentReportsRepository.Get(i => i.ReportId == reportId);
                var images = (await _unitOfWork.IncidentReportsFileRepository.GetImagesOfIncidentReport()).Where(i => i.ReportId == reportId).ToList();
                if (incident == null)
                {
                    return new BusinessResult(Const.WARNING_NO_DATA_CODE, Const.WARNING_NO_DATA_MSG, new IncidentReport());
                }
                else
                {
                    foreach (var image in images)
                    {
                        await _firebaseStorageService.DeleteImageAsync(_firebaseStorageService.ExtractImageNameFromUrl(image.FileUrl));
                        await _unitOfWork.IncidentReportsFileRepository.RemoveAsync(image);
                    }
                    var result = await _unitOfWork.IncidentReportsRepository.RemoveAsync(incident);
                    if (result)
                        return new BusinessResult(Const.SUCCESS_DELETE_CODE, Const.SUCCESS_DELETE_MSG, incident);
                    else
                        return new BusinessResult(Const.FAIL_DELETE_CODE, Const.FAIL_DELETE_MSG, incident);
                }
            }
            catch (Exception ex)
            {
                return new BusinessResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }
        #endregion

        #region Update Status Incident Report
        public async Task<IBusinessResult> ResolvedReport(ResolvedIncidentReportRequest incidentReportRequest, ClaimsPrincipal claims)
        {
            try
            {
                var userId = claims.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = claims.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                var incident = _unitOfWork.IncidentReportsRepository.Get(i => i.ReportId == incidentReportRequest.reportId);
                var trip = _unitOfWork.TripRepository.Get(t => t.TripId == incident.TripId);
                var driver = _unitOfWork.DriverRepository.Get(d => d.DriverId == trip.DriverId);

                if (incident == null)
                {
                    return new BusinessResult(Const.WARNING_NO_DATA_CODE, Const.WARNING_NO_DATA_MSG, new IncidentReport());
                }
                else
                {
                    if (incident.Type == 1) // Delay incident
                    {
                        incident.Status = "Resolved";
                        incident.ResolutionDetails = incidentReportRequest.ResolutionDetails;
                        incident.HandledBy = userName;
                        incident.HandledTime = DateTime.Now;

                        // Restore the previous status of the trip
                        var previousStatus = await _unitOfWork.TripStatusHistoryRepository.GetPreviousStatusOfTrip(trip.TripId);
                        if (previousStatus != null)
                        {
                            trip.Status = previousStatus.StatusId;

                            // Record the status change in history
                            await _unitOfWork.TripStatusHistoryRepository.CreateAsync(new TripStatusHistory
                            {
                                HistoryId = Guid.NewGuid().ToString(),
                                TripId = trip.TripId,
                                StatusId = previousStatus.StatusId,
                                StartTime = DateTime.Now
                            });
                        }
                        else
                        {
                            // If no previous status found, set to a default like "is_delivering"
                            return new BusinessResult(Const.FAIL_UPDATE_CODE, Const.FAIL_UPDATE_MSG, incident);
                        }

                        await _unitOfWork.TripRepository.UpdateAsync(trip);
                    }
                    else if (incident.Type == 2) // Cancellation incident
                    {
                        incident.Status = "Resolved";
                        incident.ResolutionDetails = incidentReportRequest.ResolutionDetails;
                        incident.HandledBy = userName;
                        incident.HandledTime = DateTime.Now;
                        if (await _unitOfWork.TripRepository.IsDriverHaveProcessTrip(trip.DriverId, trip.TripId) == false)
                        {
                            driver.Status = 1; // Free
                        }
                        trip.EndTime = DateTime.Now;
                        if (incident.VehicleType == 1)
                        {
                            var tractor = _unitOfWork.TractorRepository.Get(t => t.TractorId == trip.TractorId);
                            tractor.Status = VehicleStatus.Active.ToString();
                            await _unitOfWork.TractorRepository.UpdateAsync(tractor);
                        }
                        if (incident.VehicleType == 2)
                        {
                            var trailer = _unitOfWork.TrailerRepository.Get(r => r.TrailerId == trip.TrailerId);
                            trailer.Status = VehicleStatus.Active.ToString();
                            await _unitOfWork.TrailerRepository.UpdateAsync(trailer);
                        }

                        await _unitOfWork.DriverRepository.UpdateAsync(driver);
                        await _unitOfWork.TripRepository.UpdateAsync(trip);
                    }
                    else if (incident.Type == 3) // Cancellation incident
                    {
                        incident.Status = "Resolved";
                        incident.ResolutionDetails = incidentReportRequest.ResolutionDetails;
                        incident.HandledBy = userName;
                        incident.HandledTime = DateTime.Now;
                        // Restore the previous status of the trip
                        var previousStatus = await _unitOfWork.TripStatusHistoryRepository.GetPreviousStatusOfTrip(trip.TripId);
                        if (previousStatus != null)
                        {
                            trip.Status = previousStatus.StatusId;

                            // Record the status change in history
                            await _unitOfWork.TripStatusHistoryRepository.CreateAsync(new TripStatusHistory
                            {
                                HistoryId = Guid.NewGuid().ToString(),
                                TripId = trip.TripId,
                                StatusId = previousStatus.StatusId,
                                StartTime = DateTime.Now
                            });
                        }
                        else
                        {
                            // If no previous status found, set to a default like "is_delivering"
                            return new BusinessResult(Const.FAIL_UPDATE_CODE, Const.FAIL_UPDATE_MSG, incident);
                        }

                        await _unitOfWork.TripRepository.UpdateAsync(trip);
                    }

                    var result = await _unitOfWork.IncidentReportsRepository.UpdateAsync(incident);
                    var data = await _unitOfWork.IncidentReportsRepository.GetImagesByReportId(incident.ReportId);
                    var existingTrip = _unitOfWork.TripRepository.Get(t => t.TripId == incident.TripId);
                    var order = _unitOfWork.OrderRepository.Get(i => i.OrderId == trip.OrderId);
                    var owner = order.CreatedBy;
                    if (result > 0)
                    {
                        // Gửi thông báo sau khi cập nhật thành công
                        await _notification.SendNotificationAsync(owner, "Báo cáo sự cố đã được cập nhật", $"Sự cố {incident.ReportId} của {existingTrip.TripId} đã được giải quyết bởi {data.ReportedBy}.", data.ReportedBy);
                        return new BusinessResult(Const.SUCCESS_UPDATE_CODE, Const.SUCCESS_UPDATE_MSG, data);
                    }
                    else
                    {
                        return new BusinessResult(Const.FAIL_UPDATE_CODE, Const.FAIL_UPDATE_MSG, data);
                    }
                }
            }
            catch (Exception ex)
            {
                return new BusinessResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }
        #endregion

        #region Update Incident Report
        public async Task<IBusinessResult> UpdateIncidentReportMO(UpdateIncidentReportMORequest updateIncidentReportMO, ClaimsPrincipal claims)
        {
            try
            {
                var userId = claims.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = claims.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

                var incident = await _unitOfWork.IncidentReportsRepository.GetIncidentReportDetails(updateIncidentReportMO.ReportId);
                var trip = _unitOfWork.TripRepository.Get(t => t.TripId == incident!.TripId);
                if (incident is null)
                {
                    return new BusinessResult(Const.WARNING_NO_DATA_CODE, Const.WARNING_NO_DATA_MSG, new IncidentReport());
                }
                int? previousVehicleType = incident.VehicleType;


                incident.ReportId = updateIncidentReportMO.ReportId is null ? incident.ReportId : updateIncidentReportMO.ReportId;
                incident.ReportedBy = userName;
                incident.IncidentType = updateIncidentReportMO.IncidentType is null ? incident.IncidentType : updateIncidentReportMO.IncidentType;
                incident.Description = updateIncidentReportMO.Description is null ? incident.Description : updateIncidentReportMO.Description;
                incident.Location = updateIncidentReportMO.Location is null ? incident.Location : updateIncidentReportMO.Location;
                incident.Type = updateIncidentReportMO.Type is null ? incident.Type : updateIncidentReportMO.Type;
                incident.VehicleType = updateIncidentReportMO.VehicleType is null ? incident.VehicleType : updateIncidentReportMO.VehicleType;

                if (previousVehicleType != incident.VehicleType && incident.Type >= 2)
                {
                    if (previousVehicleType == 1 && incident.VehicleType == 2)
                    {
                        var tractor = _unitOfWork.TractorRepository.Get(t => t.TractorId == trip.TractorId);
                        if (tractor != null && tractor.Status == VehicleStatus.Inactive.ToString())
                        {
                            tractor.Status = VehicleStatus.Active.ToString();
                            await _unitOfWork.TractorRepository.UpdateAsync(tractor);
                        }

                        var trailer = _unitOfWork.TrailerRepository.Get(r => r.TrailerId == trip.TrailerId);
                        if (trailer != null)
                        {
                            trailer.Status = VehicleStatus.Inactive.ToString();
                            await _unitOfWork.TrailerRepository.UpdateAsync(trailer);
                        }
                    }
                    else if (previousVehicleType == 2 && incident.VehicleType == 1)
                    {
                        var trailer = _unitOfWork.TrailerRepository.Get(r => r.TrailerId == trip.TrailerId);
                        if (trailer != null && trailer.Status == VehicleStatus.Inactive.ToString())
                        {
                            trailer.Status = VehicleStatus.Active.ToString();
                            await _unitOfWork.TrailerRepository.UpdateAsync(trailer);
                        }

                        var tractor = _unitOfWork.TractorRepository.Get(t => t.TractorId == trip.TractorId);
                        if (tractor != null)
                        {
                            tractor.Status = VehicleStatus.Inactive.ToString();
                            await _unitOfWork.TractorRepository.UpdateAsync(tractor);
                        }
                    }
                }


                if (updateIncidentReportMO.RemovedImage != null && updateIncidentReportMO.RemovedImage.Count > 0)
                {
                    foreach (var fileId in updateIncidentReportMO.RemovedImage)
                    {
                        var file = _unitOfWork.IncidentReportsFileRepository.Get(f => f.FileId == fileId);
                        if (file != null)
                        {
                            await _unitOfWork.IncidentReportsFileRepository.RemoveAsync(file);
                        }
                    }
                }

                // Xử lý thêm ảnh mới
                if (updateIncidentReportMO.AddedImage is not null)
                {
                    string FileId;
                    List<IncidentReportsFile> images;
                    int id;
                    for (int i = 0; i < updateIncidentReportMO.AddedImage.Count; i++)
                    {
                        var image = updateIncidentReportMO.AddedImage[i];
                        var FileExtension = Path.GetExtension(image.FileName).ToLowerInvariant();
                        images = await _unitOfWork.IncidentReportsFileRepository.GetImagesOfIncidentReport();
                        id = images.Count + 1;
                        if (_unitOfWork.IncidentReportsFileRepository.Get(i => i.FileId == $"{Const.INCIDENTREPORTIMAGE}{id.ToString("D6")}") is not null)
                        {
                            id = await _unitOfWork.IncidentReportsFileRepository.FindEmptyPositionWithBinarySearch(images, 1, id, Const.INCIDENTREPORTIMAGE, Const.INCIDENTREPORTIMAGE_INDEX);
                        }
                        FileId = $"{Const.INCIDENTREPORTIMAGE}{Guid.NewGuid().ToString()}";
                        await _unitOfWork.IncidentReportsFileRepository.CreateAsync(new IncidentReportsFile
                        {
                            FileId = FileId,
                            ReportId = incident.ReportId,
                            FileName = Path.GetFileName(image.FileName),
                            FileType = GetFileTypeFromExtension(FileExtension),
                            FileUrl = await _firebaseStorageService.UploadImageAsync(image),
                            UploadDate = DateTime.Now,
                            UploadBy = userName,
                            Type = 1 // Set the ImageType
                        });
                    }
                }

                var result = await _unitOfWork.IncidentReportsRepository.UpdateAsync(incident);
                var data = await _unitOfWork.IncidentReportsRepository.GetImagesByReportId(incident.ReportId);
                var order = _unitOfWork.OrderRepository.Get(i => i.OrderId == trip.OrderId);
                var owner = order.CreatedBy;
                if (result > 0)
                {
                    // Gửi thông báo sau khi cập nhật thành công
                    await _notification.SendNotificationAsync(owner, $"Thay đổi của báo cáo sự cố của {trip.TripId}", $"Sự cố của chuyến {trip.TripId} đã được điều chỉnh bởi {data.ReportedBy}.", data.ReportedBy);
                    return new BusinessResult(Const.SUCCESS_UPDATE_CODE, Const.SUCCESS_UPDATE_MSG, data);
                }
                else
                {
                    return new BusinessResult(Const.FAIL_UPDATE_CODE, Const.FAIL_UPDATE_MSG, data);
                }
            }
            catch (Exception ex)
            {
                return new BusinessResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }
        #endregion

        public async Task<ApiResponse<List<IncidentReportAdminDTO>>> GetIncidentReportsByVehicleAsync(string vehicleId, int vehicleType)
        {
            try
            {
                var incidents = await _unitOfWork.IncidentReportsRepository.GetIncidentsByVehicleAsync(vehicleId, vehicleType);

                var incidentDtos = incidents.Select(incident => new IncidentReportAdminDTO
                {
                    ReportId = incident.ReportId,
                    TripId = incident.TripId,
                    IncidentType = incident.IncidentType,
                    Description = incident.Description,
                    IncidentTime = incident.IncidentTime,
                    Status = incident.Status,
                    ResolutionDetails = incident.ResolutionDetails,
                    HandledBy = incident.HandledBy,
                    HandledTime = incident.HandledTime,
                    ReportedBy = incident.ReportedBy,
                    Files = incident.IncidentReportsFiles?.ToList() ?? new List<IncidentReportsFile>()
                }).ToList();

                return new ApiResponse<List<IncidentReportAdminDTO>>(
                    success: true,
                    data: incidentDtos,
                    message: "Get incident history successfully",
                    messageVN: "Lấy dữ liệu sự cố thành công",
                    errors: null
                );
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<IncidentReportAdminDTO>>(
                    success: false,
                    data: null,
                    message: "Get incident failed",
                    messageVN: "Lỗi khi lấy dữ liệu sự cố",
                    errors: ex.Message
                );
            }
        }

    }
}