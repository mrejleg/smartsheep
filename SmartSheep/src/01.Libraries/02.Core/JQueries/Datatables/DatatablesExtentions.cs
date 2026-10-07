using Microsoft.EntityFrameworkCore;
using Project.Base.Models;
using Project.Base.Models.JQueries.Datatables;
using Project.Core.Extentions;
using Project.Core.Interfaces.Repositories.Dappers;
using System.Linq.Dynamic.Core;

namespace Project.Core.JQueries.Datatables
{
    public static class DatatablesExtentions
    {
        public static DatatablesPagedResults<TEntity> Datatables<TEntity>(this IQueryable<TEntity> source, DatatablesParameter parameter)
        {
            TEntity[] items;
            source = SearchData(source, parameter);
            source = SortData(source, parameter);
            var size = source.Count();
            if (parameter.Length > 0)
            {
                items = source
                    .Skip(parameter.Start / parameter.Length * parameter.Length)
                    .Take(parameter.Length)
                    .ToArray();
            }
            else
            {
                items = source
                .ToArray();
            }

            return new DatatablesPagedResults<TEntity>
            {
                Items = items,
                TotalSize = size
            };
        }

        public static async Task<DatatablesPagedResults<TEntity>> DatatablesAsync<TEntity>(this IQueryable<TEntity> source, DatatablesParameter parameter)
        {
            TEntity[] items;
            source = SearchData(source, parameter);
            source = SortData(source, parameter);
            var size = await source.CountAsync();
            if (parameter.Length > 0)
            {
                items = await source
                    .Skip(parameter.Start / parameter.Length * parameter.Length)
                    .Take(parameter.Length)
                    .ToArrayAsync();
            }
            else
            {
                items = await source
                .ToArrayAsync();
            }

            return new DatatablesPagedResults<TEntity>
            {
                Items = items,
                TotalSize = size
            };
        }

        private static IQueryable<TEntity> SearchData<TEntity>(IQueryable<TEntity> source, DatatablesParameter parameter)
        {
            if (!string.IsNullOrEmpty(parameter.Search.Value))
            {
                Type objType = typeof(TEntity);

                var searchCriteria = string.Empty;

                var columns = parameter.Columns.Where(x => x.Searchable == true).ToArray();

                foreach (var item in columns)
                {
                    var fieldType = string.Empty;

                    if (item.Data.Split(".").Count() > 1)
                    {
                        fieldType = "string";
                    }
                    else
                    {
                        fieldType = item.Name;
                    }

                    if (fieldType == "string")
                    {
                        searchCriteria += string.Format("({0} ?? \"\").ToLower().Contains(\"{1}\")", item.Data, parameter.Search.Value.ToLower());
                        searchCriteria += " or ";
                    }
                    else if (fieldType == "guid")
                    {
                        searchCriteria += item.Data + "=\"" + parameter.Search.Value + "\"";
                        searchCriteria += " or ";
                    }
                    else if (fieldType == "datetime")
                    {
                        if (parameter.Search.Value.IsDate())
                        {
                            var dateValue = DateTime.Parse(parameter.Search.Value);
                            var dateValueAdd = DateTime.Parse(parameter.Search.Value).AddDays(1);

                            searchCriteria += item.Data + " >= DateTime(" + dateValue.Year + ", " + dateValue.Month + ", " + dateValue.Day + ") and " + item.Data + " < DateTime(" + dateValueAdd.Year + ", " + dateValueAdd.Month + ", " + dateValueAdd.Day + ")";
                            searchCriteria += " or ";
                        }
                    }
                    else
                    {
                        if (parameter.Search.Value.IsNumber())
                        {
                            searchCriteria += item.Data + "=" + parameter.Search.Value;
                            searchCriteria += " or ";
                        }
                    }
                }

                searchCriteria = searchCriteria.Remove(searchCriteria.Length - 4, 4);

                source = source.Where(searchCriteria, parameter.Search.Value);
            }

            return source;
        }

