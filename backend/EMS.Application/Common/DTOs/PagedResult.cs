using System.Collections.Generic;

namespace EMS.Application.Common.DTOs
{
    public class PagedResult<T>
    {
        public IEnumerable<T> Data { get; set; } = [];
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => PageSize > 0 ? (TotalCount + PageSize - 1) / PageSize : 0;

        public static PagedResult<T> Create(IEnumerable<T> data, int page, int pageSize, int totalCount) =>
            new() { Data = data, Page = page, PageSize = pageSize, TotalCount = totalCount };
    }
}
