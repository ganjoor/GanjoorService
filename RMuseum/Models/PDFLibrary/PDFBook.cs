using RSecurityBackend.Models.Image;
using System;

namespace RMuseum.Models.PDFLibrary
{
    /// <summary>
    /// PDF Book
    /// </summary>
    public class PDFBook
    {
        /// <summary>
        /// Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Friendly Url
        /// </summary>
        public string FriendlyUrl { get; set; }

        /// <summary>
        /// Title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// secondary or complementary title
        /// </summary>
        public string SubTitle { get; set; }

        /// <summary>
        /// authors (descriptive)
        /// </summary>
        public string AuthorsLine { get; set; }

        /// <summary>
        /// ISBN
        /// </summary>
        public string ISBN { get; set; }

        /// <summary>
        /// description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// language
        /// </summary>
        public string Language { get; set; }

        /// <summary>
        /// is translation
        /// </summary>
        public bool IsTranslation { get; set; }

        /// <summary>
        /// translators (descriptive)
        /// </summary>
        public string TranslatorsLine { get; set; }

        /// <summary>
        /// title in original language
        /// </summary>
        public string TitleInOriginalLanguage { get; set; }

        /// <summary>
        /// publisher (descriptive)
        /// </summary>
        public string PublisherLine { get; set; }

        /// <summary>
        /// publishing date (descriptive)
        /// </summary>
        public string PublishingDate { get; set; }

        /// <summary>
        /// publishing location (descriptive)
        /// </summary>
        public string PublishingLocation { get; set; }

        /// <summary>
        /// publishing number
        /// </summary>
        public int? PublishingNumber { get; set; }

        /// <summary>
        /// claimed page count
        /// </summary>
        public int? ClaimedPageCount { get; set; }

        /// <summary>
        /// Date/Time
        /// </summary>
        public DateTime DateTime { get; set; }

        /// <summary>
        /// Last Modified for caching purposes
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// PDF File
        /// </summary>
        public RImage PDFFile { get; set; }

        /// <summary>
        /// external url for PDF File
        /// </summary>
        public string ExternalPDFFileUrl { get; set; }

        /// <summary>
        /// Cover Image
        /// </summary>
        public RImage CoverImage { get; set; }

        /// <summary>
        /// external cover image url
        /// </summary>
        public string ExtenalCoverImageUrl { get; set; }

        /// <summary>
        /// original source name
        /// </summary>
        public string OriginalSourceName { get; set; }

        /// <summary>
        /// original source url
        /// </summary>
        public string OriginalSourceUrl { get; set; }

        /// <summary>
        /// original file url
        /// </summary>
        public string OriginalFileUrl { get; set; }
    }
}
