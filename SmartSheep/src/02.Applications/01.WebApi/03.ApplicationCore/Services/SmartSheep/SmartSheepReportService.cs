using Api.ApplicationCore.Interfaces.Repositories;
using Api.ApplicationCore.Interfaces.SmartSheep;
using Api.Domain.Entities.SmartSheep;
using Api.Domain.Models.Dashboards;
using Api.Domain.Models.Reports;
using Microsoft.EntityFrameworkCore;

namespace Api.ApplicationCore.Services.SmartSheep
{
    public class SmartSheepReportService : ISmartSheepReportService
    {
        private readonly IMasterDataRepositories _masterDataRepository;
        private readonly ISmartSheepRepositories _smartSheepRepository;

        public SmartSheepReportService(IMasterDataRepositories masterDataRepository, ISmartSheepRepositories smartSheepRepository)
        {
            _masterDataRepository = masterDataRepository;
            _smartSheepRepository = smartSheepRepository;
        }

        public async Task<DashboardDataModel> GetDashboardAsync(DashboardFilterParam param)
        {
            var startDate = (param.StartDate ?? DateTime.Today).Date;
            var endDate = (param.EndDate ?? param.StartDate ?? DateTime.Today).Date;
            var nextDay = endDate.AddDays(1);

            var farms = _masterDataRepository.Farms.GetAll().AsNoTracking().Where(x => x.IsActive);
            var barns = _masterDataRepository.Barns.GetAll().AsNoTracking().Where(x => x.IsActive);
            var livestocks = _masterDataRepository.Livestocks.GetAll().AsNoTracking().Where(x => x.IsActive);
            var sourceVideos = _masterDataRepository.SourceVideos.GetAll().AsNoTracking().Where(x => x.IsActive);
            var activities = _smartSheepRepository.SucklingActivities.GetAll().AsNoTracking()
                .Where(x => x.ActivityStart >= startDate && x.ActivityStart < nextDay);
            var statistics = _smartSheepRepository.SucklingStatistics.GetAll().AsNoTracking();
            var reminders = _smartSheepRepository.ReminderLogs.GetAll().AsNoTracking();

            if (param.FarmId.HasValue)
            {
                farms = farms.Where(x => x.Id == param.FarmId.Value);
                barns = barns.Where(x => x.FkFarmId == param.FarmId.Value);
                sourceVideos = sourceVideos.Where(x => x.Barn != null && x.Barn.FkFarmId == param.FarmId.Value);
                activities = activities.Where(x => x.Barn != null && x.Barn.FkFarmId == param.FarmId.Value);
                statistics = statistics.Where(x => x.Barn != null && x.Barn.FkFarmId == param.FarmId.Value);
                reminders = reminders.Where(x => x.SucklingStatistic != null && x.SucklingStatistic.Barn != null && x.SucklingStatistic.Barn.FkFarmId == param.FarmId.Value);
            }

            if (param.BarnId.HasValue)
            {
                var barnFarmId = await _masterDataRepository.Barns.GetAll().AsNoTracking()
                    .Where(x => x.Id == param.BarnId.Value)
                    .Select(x => x.FkFarmId)
                    .FirstOrDefaultAsync();

                if (barnFarmId.HasValue)
                {
                    farms = farms.Where(x => x.Id == barnFarmId.Value);
                }

                barns = barns.Where(x => x.Id == param.BarnId.Value);
                sourceVideos = sourceVideos.Where(x => x.FkBarnId == param.BarnId.Value);
                activities = activities.Where(x => x.FkBarnId == param.BarnId.Value);
                statistics = statistics.Where(x => x.FkBarnId == param.BarnId.Value);
                reminders = reminders.Where(x => x.SucklingStatistic != null && x.SucklingStatistic.FkBarnId == param.BarnId.Value);
            }

            var barnRows = await barns.Include(x => x.Farm).ToListAsync();
            var sourceVideoRows = await sourceVideos.Include(x => x.Barn).ToListAsync();
            var sourceVideoIds = sourceVideoRows.Select(x => x.Id).ToList();
            var statusRows = await _smartSheepRepository.SourceVideoStatusLogs.GetAll().AsNoTracking()
                .Where(x => x.FkIdSourceVideo.HasValue && sourceVideoIds.Contains(x.FkIdSourceVideo.Value))
                .OrderByDescending(x => x.LoggedAt)
                .ToListAsync();
            var latestStatuses = statusRows.GroupBy(x => x.FkIdSourceVideo!.Value).ToDictionary(x => x.Key, x => x.First());

            var hourlyTrend = await activities.Where(x => x.ActivityStart.HasValue)
                .GroupBy(x => x.ActivityStart!.Value.Hour)
                .Select(x => new DashboardHourlyTrendDto
                {
                    Hour = x.Key,
                    Frequency = x.Sum(y => y.TotalFrequency),
                    DurationSeconds = x.Sum(y => y.TotalDurationSeconds)
                })
                .OrderBy(x => x.Hour)
                .ToListAsync();

            var barnComparison = await activities
                .GroupBy(x => new { x.FkBarnId, x.Barn!.Code, x.Barn.Name })
                .Select(x => new DashboardBarnComparisonDto
                {
                    BarnId = x.Key.FkBarnId,
                    BarnCode = x.Key.Code,
                    BarnName = x.Key.Name,
                    Frequency = x.Sum(y => y.TotalFrequency),
                    DurationSeconds = x.Sum(y => y.TotalDurationSeconds)
                })
                .OrderByDescending(x => x.Frequency)
                .ToListAsync();

            var reminderRows = await reminders
                .Where(x => x.ScheduledAt >= startDate && x.ScheduledAt < nextDay)
                .GroupBy(x => x.Status)
                .Select(x => new DashboardReminderStatusDto { Status = x.Key, Total = x.Count() })
                .ToListAsync();
            var reminderStatuses = new[] { "Pending", "Sent", "Failed" }
                .Select(status => new DashboardReminderStatusDto
                {
                    Status = status,
                    Total = reminderRows.FirstOrDefault(x => x.Status == status)?.Total ?? 0
                })
                .ToList();

            var inactiveSourceVideos = sourceVideoRows
                .Where(x => !x.IsOnline)
                .Select(x =>
                {
                    latestStatuses.TryGetValue(x.Id, out var status);
                    return new DashboardInactiveSourceVideoDto
                    {
                        Id = x.Id,
                        Code = x.Code,
                        BarnName = x.Barn?.Name,
                        SourceVideoUrl = x.SourceVideoUrl,
                        LastStatus = status?.Status ?? "No Status",
                        LastCheckedAt = status?.LoggedAt
                    };
                })
                .ToList();

            var inactiveBarns = barnRows.Select(barn =>
            {
                var barnSourceVideos = sourceVideoRows.Where(x => x.FkBarnId == barn.Id).ToList();
                var online = barnSourceVideos.Count(x => x.IsOnline);
                return new DashboardInactiveBarnDto
                {
                    Id = barn.Id,
                    Code = barn.Code,
                    Name = barn.Name,
                    FarmName = barn.Farm?.Name,
                    TotalSourceVideos = barnSourceVideos.Count,
                    OnlineSourceVideos = online
                };
            }).Where(x => x.TotalSourceVideos == 0 || x.OnlineSourceVideos == 0).ToList();

            var farmRows = await farms.ToListAsync();
            var farmMaps = farmRows.Select(farm =>
            {
                var farmBarns = barnRows.Where(x => x.FkFarmId == farm.Id).ToList();
                var farmBarnIds = farmBarns.Select(x => x.Id).ToHashSet();
                var farmSourceVideos = sourceVideoRows.Where(x => x.FkBarnId.HasValue && farmBarnIds.Contains(x.FkBarnId.Value)).ToList();
                return new DashboardFarmMapDto
                {
                    Id = farm.Id,
                    Code = farm.Code,
                    Name = farm.Name,
                    Location = farm.Location,
                    Address = farm.Address,
                    Latitude = farm.Latitude,
                    Longitude = farm.Longitude,
                    TotalBarns = farmBarns.Count,
                    TotalSheep = farmBarns.Where(x => x.FkLivestockId.HasValue).Select(x => x.FkLivestockId).Distinct().Count(),
                    TotalSourceVideos = farmSourceVideos.Count,
                    OnlineSourceVideos = farmSourceVideos.Count(x => x.IsOnline)
                };
            }).ToList();

            var totalSheep = param.FarmId.HasValue || param.BarnId.HasValue
                ? barnRows.Where(x => x.FkLivestockId.HasValue).Select(x => x.FkLivestockId).Distinct().Count()
                : await livestocks.CountAsync();

            return new DashboardDataModel
            {
                Date = startDate,
                TotalFarms = farmRows.Count,
                TotalBarns = barnRows.Count,
                TotalSheep = totalSheep,
                TodayActivities = await activities.CountAsync(),
                TodayFrequency = await activities.SumAsync(x => (int?)x.TotalFrequency) ?? 0,
                TodayDurationSeconds = await activities.SumAsync(x => (int?)x.TotalDurationSeconds) ?? 0,
                StatisticsRequiringReminder = await statistics
                    .CountAsync(x => x.RequiresReminder && x.ActivityFrom >= startDate && x.ActivityFrom < nextDay),
                HourlyTrend = hourlyTrend,
                BarnComparison = barnComparison,
                Reminders = reminderStatuses,
                InactiveSourceVideos = inactiveSourceVideos,
                InactiveBarns = inactiveBarns,
                FarmMaps = farmMaps
            };
        }

