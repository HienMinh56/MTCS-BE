using Microsoft.AspNetCore.Mvc;
using MTCS.Data.Models;
using MTCS.Data.Request;
using MTCS.Service.Base;
using MTCS.Service.Services;

namespace MTCS.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IncidentReportController : ControllerBase
    {
        private readonly IIncidentReportsService _incidentReportsService;

        public IncidentReportController(IIncidentReportsService incidentReportsService)
        {
            _incidentReportsService = incidentReportsService;
        }

        [HttpGet("driver/{driverId}")]
        public async Task<IBusinessResult> GetIncidentReportsByDriverId(string driverId)
        {
            var result = await _incidentReportsService.GetIncidentReportsByDriverId(driverId);
            return result;
        }

        [HttpGet("trip/{tripId}")]
        public async Task<IBusinessResult> GetIncidentReportsByTripId(string tripId)
        {
            var result = await _incidentReportsService.GetIncidentReportsByTripId(tripId);
            return result;
        }

        [HttpGet("report/{reportId}")]
        public async Task<IBusinessResult> GetIncidentReportsByReportId(string reportId)
        {
            var result = await _incidentReportsService.GetIncidentReportsByReportId(reportId);
            return result;
        }

        [HttpPost]
        public async Task<IBusinessResult> CreateIncidentReport([FromForm] CreateIncidentReportRequest request)
        {
            var currentUser = HttpContext.User;
            var result = await _incidentReportsService.CreateIncidentReport(request, currentUser);
            return result;
        }

        [HttpPut]
        public async Task<IBusinessResult> UpdateIncidentReport([FromForm] UpdateIncidentReportRequest request)
        {
            var currentUser = HttpContext.User;
            var result = await _incidentReportsService.UpdateIncidentReport(request, currentUser);
            return result;
        }

        [HttpPut("IncidentReportFile")]
        public async Task<IBusinessResult> UpdateIncidentReportsFileInfo ([FromForm] List<IncidentReportsFileUpdateRequest> request)
        {
            var currentUser = HttpContext.User;
            var result = await _incidentReportsService.UpdateIncidentReportFileInfo(request, currentUser);
            return result;
        }

        [HttpDelete]
        public async Task<IBusinessResult> DeleteIncidentReport(string reportId)
        {
            var result = await _incidentReportsService.DeleteIncidentReportById(reportId);
            return result;
        }
    }
}