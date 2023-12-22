using System;

namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// discover quoted q item
    /// </summary>
    public class DiscoverQuotedQueueItem
    {
        /// <summary>
        /// id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// poet id
        /// </summary>
        public int PoetId { get; set; }

        /// <summary>
        /// poem id
        /// </summary>
        public int PoemId { get; set; }

        /// <summary>
        /// related poet id
        /// </summary>
        public int RelatedPoetId { get; set; }

        /// <summary>
        /// related poem id
        /// </summary>
        public int RelatedPoemId { get; set; }
    }
}
