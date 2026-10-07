using System.Globalization;
using Api.ApplicationCore.Interfaces.SmartSheep;
using Api.ApplicationCore.Interfaces.Repositories;
using Api.Domain.Entities.SmartSheep;
using DevExtreme.AspNet.Data.ResponseModel;
using Microsoft.EntityFrameworkCore;
using Project.Base.Models;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.JQueries.Datatables;
using Project.Core.DevExpress;

namespace Api.ApplicationCore.Services.SmartSheep
{
    public class SucklingStatisticService : ISucklingStatisticService
    {
        private const int DefaultStatisticsDurationMinutes = 120;
        private const string Suckling = "Suckling";
        private const string FailedAttempt = "FailedAttempt";
        private const string Approach = "Approach";

        private readonly ISmartSheepRepositories _smartSheepRepository;

        public SucklingStatisticService(ISmartSheepRepositories repository)
        {
            _smartSheepRepository = repository;
        }

        public async Task<LoadResult> DxGridAsync(DataSourceLoadOptions param)
        {
            var datas = GetAll();
            return await DxDataGridExtentions.DxDataGridAsync(datas, param);
        }

        public IQueryable<SucklingStatistic> GetAll()
        {
            return _smartSheepRepository.SucklingStatistics.GetAll();
        }

        public async Task<DataPagedResults<SucklingStatistic>> GetAsync(DataParameter param)
        {
            var data = GetAll();
            return await _smartSheepRepository.SucklingStatistics.GetByPagingAsync(data, param.Start, param.Length);
        }

        public async Task<SucklingStatistic> GetAsync(Guid id)
        {
            return await GetAll().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Guid?> PostAsync(SucklingStatistic model)
        {
            if (!model.FkBarnId.HasValue || !await _smartSheepRepository.Contexts.Barn.AnyAsync(x => x.Id == model.FkBarnId))
                return null;
            var data = await _smartSheepRepository.SucklingStatistics.AddAsync(model);
            return data.Id;
        }

        public async Task<Guid?> PutAsync(SucklingStatistic model)
        {
            var existing = await GetAsync(model.Id);
            if (existing == null) return null;
            // Older clients may omit the newly explicit ownership field.
            model.FkBarnId ??= existing.FkBarnId;
            if (!model.FkBarnId.HasValue || !await _smartSheepRepository.Contexts.Barn.AnyAsync(x => x.Id == model.FkBarnId))
                return null;
            await _smartSheepRepository.SucklingStatistics.UpdateAsync(model);
            return model.Id;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            bool result = false;
            var data = await GetAsync(id);

            if (data != null)
            {
                await _smartSheepRepository.SucklingStatistics.DeleteAsync(data);
                result = true;
            }

            return result;
        }

        // Statistik pemantauan per periode (proposal Tabel 6 dan 3.3.3) dari SucklingEvent hasil sync InferenceYolo.
        // Periode = SystemConfig.StatisticsDurationMinutes sejak tengah malam. RequiresReminder hanya dinilai untuk
        // periode yang sudah selesai (berakhir sebelum processedUntil): jumlah atau durasi menyusu turun dibanding
        // periode sebelumnya pada kandang dan kategori waktu yang sama. Dipanggil dari sync worker (bukan user),
        // sehingga data dibaca tanpa filter farm.
        public async Task RecalculateAsync(Guid fkBarnId, DateTime from, DateTime processedUntil)
        {
            var db = _smartSheepRepository.Contexts;
            var barn = await db.Barn.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(x => x.Id == fkBarnId);
            if (barn == null) return;

            var durationMinutes = await db.SystemConfig
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.DateModified ?? x.DateCreated)
                .Select(x => x.StatisticsDurationMinutes)
                .FirstOrDefaultAsync();
            if (durationMinutes <= 0) durationMinutes = DefaultStatisticsDurationMinutes;

            var start = from.Date.AddMinutes(Math.Floor(from.TimeOfDay.TotalMinutes / durationMinutes) * durationMinutes);
            for (; start < processedUntil; start = start.AddMinutes(durationMinutes))
            {
                var end = start.AddMinutes(durationMinutes);
                var observedUntil = end < processedUntil ? end : processedUntil;
                var events = await db.SucklingEvent.IgnoreQueryFilters().AsNoTracking()
                    .Where(x => x.IsActive && x.FkBarnId == fkBarnId && x.EventStart >= start && x.EventStart < end)
                    .ToListAsync();
                var sucklings = events.Where(x => x.EventType == Suckling).OrderBy(x => x.EventStart).ToList();

                var statistic = await db.SucklingStatistic.IgnoreQueryFilters().AsTracking()
                    .FirstOrDefaultAsync(x => x.FkBarnId == fkBarnId && x.ActivityFrom == start);
                if (statistic == null)
                {
                    statistic = new SucklingStatistic { Id = Guid.NewGuid(), FkBarnId = fkBarnId, ActivityFrom = start, IsActive = true };
                    db.SucklingStatistic.Add(statistic);
                }

                statistic.Code = $"{barn.Code}-{start.ToString("yyyyMMddHHmm", CultureInfo.InvariantCulture)}";
                statistic.PeriodDescription = $"{start.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)} - " +
                    end.ToString("HH:mm", CultureInfo.InvariantCulture);
                statistic.ActivityTo = end;
                statistic.HourNumber = start.Hour;
                statistic.TimeCategory = start.Hour is >= 6 and < 18 ? "Siang" : "Malam";
                statistic.ObservationSeconds = (int)(observedUntil - start).TotalSeconds;
                statistic.TotalFrequency = sucklings.Count;
                statistic.TotalDurationSeconds = (int)Math.Round(sucklings.Sum(x => x.DurationSeconds));
                statistic.TotalApproach = events.Count(x => x.EventType == Approach);
                statistic.TotalFailedAttempt = events.Count(x => x.EventType == FailedAttempt);
                statistic.LongestGapSeconds = GetLongestGapSeconds(sucklings, start, observedUntil);
                statistic.RequiresReminder = end <= processedUntil && await IsDecreasedAsync(statistic);
                await db.SaveChangesAsync();

                if (statistic.RequiresReminder)
                {
                    await AddReminderLogsAsync(statistic, barn.FkFarmId);
                }
            }
        }

