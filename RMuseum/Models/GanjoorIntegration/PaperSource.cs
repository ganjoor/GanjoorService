using System;

namespace RMuseum.Models.GanjoorIntegration
{
    /// <summary>
    /// Ganjoor Paper Source
    /// </summary>
    public class PaperSource
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// poet id
        /// </summary>
        public int GanjoorPoetId { get; set; }

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
        /// book type
        /// </summary>
        public LinkType BookType { get; set; }

        /// <summary>
        /// book full url
        /// </summary>
        public string BookFullUrl { get; set; }

        /// <summary>
        /// naskban book id
        /// </summary>
        public int NaskbanBookId { get; set; }

        /// <summary>
        /// book title
        /// </summary>
        public string BookFullTitle { get; set; }

        /// <summary>
        /// description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        ///  is the text original source for the category?
        /// </summary>
        public bool IsTextOriginalSource { get; set; }

        /// <summary>
        /// cover thumbnail image url
        /// </summary>
        public string CoverThumbnailImageUrl { get; set; }

        /// <summary>
        /// match percent
        /// </summary>
        public int MatchPercent { get; set; }

        /// <summary>
        /// reviewed by a human
        /// </summary>
        public bool HumanReviewed { get; set; }
    }
}
