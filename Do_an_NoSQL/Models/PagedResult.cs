namespace Do_an_NoSQL.Models
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }

        public bool HasPrevious => CurrentPage > 1;
        public bool HasNext => CurrentPage < TotalPages;

        public static PagedResult<T> Create(List<T> source, int page, int pageSize)
        {
            var result = new PagedResult<T>();

            result.TotalItems = source.Count;
            result.PageSize = pageSize;
            result.CurrentPage = page;

            result.TotalPages = (int)Math.Ceiling(result.TotalItems / (double)pageSize);

            result.Items = source
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return result;
        }
    }
}
