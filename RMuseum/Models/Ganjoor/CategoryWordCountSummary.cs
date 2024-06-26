using System;

namespace RMuseum.Models.Ganjoor
{
    public class CategoryWordCountSummary
    {
        /// <summary>
        /// id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// CatId
        /// </summary>
        public int CatId { get; set; }

        /// <summary>
        /// unique word count
        /// </summary>
        public int UniqueWordCount { get; set; }

        /// <summary>
        /// total word count
        /// </summary>
        public int TotalWordCount { get; set; }

    }
}
