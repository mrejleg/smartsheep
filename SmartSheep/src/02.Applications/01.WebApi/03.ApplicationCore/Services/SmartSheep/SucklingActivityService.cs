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
        private static readonly HashSet<string> EventTypes = new() { Suckling, "FailedAttempt", "Approach" };

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
                    SourceVideoUrl = x.SourceVideoUrl,
                    Username = x.Username,
                    Password = x.Password,
                    IsOnline = x.IsOnline
                });
        }

        // Sync kejadian dari InferenceYolo (tanpa filter farm, lihat GetSyncSourceAsync).
        // - Kejadian dengan Id yang sudah tersimpan dilewati (sync ulang tidak menggandakan data).
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
            var newEvents = param.Events
                .Where(x => EventTypes.Contains(x.EventType) && !existingIds.Contains(x.Id))
                .DistinctBy(x => x.Id)
                .ToList();

            var sucklings = newEvents.Where(x => x.EventType == Suckling).ToList();
            SucklingActivity activity = null;
            if (sucklings.Count > 0)
            {
                activity = new SucklingActivity
                {
                    Id = Guid.NewGuid(),
                    FkBarnId = source.FkBarnId,
                    ActivityStart = sucklings.Min(x => x.StartAt),
                    ActivityEnd = sucklings.Max(x => x.EndAt),
                    TotalFrequency = sucklings.Count,
                    TotalDurationSeconds = (int)Math.Round(sucklings.Sum(x => x.DurationSeconds)),
                    IsActive = true
                };
                db.SucklingActivity.Add(activity);
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
