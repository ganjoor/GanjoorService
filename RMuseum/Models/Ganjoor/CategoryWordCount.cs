using System;

namespace RMuseum.Models.Ganjoor
{
    public class CategoryWordCount
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
        /// Word
        /// </summary>
        public string Word { get; set; }

        /// <summary>
        /// Count
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// is root cat
        /// </summary>
        public bool PoetCat { get; set; }
    }
}