        // Rentang terlama tanpa menyusu: dari awal periode, antar kejadian, sampai akhir pengamatan
        private static int GetLongestGapSeconds(List<SucklingEvent> sucklings, DateTime start, DateTime end)
        {
            double longest = 0;
            var cursor = start;
            foreach (var suckling in sucklings)
            {
                longest = Math.Max(longest, (suckling.EventStart!.Value - cursor).TotalSeconds);
                if (suckling.EventEnd > cursor) cursor = suckling.EventEnd.Value;
            }

            return (int)Math.Round(Math.Max(longest, (end - cursor).TotalSeconds));
        }

        private async Task<bool> IsDecreasedAsync(SucklingStatistic statistic)
        {
            // Hanya statistik hasil sync (ObservationSeconds > 0) yang dibandingkan
            var previous = await _smartSheepRepository.Contexts.SucklingStatistic.IgnoreQueryFilters().AsNoTracking()
                .Where(x => x.IsActive && x.FkBarnId == statistic.FkBarnId && x.TimeCategory == statistic.TimeCategory &&
                    x.ActivityFrom < statistic.ActivityFrom && x.ObservationSeconds > 0)
                .OrderByDescending(x => x.ActivityFrom)
                .FirstOrDefaultAsync();

            return previous != null &&
                (statistic.TotalFrequency < previous.TotalFrequency || statistic.TotalDurationSeconds < previous.TotalDurationSeconds);
        }

        // Reminder untuk setiap user yang ditugaskan di farm kandang; dikirim oleh ReminderSchedulerService
        private async Task AddReminderLogsAsync(SucklingStatistic statistic, Guid? fkFarmId)
        {
            var db = _smartSheepRepository.Contexts;
            if (!fkFarmId.HasValue || await db.ReminderLog.IgnoreQueryFilters().AnyAsync(x => x.FkSucklingStatisticId == statistic.Id))
                return;

            var templateId = await db.ReminderTemplate
                .Where(x => x.IsActive)
                .OrderBy(x => x.Code)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync();
            if (!templateId.HasValue) return;

            var usernames = await db.UserFarm.IgnoreQueryFilters()
                .Where(x => x.IsActive && x.FkFarmId == fkFarmId.Value && x.UserLogin!.IsActive)
                .Select(x => x.UserLogin!.Username)
                .Distinct()
                .ToListAsync();

            db.ReminderLog.AddRange(usernames.Select(username => new ReminderLog
            {
                Id = Guid.NewGuid(),
                FkReminderTemplateId = templateId,
                FkSucklingStatisticId = statistic.Id,
                Username = username,
                Status = "Pending",
                ScheduledAt = DateTime.Now,
                IsActive = true
            }));
            await db.SaveChangesAsync();
        }
    }
}