        private static IQueryable<TEntity> SortData<TEntity>(IQueryable<TEntity> source, DatatablesParameter parameter)
        {
            var columns = parameter.Columns.ToArray();
            var isThenBy = false;

            if (parameter.Order.Count() > 0)
            {
                foreach (var item in parameter.Order)
                {
                    if (parameter.Columns[item.Column].Orderable)
                    {
                        if (isThenBy)
                        {
                            source = (source as IOrderedQueryable<TEntity>).ThenBy(columns[item.Column].Data + " " + item.Dir.ToString().Trim());
                        }
                        else
                        {
                            source = source.OrderBy(columns[item.Column].Data + " " + item.Dir.ToString().Trim());
                        }

                        isThenBy = true;
                    }
                }
            }

            return source;
        }

        public static DatatablesPagedResultEntity<TEntity> GetByPaging<TEntity>(this IReadOnlyList<TEntity> data, int start, int length)
        {
            var size = data.Count();

            TEntity[] items;
            if (start > 0 && length > 0)
            {
                items = data
                .Skip((start - 1) * length)
                .Take(length)
                .ToArray();
            }
            else
            {
                items = data
                .ToArray();
            }

            return new DatatablesPagedResultEntity<TEntity>
            {
                Items = items,
                TotalSize = size
            };
        }

        public static async Task<DatatablesPagedResults<TEntity>> DatatablesSqlServerAsync<TEntity>(IDapperRepository dapperRepository, string primaryKeyFieldName, string sqlQuery, DatatablesParameter parameter)
        {
            var sqlDatatables = string.Empty;
            var whereColumn = WhereDataSql(parameter);
            var sortColumn = SortDataSql(parameter);

            var size = 0;
            var sqlSelect = string.Empty;
            var sqlFrom = string.Empty;
            var sqlWhere = string.Empty;
            var sqlSort = string.Empty;
            var sqlPaging = string.Empty;

            var sql = "SELECT {0} FROM {1} {2} {3} {4}";

            if (!string.IsNullOrEmpty(sqlQuery))
            {
                if (sqlQuery.ToLower().Contains(" from "))
                {
                    var sqlQuerySplitFrom = sqlQuery.ToLower().TrimStart().TrimEnd().Split(" from ");
                    if (sqlQuerySplitFrom.Length > 0)
                    {
                        sqlSelect = sqlQuerySplitFrom[0].Replace("select ", "");
                        sqlFrom = sqlQuerySplitFrom[1];

                        if (!string.IsNullOrEmpty(sqlFrom))
                        {
                            if (sqlFrom.ToLower().Contains(" where "))
                            {
                                var sqlQuerySplitWhere = sqlFrom.ToLower().TrimStart().TrimEnd().Split(" where ");
                                if (sqlQuerySplitWhere.Length > 0)
                                {
                                    sqlFrom = sqlQuerySplitWhere[0];
                                    sqlWhere = sqlQuerySplitWhere[1];

                                    if (!string.IsNullOrEmpty(sqlWhere))
                                    {
                                        if (sqlWhere.Contains(" order by "))
                                        {
                                            var sqlQuerySplitOrderBy = sqlWhere.ToLower().TrimStart().TrimEnd().Split(" order by ");
                                            if (sqlQuerySplitOrderBy.Length > 0)
                                            {
                                                sqlWhere = sqlQuerySplitOrderBy[0];
                                                sqlSort = sqlQuerySplitOrderBy[1];
                                            }
                                        }
                                    }
                                }
                            }

                            if (sqlFrom.Contains(" order by "))
                            {
                                var sqlQuerySplitOrderBy = sqlFrom.ToLower().TrimStart().TrimEnd().Split(" order by ");
                                if (sqlQuerySplitOrderBy.Length > 0)
                                {
                                    sqlFrom = sqlQuerySplitOrderBy[0];
                                    sqlSort = sqlQuerySplitOrderBy[1];
                                }
                            }

                            if (!string.IsNullOrEmpty(sqlWhere))
                            {
                                sqlWhere = " WHERE " + sqlWhere;
                            }

                            if (!string.IsNullOrEmpty(whereColumn))
                            {
                                if (!string.IsNullOrEmpty(sqlWhere))
                                {
                                    sqlWhere += " AND " + whereColumn;
                                }
                                else
                                {
                                    sqlWhere = " WHERE " + whereColumn;
                                }
                            }

                            if (!string.IsNullOrEmpty(sqlSort))
                            {
                                sqlSort = " ORDER BY " + sqlSort;
                            }

                            if (!string.IsNullOrEmpty(sortColumn))
                            {
                                sqlSort = " ORDER BY " + sortColumn;
                            }

                            sqlDatatables = string.Format(sql, sqlSelect, sqlFrom, sqlWhere, sqlSort, sqlPaging);
                        }
                    }
                }
            }

            TEntity[] items = { };
            if (!string.IsNullOrEmpty(sqlDatatables))
            {
                var sqlCountData = string.Format("SELECT COUNT({0}) FROM {1} {2}", primaryKeyFieldName, sqlFrom, sqlWhere);
                size = await dapperRepository.GetAsync<int>(sqlCountData, null, System.Data.CommandType.Text);

                if (parameter.Length > 0)
                {
                    var startRow = parameter.Start / parameter.Length * parameter.Length;

                    if (string.IsNullOrEmpty(sqlSort))
                    {
                        sqlSort = string.Format(" ORDER BY {0} ASC ", primaryKeyFieldName);
                    }

                    sqlPaging = string.Format(" OFFSET {0} ROWS FETCH NEXT {1} ROWS ONLY", startRow, parameter.Length);
                    sqlDatatables = string.Format(sql, sqlSelect, sqlFrom, sqlWhere, sqlSort, sqlPaging);
                    var source = await dapperRepository.GetAllAsync<TEntity>(sqlDatatables, null, System.Data.CommandType.Text);
                    items = source
                        .AsQueryable()
                        .ToArray();
                }
                else
                {
                    var source = await dapperRepository.GetAllListAsync<TEntity>(sqlDatatables, null, System.Data.CommandType.Text);
                    items = source
                            .AsQueryable()
                            .ToArray();
                }
            }

            return new DatatablesPagedResults<TEntity>
            {
                Items = items,
                TotalSize = size
            };
        }

