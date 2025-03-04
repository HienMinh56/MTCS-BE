using MTCS.Common;
using MTCS.Data;
using MTCS.Data.Models;
using MTCS.Data.Request;
using MTCS.Service.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace MTCS.Service.Service
{
    public interface IIncidentReportsService
    {
        Task<IBusinessResult> GetIncidentReportsByTripId(string tripId);
        Task<IBusinessResult> CreateIncidentReport(CreateIncidentReportRequest request);
        Task<IBusinessResult> UpdateIncidentReport(UpdateIncidentReportRequest request);
        Task<IBusinessResult> DeleteIncidentReportById(string reportId);
    }

    public class IncidentReportsService : IIncidentReportsService
    {
        private readonly UnitOfWork _unitOfWork;
        private readonly IFirebaseStorageService _firebaseStorageService;
        public IncidentReportsService(IFirebaseStorageService firebaseStorageService)
        {
            _unitOfWork ??= new UnitOfWork();
            _firebaseStorageService = firebaseStorageService;
        }

        /// <summary>
        /// Get all incident reports by trip id
        /// </summary>
        /// <author name="Đoàn Lê Hiển Minh"></author>
        /// <returns></returns>
        public async Task<IBusinessResult> GetIncidentReportsByTripId(string tripId)
        {
            try
            {
                var incidents = await _unitOfWork.IncidentReportsRepository.GetIncidentReportsByTripId(tripId);
                if (incidents == null)
                {
                    return new BusinessResult(Const.WARNING_NO_DATA_CODE, Const.WARNING_NO_DATA_MSG, new IncidentReport());
                }
                return new BusinessResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, incidents);
            }
            catch (Exception ex)
            {
                return new BusinessResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }

        /// <summary>
        /// Create incident report for a trip
        /// </summary>
        /// <author name="Đoàn Lê Hiển Minh"></author>
        /// <returns></returns>
        public async Task<IBusinessResult> CreateIncidentReport(CreateIncidentReportRequest request)
        {
            var incidents = await _unitOfWork.IncidentReportsRepository.GetIncidentReportsByTripId(request.TripId);
            var Id = incidents.Count + 1;
            if (_unitOfWork.IncidentReportsRepository.Get(i => i.ReportId == $"{Const.INCIDENTREPORT}{Id.ToString("D4")}") is not null)
            {
                Id = await _unitOfWork.IncidentReportsRepository.FindEmptyPositionWithBinarySearch(incidents, 1, Id, Const.INCIDENTREPORT, Const.INCIDENTREPORT_INDEX);
            }

            var reportId = $"{Const.INCIDENTREPORT}{Id.ToString("D4")}";
            await _unitOfWork.IncidentReportsRepository.CreateAsync(new IncidentReport
            {
                ReportId = reportId,
                TripId = request.TripId,
                ReportedBy = request.ReportedBy,
                IncidentType = request.IncidentType,
                Description = request.Description,
                IncidentTime = request.IncidentTime,
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
                    foreach (var image in request.Image)
                    {
                        images = await _unitOfWork.IncidentReportsFileRepository.GetImagesOfIncidentReport();
                        iId = images.Count + 1;
                        if (_unitOfWork.IncidentReportsFileRepository.Get(i => i.FileId == $"{Const.INCIDENTREPORTIMAGE}{iId.ToString("D4")}") is not null)
                        {
                            iId = await _unitOfWork.IncidentReportsFileRepository.FindEmptyPositionWithBinarySearch(images, 1, iId, Const.INCIDENTREPORTIMAGE, Const.INCIDENTREPORTIMAGE_INDEX);
                        }

                        FileId = $"{Const.INCIDENTREPORTIMAGE}{iId.ToString("D4")}";
                        await _unitOfWork.IncidentReportsFileRepository.CreateAsync(new IncidentReportsFile
                        {
                            FileId = FileId,
                            ReportId = reportId,
                            FileName = image.FileName,
                            FileType = Path.GetExtension(image.FileName).TrimStart('.'),
                            FileUrl = await _firebaseStorageService.UploadImageAsync(image),
                            UploadDate = DateTime.Now,
                            UploadBy = "Admin" // FIx later
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                await DeleteIncidentReportById(reportId);
                return new BusinessResult(Const.FAIL_CREATE_CODE, Const.FAIL_CREATE_MSG, ex.Message);
            }

            var result = await _unitOfWork.IncidentReportsRepository.GetImagesByReportId(reportId);
            if (result is null)
            {
                return new BusinessResult(Const.FAIL_CREATE_CODE, Const.FAIL_CREATE_MSG, new IncidentReport());
            }

            if (result.IncidentReportsFiles is not null)
            {
                return new BusinessResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, result);
            }
            else
            {
                await DeleteIncidentReportById(reportId);
                return new BusinessResult(Const.FAIL_CREATE_CODE, Const.FAIL_CREATE_MSG, new IncidentReport());
            }
        }

        /// <summary>
        /// Update incident report for a trip
        /// </summary>
        /// <author name="Đoàn Lê Hiển Minh"></author>
        /// <returns></returns>
        public async Task<IBusinessResult> UpdateIncidentReport(UpdateIncidentReportRequest request)
        {
            var incident = await _unitOfWork.IncidentReportsRepository.GetIncidentReportDetails(request.ReportId);
            if (incident is null)
            {
                return new BusinessResult(Const.WARNING_NO_DATA_CODE, Const.WARNING_NO_DATA_MSG, new IncidentReport());
            }
            else
            {
                incident.ReportId = request.ReportId is null ? incident.ReportId : request.ReportId;
                incident.TripId = request.TripId is null ? incident.TripId : request.TripId;
                incident.ReportedBy = request.ReportedBy is null ? incident.ReportedBy : request.ReportedBy;
                incident.IncidentType = request.IncidentType is null ? incident.IncidentType : request.IncidentType;
                incident.Description = request.Description is null ? incident.Description : request.Description;
                incident.IncidentTime = request.IncidentTime != default ? request.IncidentTime : incident.IncidentTime;
                incident.Location = request.Location is null ? incident.Location : request.Location;
                incident.Type = request.Type is null ? incident.Type : request.Type;
                incident.Status = request.Status is null ? incident.Status : request.Status;
            }

            // Handle removed images
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

            // Handle added images
            if (request.AddedImage is not null)
            {
                string FileId;
                List<IncidentReportsFile> images;
                int id;
                foreach (var image in request.AddedImage)
                {
                    images = await _unitOfWork.IncidentReportsFileRepository.GetImagesOfIncidentReport();
                    id = images.Count + 1;
                    if (_unitOfWork.IncidentReportsFileRepository.Get(i => i.FileId == $"{Const.INCIDENTREPORTIMAGE}{id.ToString("D4")}") is not null)
                    {
                        id = await _unitOfWork.IncidentReportsFileRepository.FindEmptyPositionWithBinarySearch(images, 1, id, Const.INCIDENTREPORTIMAGE, Const.INCIDENTREPORTIMAGE_INDEX);
                    }

                    FileId = $"{Const.INCIDENTREPORTIMAGE}{id.ToString("D4")}";
                    await _unitOfWork.IncidentReportsFileRepository.CreateAsync(new IncidentReportsFile
                    {
                        FileId = FileId,
                        ReportId = incident.ReportId,
                        FileName = image.FileName,
                        FileType = Path.GetExtension(image.FileName).TrimStart('.'),
                        FileUrl = await _firebaseStorageService.UploadImageAsync(image),
                        UploadDate = DateTime.Now,
                        UploadBy = "Admin" // FIx later
                    });
                }
            }

            if (await _unitOfWork.IncidentReportsRepository.UpdateAsync(incident) > 0)
            {
                return new BusinessResult(Const.SUCCESS_UPDATE_CODE, Const.SUCCESS_UPDATE_MSG, incident);
            }
            else
            {
                return new BusinessResult(Const.FAIL_UPDATE_CODE, Const.FAIL_UPDATE_MSG, new IncidentReport());
            }
        }

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
    }
}