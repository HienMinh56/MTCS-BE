using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using MTCS.Common;
using MTCS.Data;
using MTCS.Data.Enums;
using MTCS.Data.Models;
using MTCS.Data.Repository;
using MTCS.Data.Request;
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
        Task<IBusinessResult> UpdateIncidentReport(UpdateIncidentReportRequest request, ClaimsPrincipal claims);
        Task<IBusinessResult> AddBillIncidentReport(AddIncidentReportImageRequest request, ClaimsPrincipal claims);
        Task<IBusinessResult> AddExchangeShipIncidentReport(AddIncidentReportImageRequest request, ClaimsPrincipal claims);
        Task<IBusinessResult> UpdateIncidentReportFileInfo(List<IncidentReportsFileUpdateRequest> requests, ClaimsPrincipal claims);
        Task<IBusinessResult> UpdateIncidentReportMO(UpdateIncidentReportMORequest updateIncidentReportMO, ClaimsPrincipal claims);
        Task<IBusinessResult> ResolvedReport(ResolvedIncidentReportRequest incidentReportRequest, ClaimsPrincipal claims);
        Task<IBusinessResult> DeleteIncidentReportById(string reportId);
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
                if (incidents == null || !incidents.Any())
                {
                    return new BusinessResult(Const.WARNING_NO_DATA_CODE, Const.WARNING_NO_DATA_MSG, new List<IncidentReport>());
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
        /// <summary>
        /// Create incident report for a trip
        /// </summary>
        /// <author name="Đoàn Lê Hiển Minh"></author>
        /// <returns></returns>
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
                }
                else if (request.Type == 2)
                {
                    trip.Status = "canceled";
                    await _unitOfWork.TripRepository.UpdateAsync(trip);

                    var tripStatusHistory = new TripStatusHistory
                    {
                        HistoryId = Guid.NewGuid().ToString(),
                        TripId = request.TripId,
                        StatusId = "canceled",
                        StartTime = DateTime.Now
                    };
                    await _unitOfWork.TripStatusHistoryRepository.CreateAsync(tripStatusHistory);
                }
            }
            catch (Exception ex)
            {
                await DeleteIncidentReportById(reportId);
                return new BusinessResult(Const.FAIL_CREATE_CODE, Const.FAIL_CREATE_MSG, ex.Message);
            }

            var result = await _unitOfWork.IncidentReportsRepository.GetImagesByReportId(reportId);
            var order = await _unitOfWork.FuelReportRepository.GetOrderByTripId(request.TripId);
            var owner = order.CreatedBy;
            if (result is null)
            {
                return new BusinessResult(Const.FAIL_CREATE_CODE, Const.FAIL_CREATE_MSG, new IncidentReport());
            }

            if (result is not null)
            {
                // Gửi thông báo sau khi tạo thành công
                await _notification.SendNotificationAsync(owner, "Incident Report Created", $"New Incident Report Created from {result.ReportedBy}.", result.ReportedBy);
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

        #region Update Incident Report
        /// <summary>
        /// Update incident report for a trip
        /// </summary>
        /// <author name="Đoàn Lê Hiển Minh"></author>
        /// <returns></returns>
        public async Task<IBusinessResult> UpdateIncidentReport(UpdateIncidentReportRequest request, ClaimsPrincipal claims)
        {
            var userId = claims.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = claims.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

            var incident = await _unitOfWork.IncidentReportsRepository.GetIncidentReportDetails(request.ReportId);
            if (incident is null)
            {
                return new BusinessResult(Const.WARNING_NO_DATA_CODE, Const.WARNING_NO_DATA_MSG, new IncidentReport());
            }
            else
            {
                incident.ReportId = request.ReportId is null ? incident.ReportId : request.ReportId;
                incident.TripId = request.TripId is null ? incident.TripId : request.TripId;
                incident.ReportedBy = userName;
                incident.IncidentType = request.IncidentType is null ? incident.IncidentType : request.IncidentType;
                incident.Description = request.Description is null ? incident.Description : request.Description;
                incident.Location = request.Location is null ? incident.Location : request.Location;
                incident.Type = request.Type is null ? incident.Type : request.Type;
                incident.Status = request.Status is null ? incident.Status : request.Status;
                incident.HandledBy = request.HandledBy is null ? incident.HandledBy : request.HandledBy;
                incident.HandledTime = request.HandledTime != default ? request.HandledTime : incident.HandledTime;
                incident.ResolutionDetails = request.ResolutionDetails != default ? request.ResolutionDetails : incident.ResolutionDetails;
            }

            // Xử lý xóa ảnh bị loại bỏ
            if (request.RemovedImage is not [])
            {
                IncidentReportsFile? image;
                foreach (var url in request.RemovedImage)
                {
                    if ((image = await _unitOfWork.IncidentReportsFileRepository.GetImageByUrl(url)) is not null && image.ReportId == incident.ReportId)
                    {
                        await _firebaseStorageService.DeleteImageAsync(_firebaseStorageService.ExtractImageNameFromUrl(url));
                        await _unitOfWork.IncidentReportsFileRepository.RemoveAsync(image);
                    }
                }
            }

            // Xử lý thêm ảnh mới
            if (request.AddedImage is not null)
            {
                string FileId;
                List<IncidentReportsFile> images;
                int id;
                for (int i = 0; i < request.AddedImage.Count; i++)
                {
                    var image = request.AddedImage[i];
                    var imageType = request.ImageType[i];
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
                        Type = imageType // Set the ImageType
                    });
                }
            }

            var result = await _unitOfWork.IncidentReportsRepository.UpdateAsync(incident);
            var data = await _unitOfWork.IncidentReportsRepository.GetImagesByReportId(incident.ReportId);
            var order = await _unitOfWork.FuelReportRepository.GetOrderByTripId(incident.TripId);
            var owner = order.CreatedBy;
            if (result > 0)
            {
                // Gửi thông báo sau khi cập nhật thành công
                await _notification.SendNotificationAsync(owner, "Incident Report Updated", $"New Incident report Updated by {data.ReportedBy}.", data.ReportedBy);
                return new BusinessResult(Const.SUCCESS_UPDATE_CODE, Const.SUCCESS_UPDATE_MSG, data);
            }
            else
            {
                return new BusinessResult(Const.FAIL_UPDATE_CODE, Const.FAIL_UPDATE_MSG, data);
            }
        }
        #endregion

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
        /// <summary>
        /// Delete incident report by report id
        /// </summary>
        /// <author name="Đoàn Lê Hiển Minh"></author>
        /// <returns></returns>
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

        public async Task<IBusinessResult> ResolvedReport(ResolvedIncidentReportRequest incidentReportRequest, ClaimsPrincipal claims)
        {
            try
            {
                var userId = claims.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = claims.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                var incident = _unitOfWork.IncidentReportsRepository.Get(i => i.ReportId == incidentReportRequest.reportId);
                var trip = _unitOfWork.TripRepository.Get(t => t.TripId == incident.TripId);
                var driver = _unitOfWork.DriverRepository.Get(d => d.DriverId == trip.DriverId);
                var tractor = _unitOfWork.TractorRepository.Get(t => t.TractorId == trip.TractorId);
                var trailer = _unitOfWork.TrailerRepository.Get(t => t.TrailerId == trip.TrailerId);

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
                        driver.Status = 1; // Free
                        tractor.Status = VehicleStatus.Active.ToString();
                        trailer.Status = VehicleStatus.Active.ToString();
                        trip.EndTime = DateTime.Now;

                        await _unitOfWork.DriverRepository.UpdateAsync(driver);
                        await _unitOfWork.TractorRepository.UpdateAsync(tractor);
                        await _unitOfWork.TrailerRepository.UpdateAsync(trailer);
                    }

                    var result = await _unitOfWork.IncidentReportsRepository.UpdateAsync(incident);
                    if (result > 0)
                        return new BusinessResult(Const.SUCCESS_UPDATE_CODE, Const.SUCCESS_UPDATE_MSG, incident);
                    else
                        return new BusinessResult(Const.FAIL_UPDATE_CODE, Const.FAIL_UPDATE_MSG, incident);
                }
            }
            catch (Exception ex)
            {
                return new BusinessResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }

        public async Task<IBusinessResult> UpdateIncidentReportMO(UpdateIncidentReportMORequest updateIncidentReportMO, ClaimsPrincipal claims)
        {
            try
            {
                var userId = claims.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = claims.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

                var incident = await _unitOfWork.IncidentReportsRepository.GetIncidentReportDetails(updateIncidentReportMO.ReportId);
                if (incident is null)
                {
                    return new BusinessResult(Const.WARNING_NO_DATA_CODE, Const.WARNING_NO_DATA_MSG, new IncidentReport());
                }
                else
                {
                    incident.ReportId = updateIncidentReportMO.ReportId is null ? incident.ReportId : updateIncidentReportMO.ReportId;
                    incident.ReportedBy = userName;
                    incident.IncidentType = updateIncidentReportMO.IncidentType is null ? incident.IncidentType : updateIncidentReportMO.IncidentType;
                    incident.Description = updateIncidentReportMO.Description is null ? incident.Description : updateIncidentReportMO.Description;
                    incident.Location = updateIncidentReportMO.Location is null ? incident.Location : updateIncidentReportMO.Location;
                    incident.Type = updateIncidentReportMO.Type is null ? incident.Type : updateIncidentReportMO.Type;
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
                var order = await _unitOfWork.FuelReportRepository.GetOrderByTripId(incident.TripId);
                var owner = order.CreatedBy;
                if (result > 0)
                {
                    // Gửi thông báo sau khi cập nhật thành công
                    await _notification.SendNotificationAsync(owner, "Incident Report Updated", $"New Incident report Updated by {data.ReportedBy}.", data.ReportedBy);
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
    }
}