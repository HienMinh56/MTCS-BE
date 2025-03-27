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

        [HttpGet]
        public async Task<IBusinessResult> GetAllIncidentReports(string? driverId, string? tripId, string? reportId)
        {
            var result = await _incidentReportsService.GetAllIncidentReports(driverId, tripId, reportId);
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