        public static async Task<DatatablesPagedResults<TEntity>> DatatablesSqlServerAsync<TEntity>(IDapperRepository dapperRepository, string primaryKeyFieldName, string sqlSelectQuery, string sqlFromQuery, string sqlWhereQuery, string sqlGroupingQuery, string sqlSortQuery, DatatablesParameter parameter)
        {
            var sqlDatatables = string.Empty;
            var whereColumn = WhereDataSql(parameter);
            var sortColumn = SortDataSql(parameter);

            var size = 0;
            var sqlSelect = sqlSelectQuery;
            var sqlFrom = sqlFromQuery;
            var sqlWhere = sqlWhereQuery;
            var sqlGrouping = sqlGroupingQuery;
            var sqlSort = sqlSortQuery;
            var sqlPaging = string.Empty;

            var sql = "SELECT {0} FROM {1} {2} {3} {4} {5}";

            if (!string.IsNullOrEmpty(sqlSelect))
            {
                sqlDatatables = string.Format(sql, sqlSelect, sqlFrom, sqlWhere, sqlGrouping, sqlSort, sqlPaging);

                if (!string.IsNullOrEmpty(whereColumn))
                {
                    if (!string.IsNullOrEmpty(sqlWhere))
                    {
                        sqlWhere += " AND " + whereColumn;
                    }
                    else
                    {
                        sqlWhere = " WHERE " + whereColumn;
                    }
                }

                if (!string.IsNullOrEmpty(sortColumn))
                {
                    sqlSort = " ORDER BY " + sortColumn;
                }
            }

            TEntity[] items = { };
            if (!string.IsNullOrEmpty(sqlDatatables))
            {
                var sqlCountData = string.Format("SELECT COUNT({0}) FROM {1} {2}", primaryKeyFieldName, sqlFrom, sqlWhere);
                size = await dapperRepository.GetAsync<int>(sqlCountData, null, System.Data.CommandType.Text);

                if (parameter.Length > 0)
                {
                    var startRow = parameter.Start / parameter.Length * parameter.Length;

                    if (string.IsNullOrEmpty(sqlSort))
                    {
                        sqlSort = string.Format(" ORDER BY {0} ASC ", primaryKeyFieldName);
                    }

                    sqlPaging = string.Format(" OFFSET {0} ROWS FETCH NEXT {1} ROWS ONLY", startRow, parameter.Length);
                    sqlDatatables = string.Format(sql, sqlSelect, sqlFrom, sqlWhere, sqlGrouping, sqlSort, sqlPaging);
                    var source = await dapperRepository.GetAllAsync<TEntity>(sqlDatatables, null, System.Data.CommandType.Text);
                    items = source
                        .AsQueryable()
                        .ToArray();
                }
                else
                {
                    var source = await dapperRepository.GetAllListAsync<TEntity>(sqlDatatables, null, System.Data.CommandType.Text);
                    items = source
                            .AsQueryable()
                            .ToArray();
                }
            }

            return new DatatablesPagedResults<TEntity>
            {
                Items = items,
                TotalSize = size
            };
        }

