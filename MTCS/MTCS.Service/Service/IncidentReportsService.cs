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

namespace MTCS.Service.Service
{
    public class IncidentReportsService
    {
        public interface IIncidentReportsService
        {
            Task<IBusinessResult> GetIncidentReportsByTripId(string tripId);
            Task<IBusinessResult> CreateIncidentReport(CreateIncidentReportRequest request);
            Task<IBusinessResult> UpdateIncidentReport(UpdateIncidentReportRequest request);
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

            public async Task<IBusinessResult> CreateIncidentReport(CreateIncidentReportRequest request)
            {
                #region Business rule
                //1.create a new IncidentReport entity and add it into trip in db
                //2.check if new IncidentReport was create. if wasn't, return failed message.
                //3.create new image entities with incidentId of IncidentReport created as foreign key
                //4.check if new image entity was created. if wasn't, remove IncidentReport entity created from DB and return failed message.
                #endregion

                var incidents = await _unitOfWork.IncidentReportsRepository.GetIncidentReportsByTripId(request.TripId);
                var Id = incidents.Count + 1;
                if (_unitOfWork.IncidentReportsRepository.Get(i => i.ReportId == $"{Const.INCIDENTREPORT}{Id.ToString("D4")}") is not null){
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
                        string imageId;
                        List<IncidentReportsFile> images;
                        int iId = 1;
                        foreach (var image in request.Image)
                        {
                            images = await _unitOfWork.
                        }
                    }
                }
            }
        }
    }
}