namespace RSecurityBackend.Models.Generic
{
    /// <summary>
    /// pagination metadat
    /// </summary>
    public class PaginationMetadata
    {
        /// <summary>
        /// total count
        /// </summary>
        public int totalCount { get; set; }

        /// <summary>
        /// page size
        /// </summary>
        public int pageSize { get; set; }

        /// <summary>
        /// current page
        /// </summary>
        public int currentPage { get; set; }

        /// <summary>
        /// total pages
        /// </summary>
        public int totalPages { get; set; }

        /// <summary>
        /// has previous page
        /// </summary>
        public bool hasPreviousPage { get; set; }

        /// <summary>
        /// has next page
        /// </summary>
        public bool hasNextPage { get; set; }
    }
}
