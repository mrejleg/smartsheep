using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Data.ResponseModel;
using Newtonsoft.Json;
using Project.Base.Models.DevExpress.DxGrids;
using System.Collections;

namespace Project.Core.DevExpress
{
    public static class DxDataGridExtentions
    {
        public static LoadResult DxDataGrid<TEntity>(IEnumerable<TEntity> source, DataSourceLoadOptions parameters)
        {
            var paramData = new DataSourceLoadOptionsBase
            {
                RequireTotalCount = parameters.RequireTotalCount,
                RequireGroupCount = parameters.RequireGroupCount,
                IsCountQuery = parameters.IsCountQuery,
                IsSummaryQuery = parameters.IsSummaryQuery,
                Skip = parameters.Skip,
                Take = parameters.Take,
                Sort = null,
                Group = null,
                Filter = null,
                TotalSummary = null,
                GroupSummary = null,
                Select = null,
                PreSelect = null,
                RemoteSelect = parameters.RemoteSelect,
                RemoteGrouping = parameters.RemoteGrouping,
                ExpandLinqSumType = parameters.ExpandLinqSumType,
                PrimaryKey = null,
                DefaultSort = parameters.DefaultSort,
                StringToLower = parameters.StringToLower,
                PaginateViaPrimaryKey = parameters.PaginateViaPrimaryKey,
                SortByPrimaryKey = parameters.SortByPrimaryKey,
                AllowAsyncOverSync = parameters.AllowAsyncOverSync
            };

            if (!string.IsNullOrEmpty(parameters.Sort))
            {
                paramData.Sort = JsonConvert.DeserializeObject<SortingInfo[]>(parameters.Sort);
            }

            if (!string.IsNullOrEmpty(parameters.Group))
            {
                paramData.Group = JsonConvert.DeserializeObject<GroupingInfo[]>(parameters.Group);
            }

            if (!string.IsNullOrEmpty(parameters.Filter))
            {
                paramData.Filter = JsonConvert.DeserializeObject<IList>(parameters.Filter);
            }

            if (!string.IsNullOrEmpty(parameters.TotalSummary))
            {
                paramData.TotalSummary = JsonConvert.DeserializeObject<SummaryInfo[]>(parameters.TotalSummary);
            }

            if (!string.IsNullOrEmpty(parameters.GroupSummary))
            {
                paramData.GroupSummary = JsonConvert.DeserializeObject<SummaryInfo[]>(parameters.GroupSummary);
            }

            if (!string.IsNullOrEmpty(parameters.Select))
            {
                paramData.Select = JsonConvert.DeserializeObject<string[]>(parameters.Select);
            }

            if (!string.IsNullOrEmpty(parameters.PreSelect))
            {
                paramData.PreSelect = JsonConvert.DeserializeObject<string[]>(parameters.PreSelect);
            }

            if (!string.IsNullOrEmpty(parameters.PrimaryKey))
            {
                paramData.PrimaryKey = JsonConvert.DeserializeObject<string[]>(parameters.PrimaryKey);
            }

            return DataSourceLoader.Load(source, parameters);
        }

        public static async Task<LoadResult> DxDataGridAsync<TEntity>(IQueryable<TEntity> source, DataSourceLoadOptions parameters)
        {
            var paramData = new DataSourceLoadOptionsBase
            {
                RequireTotalCount = parameters.RequireTotalCount,
                RequireGroupCount = parameters.RequireGroupCount,
                IsCountQuery = parameters.IsCountQuery,
                IsSummaryQuery = parameters.IsSummaryQuery,
                Skip = parameters.Skip,
                Take = parameters.Take,
                Sort = null,
                Group = null,
                Filter = null,
                TotalSummary = null,
                GroupSummary = null,
                Select = null,
                PreSelect = null,
                RemoteSelect = parameters.RemoteSelect,
                RemoteGrouping = parameters.RemoteGrouping,
                ExpandLinqSumType = parameters.ExpandLinqSumType,
                PrimaryKey = null,
                DefaultSort = parameters.DefaultSort,
                StringToLower = parameters.StringToLower,
                PaginateViaPrimaryKey = parameters.PaginateViaPrimaryKey,
                SortByPrimaryKey = parameters.SortByPrimaryKey,
                AllowAsyncOverSync = parameters.AllowAsyncOverSync
            };

            if (!string.IsNullOrEmpty(parameters.Sort))
            {
                paramData.Sort = JsonConvert.DeserializeObject<SortingInfo[]>(parameters.Sort);
            }

            if (!string.IsNullOrEmpty(parameters.Group))
            {
                paramData.Group = JsonConvert.DeserializeObject<GroupingInfo[]>(parameters.Group);
            }

            if (!string.IsNullOrEmpty(parameters.Filter))
            {
                paramData.Filter = JsonConvert.DeserializeObject<IList>(parameters.Filter);
            }

            if (!string.IsNullOrEmpty(parameters.TotalSummary))
            {
                paramData.TotalSummary = JsonConvert.DeserializeObject<SummaryInfo[]>(parameters.TotalSummary);
            }

            if (!string.IsNullOrEmpty(parameters.GroupSummary))
            {
                paramData.GroupSummary = JsonConvert.DeserializeObject<SummaryInfo[]>(parameters.GroupSummary);
            }

            if (!string.IsNullOrEmpty(parameters.Select))
            {
                paramData.Select = JsonConvert.DeserializeObject<string[]>(parameters.Select);
            }

            if (!string.IsNullOrEmpty(parameters.PreSelect))
            {
                paramData.PreSelect = JsonConvert.DeserializeObject<string[]>(parameters.PreSelect);
            }

            if (!string.IsNullOrEmpty(parameters.PrimaryKey))
            {
                paramData.PrimaryKey = JsonConvert.DeserializeObject<string[]>(parameters.PrimaryKey);
            }

            return await DataSourceLoader.LoadAsync(source, paramData);
        }
    }
}
