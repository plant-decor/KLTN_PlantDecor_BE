namespace PlantDecor.DataAccessLayer.Helpers
{
    public class Pagination
    {
        private int _pageNumber = 1;
        private int _pageSize = 10;

        public int PageNumber
        {
            get => _pageNumber;
            set => _pageNumber = value > 0 ? value : 1;
        }

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > 0 && value <= 100 ? value : 10; // Giới hạn max 100 items per page
        }

        // Skip items for database query
        public int Skip => (PageNumber - 1) * PageSize;

        // Take items for database query
        public int Take => PageSize;

        // Constructor with default values
        public Pagination() { }

        public Pagination(int pageNumber, int pageSize)
        {
            PageNumber = pageNumber;
            PageSize = pageSize;
        }
    }
}
