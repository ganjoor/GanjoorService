namespace RSecurityBackend.Models.Generic
{
    /// <summary>
    /// Paging Parameter Model
    /// </summary>
    /// <remarks>
    /// https://www.c-sharpcorner.com/article/how-to-do-paging-with-asp-net-web-api/
    /// </remarks>
    public class PagingParameterModel
    {
        /// <summary>
        /// max page size
        /// </summary>
        const int MaxPageSize = 1000;

        /// <summary>
        /// page number
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// actual page size
        /// </summary>
        private int _pageSize { get; set; } = -1;

        /// <summary>
        /// settable  page size
        /// </summary>
        public int PageSize
        {

            get { return _pageSize; }
            set
            {
                _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
            }
        }
    }
}
