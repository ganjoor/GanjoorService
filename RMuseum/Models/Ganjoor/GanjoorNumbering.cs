using System;

namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// Numbering Schema
    /// </summary>
    public class GanjoorNumbering
    {
        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// starting category id
        /// </summary>
        public int StartCatId { get; set; }

        /// <summary>
        /// ending category
        /// </summary>
        public GanjoorCat StartCat { get; set; }

        /// <summary>
        /// ending category id
        /// </summary>
        public int? EndCatId { get; set; }

        /// <summary>
        /// ending category
        /// </summary>
        public virtual GanjoorCat EndCat { get; set; }

        /// <summary>
        /// lines count
        /// </summary>
        public int TotalLines { get; set; }

        /// <summary>
        /// verse count
        /// </summary>
        public int TotalVerses { get; set; }

        /// <summary>
        /// total poem couplets
        /// </summary>
        public int TotalCouplets { get; set; }

        /// <summary>
        /// total paragraphs
        /// </summary>
        public int TotalParagraphs { get; set; }

        /// <summary>
        /// last counting date
        /// </summary>
        public DateTime LastCountingDate { get; set; }
    }
}
