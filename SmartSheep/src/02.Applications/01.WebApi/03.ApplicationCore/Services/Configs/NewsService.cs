using Api.ApplicationCore.Interfaces.Configs;
using Api.ApplicationCore.Interfaces.Repositories;
using Api.Domain.Entities.Configs;
using DevExtreme.AspNet.Data.ResponseModel;
using Microsoft.EntityFrameworkCore;
using Project.Base.Models;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.JQueries.Datatables;
using Project.Core.DevExpress;

namespace Api.ApplicationCore.Services.Configs
{
    public class NewsService : INewsService
    {
        private readonly IConfigRepositories _configRepository;

        public NewsService(IConfigRepositories repository)
        {
            _configRepository = repository;
        }

        public async Task<LoadResult> DxGridAsync(DataSourceLoadOptions param)
        {
            var datas = GetAll();
            return await DxDataGridExtentions.DxDataGridAsync(datas, param);
        }

        public IQueryable<News> GetAll()
        {
            return _configRepository.News.GetAll();
        }

        public async Task<DataPagedResults<News>> GetAsync(DataParameter param)
        {
            var data = GetAll();
            return await _configRepository.News.GetByPagingAsync(data, param.Start, param.Length);
        }

        public async Task<News> GetAsync(Guid id)
        {
            return await GetAll().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Guid?> PostAsync(News model)
        {
            var data = await _configRepository.News.AddAsync(model);
            return data.Id;
        }

        public async Task<Guid?> PutAsync(News model)
        {
            await _configRepository.News.UpdateAsync(model);
            return model.Id;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            bool result = false;
            var data = await GetAsync(id);

            if (data != null)
            {
                await _configRepository.News.DeleteAsync(data);
                result = true;
            }

            return result;
        }
    }
}
