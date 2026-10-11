using Api.ApplicationCore.Interfaces.SmartSheep;
using Api.ApplicationCore.Interfaces.Repositories;
using Api.Domain.Entities.SmartSheep;
using Api.Domain.Models.SmartSheep;
using DevExtreme.AspNet.Data.ResponseModel;
using Microsoft.EntityFrameworkCore;
using Project.Base.Models;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.JQueries.Datatables;
using Project.Core.DevExpress;

namespace Api.ApplicationCore.Services.SmartSheep
{
    public class SucklingActivityService : ISucklingActivityService
    {
        private const string Suckling = "Suckling";
        private const string Approach = "Approach";
        private static readonly HashSet<string> EventTypes = new() { Suckling, "FailedAttempt", Approach };

        private readonly ISmartSheepRepositories _smartSheepRepository;
        private readonly ISucklingStatisticService _statisticService;

        public SucklingActivityService(ISmartSheepRepositories repository, ISucklingStatisticService statisticService)
        {
            _smartSheepRepository = repository;
            _statisticService = statisticService;
        }

        public async Task<LoadResult> DxGridAsync(DataSourceLoadOptions param)
        {
            var datas = GetAll();
            return await DxDataGridExtentions.DxDataGridAsync(datas, param);
        }

        public IQueryable<SucklingActivity> GetAll()
        {
            return _smartSheepRepository.SucklingActivities.GetAll()
                .Include(x => x.Barn);
        }

        public IQueryable<SucklingActivity> GetAllByBarn(Guid fkBarnId)
        {
            return GetAll().Where(x => x.FkBarnId == fkBarnId);
        }

        public async Task<DataPagedResults<SucklingActivity>> GetAsync(DataParameter param)
        {
            var data = GetAll();
            return await _smartSheepRepository.SucklingActivities.GetByPagingAsync(data, param.Start, param.Length);
        }

