using System;

namespace RMuseum.Models.PDFLibrary
{
    public class GanjoorPoemMatchFinding
    {
        /// <summary>
        /// id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// cat id
        /// </summary>
        public int GanjoorCatId { get; set; }

        /// <summary>
        /// cat full title
        /// </summary>
        public string GanjoorCatFullTitle { get; set; }

        /// <summary>
        /// cat full url
        /// </summary>
        public string GanjoorCatFullUrl { get; set; }

        /// <summary>
        /// start from this poem
        /// </summary>
        public int GanjoorPoemId { get; set; } = 0;

        /// <summary>
        /// book id
        /// </summary>
        public int BookId { get; set; }

        /// <summary>
        /// book title
        /// </summary>
        public string BookTitle { get; set; }

        /// <summary>
        /// page number
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// threshold
        /// </summary>
        public int Threshold { get; set; }

        /// <summary>
        /// queue time
        /// </summary>
        public DateTime QueueTime { get; set; }

        /// <summary>
        /// started
        /// </summary>
        public bool Started { get; set; }

        /// <summary>
        /// start time
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// last update
        /// </summary>
        public DateTime? LastUpdate { get; set; }

        /// <summary>
        /// updated by user
        /// </summary>
        public Guid? LastUpdatedByUserId { get; set; }

        /// <summary>
        /// current poem id
        /// </summary>
        public int CurrentPoemId { get; set; }

        /// <summary>
        /// progress
        /// </summary>
        public int Progress { get; set; }

        /// <summary>
        /// current page number
        /// </summary>
        public int CurrentPageNumber { get; set; }

        /// <summary>
        /// finished
        /// </summary>
        public bool Finished { get; set; }

        /// <summary>
        /// finish time
        /// </summary>
        public DateTime? FinishTime { get; set; }
    }
}
