using Api.ApplicationCore.Interfaces.SmartSheep;
using Api.Controllers.FilterAttributes;
using Api.Domain.Models.Dashboards;
using Api.Domain.Models.Reports;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Core.RestApis;
using Project.Core.RestApis.Response;

namespace Api.Controllers.SmartSheep
{
    public class SmartSheepReportController : BaseApiController
    {
        private readonly ISmartSheepReportService _service;

        public SmartSheepReportController(ISmartSheepReportService service)
        {
            _service = service;
        }

        [HttpGet("Dashboard")]
        [AuthorizeFilter(Scope = "SmartSheepReport.View")]
        public async Task<IActionResult> DashboardAsync([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] Guid? farmId, [FromQuery] Guid? barnId)
        {
            if (startDate.HasValue && endDate.HasValue && startDate.Value.Date > endDate.Value.Date)
            {
                return new BadRequest("Start date must be before or equal to end date", new { startDate, endDate }).ReturnResponse();
            }

            var data = await _service.GetDashboardAsync(new DashboardFilterParam
            {
                StartDate = startDate,
                EndDate = endDate,
                FarmId = farmId,
                BarnId = barnId
            });

            return new OK("Success get dashboard data", data).ReturnResponse();
        }

        [HttpGet("SucklingActivities")]
        [AuthorizeFilter(Scope = "SmartSheepReport.View")]
        public async Task<IActionResult> ActivityReportAsync([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? hour, [FromQuery] Guid? farmId, [FromQuery] Guid? barnId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var validation = ValidateFilter(from, to, hour);
            if (validation != null) return validation;

            var data = await _service.GetSucklingActivities(new SmartSheepReportFilterParam
            {
                From = from,
                To = to,
                Hour = hour,
                FarmId = farmId,
                BarnId = barnId
            }).Skip((Math.Max(page, 1) - 1) * Math.Clamp(pageSize, 1, 100)).Take(Math.Clamp(pageSize, 1, 100) + 1).ToListAsync();

            return new OK("Success get suckling activity report", data).ReturnResponse();
        }

        [HttpGet("SucklingStatistics")]
        [AuthorizeFilter(Scope = "SmartSheepReport.View")]
        public async Task<IActionResult> StatisticReportAsync([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? hour, [FromQuery] Guid? farmId, [FromQuery] Guid? barnId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var validation = ValidateFilter(from, to, hour);
            if (validation != null) return validation;

            var data = await _service.GetSucklingStatistics(new SmartSheepReportFilterParam
            {
                From = from,
                To = to,
                Hour = hour,
                FarmId = farmId,
                BarnId = barnId
            }).Skip((Math.Max(page, 1) - 1) * Math.Clamp(pageSize, 1, 100)).Take(Math.Clamp(pageSize, 1, 100) + 1).ToListAsync();

            return new OK("Success get suckling statistic report", data).ReturnResponse();
        }

        [HttpGet("Reminders")]
        [AuthorizeFilter(Scope = "SmartSheepReport.View")]
        public async Task<IActionResult> ReminderReportAsync([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? hour, [FromQuery] Guid? farmId, [FromQuery] Guid? barnId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var validation = ValidateFilter(from, to, hour);
            if (validation != null) return validation;

            var data = await _service.GetReminders(new SmartSheepReportFilterParam
            {
                From = from,
                To = to,
                Hour = hour,
                FarmId = farmId,
                BarnId = barnId
            }).Skip((Math.Max(page, 1) - 1) * Math.Clamp(pageSize, 1, 100)).Take(Math.Clamp(pageSize, 1, 100) + 1).ToListAsync();

            return new OK("Success get reminder report", data).ReturnResponse();
        }

        private IActionResult? ValidateFilter(DateTime? from, DateTime? to, int? hour)
        {
            if (from.HasValue && to.HasValue && from.Value > to.Value)
            {
                return new BadRequest("From date must be before or equal to date", new { from, to }).ReturnResponse();
            }

            if (hour.HasValue && (hour.Value < 0 || hour.Value > 23))
            {
                return new BadRequest("Hour must be between 0 and 23", new { hour }).ReturnResponse();
            }

            return null;
        }
    }
}
