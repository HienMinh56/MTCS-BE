using Microsoft.AspNetCore.Mvc;
using MTCS.Data.Request;
using MTCS.Service.Base;
using MTCS.Service.Services;
using static MTCS.Service.Services.IncidentReportsService;

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

        [HttpGet("{driverId}")]
        public async Task<IBusinessResult> GetIncidentReportsByDriverId(string driverId)
        {
            var result = await _incidentReportsService.GetIncidentReportsByDriverId(driverId);
            return result;
        }

        [HttpGet("{tripId}")]
        public async Task<IBusinessResult> GetIncidentReportsByTripId(string tripId)
        {
            var result = await _incidentReportsService.GetIncidentReportsByTripId(tripId);
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

        [HttpDelete]
        public async Task<IBusinessResult> DeleteIncidentReport(string reportId)
        {
            var result = await _incidentReportsService.DeleteIncidentReportById(reportId);
            return result;
        }
    }
}