        public async Task<SucklingActivity> GetAsync(Guid id)
        {
            return await GetAll().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Guid?> PostAsync(SucklingActivity model)
        {
            var data = await _smartSheepRepository.SucklingActivities.AddAsync(model);
            return data.Id;
        }

        public async Task<Guid?> PutAsync(SucklingActivity model)
        {
            await _smartSheepRepository.SucklingActivities.UpdateAsync(model);
            return model.Id;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            bool result = false;
            var data = await GetAsync(id);

            if (data != null)
            {
                await _smartSheepRepository.SucklingActivities.DeleteAsync(data);
                result = true;
            }

            return result;
        }

        // Sumber video untuk InferenceYolo. Worker bukan user farm, sehingga dibaca tanpa filter farm;
        // akses dibatasi scope client SucklingActivity.Add.
        public async Task<List<SucklingSyncSourceDto>> GetSyncSourcesAsync()
        {
            return await GetSyncSources().Where(x => x.IsOnline).ToListAsync();
        }

        public async Task<SucklingSyncSourceDto> GetSyncSourceAsync(string code)
        {
            return await GetSyncSources().FirstOrDefaultAsync(x => x.Code == code);
        }

        private IQueryable<SucklingSyncSourceDto> GetSyncSources()
        {
            return _smartSheepRepository.Contexts.SourceVideo.IgnoreQueryFilters().AsNoTracking()
                .Where(x => x.IsActive && x.FkBarnId != null)
                .OrderBy(x => x.Code)
                .Select(x => new SucklingSyncSourceDto
                {
                    Id = x.Id,
                    Code = x.Code,
                    BarnCode = x.Barn!.Code,
                    EweCount = x.Barn!.EweCount,
                    LambCount = x.Barn!.LambCount,
                    SourceVideoUrl = x.SourceVideoUrl,
                    Username = x.Username,
                    Password = x.Password,
                    IsOnline = x.IsOnline
                });
        }

        // Sync kejadian dari InferenceYolo (tanpa filter farm, lihat GetSyncSourceAsync).
        // - Kejadian dengan Id yang sudah tersimpan dilewati (sync ulang tidak menggandakan data).
        // - Rekaman yang diproses ulang setelah worker dijalankan ulang menghasilkan Id baru: kejadian SourceVideo yang sama
        //   dengan anak, waktu mulai, dan jenis (pendekatan, atau menyusu/pendekatan gagal) yang sama tidak ditambah lagi;
        //   bila lebih panjang (sebelumnya terpotong saat worker dihentikan) baris yang ada diperbarui, beserta total
        //   sesinya; pendekatan gagal yang ternyata menyusu masuk ke sesi sync ini.
        // - Kejadian menyusu dalam satu sync = satu sesi SucklingActivity; pendekatan dan pendekatan gagal
        //   disimpan tanpa sesi.
        // - Statistik periode dalam rentang ProcessedFrom-ProcessedUntil (dan periode kejadian yang terkirim)
        //   dihitung ulang.
        // Mengembalikan jumlah kejadian baru, atau null bila SourceVideo tidak ditemukan.
        public async Task<int?> SyncAsync(SucklingSyncParam param)
        {
            var db = _smartSheepRepository.Contexts;
            var source = await db.SourceVideo.IgnoreQueryFilters().AsNoTracking()
                .FirstOrDefaultAsync(x => x.IsActive && x.Code == param.SourceVideoCode);
            if (source?.FkBarnId == null) return null;

            var ids = param.Events.Select(x => x.Id).ToList();
            var existingIds = await db.SucklingEvent.IgnoreQueryFilters()
                .Where(x => ids.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync();
            var starts = param.Events.Select(x => (DateTime?)x.StartAt).ToList();
            var replayedEvents = await db.SucklingEvent.IgnoreQueryFilters().AsTracking()
                .Where(x => x.FkSourceVideoId == source.Id && starts.Contains(x.EventStart))
                .ToListAsync();
            SucklingEvent FindReplayed(SucklingSyncEventParam x) => replayedEvents.FirstOrDefault(e => e.LambTrackId == x.LambTrackId &&
                e.EventStart == x.StartAt && (e.EventType == Approach) == (x.EventType == Approach));
            var upgraded = new List<SucklingEvent>();
            var extendedActivityIds = new List<Guid>();
            foreach (var x in param.Events.Where(x => EventTypes.Contains(x.EventType) && !existingIds.Contains(x.Id)))
            {
                var replayed = FindReplayed(x);
                if (replayed != null && x.EndAt > replayed.EventEnd)
                {
                    if (replayed.FkSucklingActivityId.HasValue)
                    {
                        extendedActivityIds.Add(replayed.FkSucklingActivityId.Value);
                    }
                    else if (x.EventType == Suckling)
                    {
                        upgraded.Add(replayed);
                    }

                    replayed.EventType = x.EventType;
                    replayed.EventEnd = x.EndAt;
                    replayed.DurationSeconds = Math.Round(x.DurationSeconds, 3);
                    replayed.FrameCount = x.FrameCount;
                }
            }

            var newEvents = param.Events
                .Where(x => EventTypes.Contains(x.EventType) && !existingIds.Contains(x.Id) && FindReplayed(x) == null)
                .DistinctBy(x => x.Id)
                .DistinctBy(x => (x.EventType == Approach, x.LambTrackId, x.StartAt))
                .ToList();

            var sucklings = newEvents.Where(x => x.EventType == Suckling).ToList();
            SucklingActivity activity = null;
            if (sucklings.Count > 0 || upgraded.Count > 0)
            {
                activity = new SucklingActivity
                {
                    Id = Guid.NewGuid(),
                    FkBarnId = source.FkBarnId,
                    ActivityStart = sucklings.Select(x => (DateTime?)x.StartAt).Concat(upgraded.Select(x => x.EventStart)).Min(),
                    ActivityEnd = sucklings.Select(x => (DateTime?)x.EndAt).Concat(upgraded.Select(x => x.EventEnd)).Max(),
                    TotalFrequency = sucklings.Count + upgraded.Count,
                    TotalDurationSeconds = (int)Math.Round(sucklings.Sum(x => x.DurationSeconds) + upgraded.Sum(x => x.DurationSeconds)),
                    IsActive = true
                };
                db.SucklingActivity.Add(activity);
                upgraded.ForEach(x => x.FkSucklingActivityId = activity.Id);
            }

            if (extendedActivityIds.Count > 0)
            {
                var linkedEvents = await db.SucklingEvent.IgnoreQueryFilters().AsTracking()
                    .Where(x => x.FkSucklingActivityId.HasValue && extendedActivityIds.Contains(x.FkSucklingActivityId.Value))
                    .ToListAsync();
                foreach (var linked in await db.SucklingActivity.IgnoreQueryFilters().AsTracking()
                    .Where(x => extendedActivityIds.Contains(x.Id)).ToListAsync())
                {
                    var events = linkedEvents.Where(x => x.FkSucklingActivityId == linked.Id).ToList();
                    linked.ActivityStart = events.Min(x => x.EventStart);
                    linked.ActivityEnd = events.Max(x => x.EventEnd);
                    linked.TotalFrequency = events.Count;
                    linked.TotalDurationSeconds = (int)Math.Round(events.Sum(x => x.DurationSeconds));
                }
            }

            db.SucklingEvent.AddRange(newEvents.Select(x => new SucklingEvent
            {
                Id = x.Id,
                FkSucklingActivityId = x.EventType == Suckling ? activity?.Id : null,
                FkBarnId = source.FkBarnId,
                FkSourceVideoId = source.Id,
                EventType = x.EventType,
                LambTrackId = x.LambTrackId,
                EweTrackId = x.EweTrackId,
                EventStart = x.StartAt,
                EventEnd = x.EndAt,
                DurationSeconds = Math.Round(x.DurationSeconds, 3),
                FrameCount = x.FrameCount,
                IsActive = true
            }));
            await db.SaveChangesAsync();

            var from = param.Events.Count > 0 && param.Events.Min(x => x.StartAt) < param.ProcessedFrom
                ? param.Events.Min(x => x.StartAt)
                : param.ProcessedFrom;
            await _statisticService.RecalculateAsync(source.FkBarnId.Value, from, param.ProcessedUntil);

            return newEvents.Count;
        }
    }
}
