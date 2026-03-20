namespace PlantDecor.DataAccessLayer.Helpers
{
    public class PaginatedResult<T>
    {
        // Danh sách các item muốn get ra
        public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
        // Các thông tin về phân trang
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        // Helpers để biết có trang trước/trang sau hay không
        public bool HasPrevious => PageNumber > 1;
        public bool HasNext => PageNumber < TotalPages;

        public PaginatedResult() { }

        public PaginatedResult(IEnumerable<T> items, int totalCount, int pageNumber, int pageSize)
        {
            Items = items;
            TotalCount = totalCount;
            PageNumber = pageNumber;
            PageSize = pageSize;
        }
    }
}
