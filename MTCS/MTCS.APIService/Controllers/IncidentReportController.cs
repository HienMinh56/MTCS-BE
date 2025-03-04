using Microsoft.AspNetCore.Mvc;
using MTCS.Data.Request;
using MTCS.Service.Base;
using MTCS.Service.Service;
using static MTCS.Service.Service.IncidentReportsService;

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

        [HttpGet("{id}")]
        public async Task<IBusinessResult> GetIncidentReportsByTripId(string tripId)
        {
            var result = await _incidentReportsService.GetIncidentReportsByTripId(tripId);
            return result;
        }

        [HttpPost]
        public async Task<IBusinessResult> CreateIncidentReport([FromForm] CreateIncidentReportRequest request)
        {
            var result = await _incidentReportsService.CreateIncidentReport(request);
            return result;
        }

        [HttpPut]
        public async Task<IBusinessResult> UpdateIncidentReport([FromForm] UpdateIncidentReportRequest request)
        {
            var result = await _incidentReportsService.UpdateIncidentReport(request);
            return result;
        }
    }
}