        public IQueryable<SucklingActivity> GetSucklingActivities(SmartSheepReportFilterParam param)
        {
            var data = _smartSheepRepository.SucklingActivities.GetAll().Include(x => x.Barn).AsNoTracking();
            if (param.From.HasValue) data = data.Where(x => x.ActivityStart >= param.From.Value);
            if (param.To.HasValue) data = data.Where(x => x.ActivityStart <= param.To.Value);
            if (param.Hour.HasValue) data = data.Where(x => x.ActivityStart.HasValue && x.ActivityStart.Value.Hour == param.Hour.Value);
            if (param.FarmId.HasValue) data = data.Where(x => x.Barn != null && x.Barn.FkFarmId == param.FarmId.Value);
            if (param.BarnId.HasValue) data = data.Where(x => x.FkBarnId == param.BarnId.Value);
            return data.OrderByDescending(x => x.ActivityStart);
        }

        public IQueryable<SucklingStatistic> GetSucklingStatistics(SmartSheepReportFilterParam param)
        {
            var data = _smartSheepRepository.SucklingStatistics.GetAll().AsNoTracking();
            if (param.From.HasValue) data = data.Where(x => x.ActivityFrom >= param.From.Value);
            if (param.To.HasValue) data = data.Where(x => x.ActivityTo <= param.To.Value);
            if (param.Hour.HasValue) data = data.Where(x => x.HourNumber == param.Hour.Value);
            if (param.FarmId.HasValue) data = data.Where(x => x.Barn != null && x.Barn.FkFarmId == param.FarmId.Value);
            if (param.BarnId.HasValue) data = data.Where(x => x.FkBarnId == param.BarnId.Value);
            if (param.RequiresReminder.HasValue) data = data.Where(x => x.RequiresReminder == param.RequiresReminder.Value);
            return data.OrderByDescending(x => x.ActivityFrom);
        }

        public IQueryable<ReminderLog> GetReminders(SmartSheepReportFilterParam param)
        {
            var data = _smartSheepRepository.ReminderLogs.GetAll()
                .Include(x => x.ReminderTemplate)
                .Include(x => x.SucklingStatistic)
                .AsNoTracking();
            if (param.From.HasValue) data = data.Where(x => x.ScheduledAt >= param.From.Value);
            if (param.To.HasValue) data = data.Where(x => x.ScheduledAt <= param.To.Value);
            if (param.Hour.HasValue) data = data.Where(x => x.ScheduledAt.HasValue && x.ScheduledAt.Value.Hour == param.Hour.Value);
            if (param.FarmId.HasValue) data = data.Where(x => x.SucklingStatistic != null && x.SucklingStatistic.Barn != null && x.SucklingStatistic.Barn.FkFarmId == param.FarmId.Value);
            if (param.BarnId.HasValue) data = data.Where(x => x.SucklingStatistic != null && x.SucklingStatistic.FkBarnId == param.BarnId.Value);
            if (!string.IsNullOrWhiteSpace(param.Status)) data = data.Where(x => x.Status == param.Status);
            return data.OrderByDescending(x => x.ScheduledAt);
        }

    }
}
