namespace TaskMaster.API.Models.Common
{
    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }

        public static PagedResult<T> Create(IEnumerable<T> items, int page, int pageSize, int totalCount)
        {
            var totalPages = pageSize > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 0;

            return new PagedResult<T>
            {
                Items = items.ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasPreviousPage = page > 1,
                HasNextPage = page < totalPages
            };
        }

        public static PagedResult<T> Unpaged(IEnumerable<T> items)
        {
            var materialized = items.ToList();
            return Create(materialized, 1, materialized.Count, materialized.Count);
        }
    }
}