        public static async Task<DataPagedResults<TEntity>> GetByPagingAsync<TEntity>(IQueryable<TEntity> data, int start, int length)
        {
            var size = await data.CountAsync();

            TEntity[] items;
            if (start > 0 && length > 0)
            {
                items = await data
                .Skip((start - 1) * length)
                .Take(length)
                .ToArrayAsync();
            }
            else
            {
                items = await data
                .ToArrayAsync();
            }

            return new DataPagedResults<TEntity>
            {
                Items = items,
                TotalSize = size
            };
        }

        private static string SelectColumnSql(DatatablesParameter parameter)
        {
            var columns = parameter.Columns.Where(x => x.Data.ToLower() != "actions").ToArray();

            var selectColumns = new List<string>();
            foreach (var item in columns)
            {
                selectColumns.Add(item.Data);
            }

            var result = string.Empty;
            if (selectColumns.Count > 0)
            {
                result = string.Join(",", selectColumns);
            }

            return result;
        }

        private static string WhereDataSql(DatatablesParameter parameter)
        {
            var searchCriteria = string.Empty;

            if (!string.IsNullOrEmpty(parameter.Search.Value))
            {
                var columns = parameter.Columns.Where(x => x.Searchable == true).ToArray();

                foreach (var item in columns)
                {
                    var fieldType = string.Empty;

                    if (item.Data.Split(".").Count() > 1)
                    {
                        fieldType = "string";
                    }
                    else
                    {
                        fieldType = item.Name;
                    }

                    if (fieldType == "string")
                    {
                        searchCriteria += string.Format(" LOWER({0}) LIKE '%{1}%' OR ", item.Data, parameter.Search.Value.ToLower());
                    }
                    else if (fieldType == "guid")
                    {
                        searchCriteria += string.Format(" {0} = '{1}' OR ", item.Data, parameter.Search.Value.ToLower());
                    }
                    else if (fieldType == "datetime" || fieldType == "date")
                    {
                        if (parameter.Search.Value.IsDate())
                        {
                            searchCriteria += string.Format(" {0} = '{1}' OR ", item.Data, DateTime.Parse(parameter.Search.Value).ToString("yyyy-MM-dd"));
                        }
                    }
                    else
                    {
                        if (parameter.Search.Value.IsNumber())
                        {
                            searchCriteria += string.Format(" {0} = {1} OR ", item.Data, DateTime.Parse(parameter.Search.Value).ToString("yyyy-MM-dd"));
                        }
                    }
                }

                searchCriteria = searchCriteria.Remove(searchCriteria.Length - 3, 3);
            }

            return searchCriteria;
        }

        private static string SortDataSql(DatatablesParameter parameter)
        {
            var columns = parameter.Columns.ToArray();
            var sortCriteria = string.Empty;

            if (parameter.Order.Count() > 0)
            {
                foreach (var item in parameter.Order)
                {
                    if (parameter.Columns[item.Column].Orderable)
                    {
                        sortCriteria = columns[item.Column].Data + " " + item.Dir.ToString().Trim();
                    }
                }
            }

            return sortCriteria;
        }
    }
}
