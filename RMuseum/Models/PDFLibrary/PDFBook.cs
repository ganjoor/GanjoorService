using RMuseum.Models.Artifact;
using RMuseum.Models.Artifact.ViewModels;
using RSecurityBackend.Models.Image;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

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
        /// book id
        /// </summary>
        public int BookId { get; set; }

        /// <summary>
        /// book
        /// </summary>
        public Book Book { get; set; }

        /// <summary>
        /// Publish Status
        /// </summary>
        public PublishStatus Status { get; set; }

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
        /// publishing number  (تیراژ)
        /// </summary>
        public int? PublishingNumber { get; set; }

        /// <summary>
        /// claimed page count
        /// </summary>
        public int? ClaimedPageCount { get; set; }

        /// <summary>
        /// MultiVolumePDFCollection Id
        /// </summary>
        public int? MultiVolumePDFCollectionId { get; set; }

        /// <summary>
        /// MultiVolumePDFCollection
        /// </summary>
        public virtual MultiVolumePDFCollection MultiVolumePDFCollection { get; set; }

        /// <summary>
        /// Volume Order
        /// </summary>
        public int VolumeOrder { get; set; }

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
        public virtual RImage CoverImage { get; set; }

        /// <summary>
        /// Cover Image Id
        /// </summary>
        public Guid? CoverImageId { get; set; }

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

        /// <summary>
        /// contributers
        /// </summary>
        public ICollection<AuthorRole> Contributers { get; set; }

        /// <summary>
        /// Pages
        /// </summary>
        public ICollection<PDFPage> Pages { get; set; }

        /// <summary>
        /// page count
        /// </summary>
        public int PageCount { get; set; }

        /// <summary>
        /// Attributes
        /// </summary>
        public ICollection<RTagValue> Tags { get; set; }

        /// <summary>
        /// file check sum
        /// </summary>
        public string FileMD5CheckSum { get; set; }

        /// <summary>
        /// original file name
        /// </summary>
        public string OriginalFileName { get; set; }

        /// <summary>
        /// storage folder name
        /// </summary>
        public string StorageFolderName { get; set; }

        /// <summary>
        /// Book Script Type
        /// </summary>
        public BookScriptType BookScriptType { get; set; }

        /// <summary>
        /// PDF Source Id
        /// </summary>
        public int? PDFSourceId { get; set; }

        /// <summary>
        /// PDF Source
        /// </summary>
        public virtual PDFSource PDFSource { get; set; }

        /// <summary>
        /// tags view models
        /// </summary>
        [NotMapped]
        public ICollection<RArtifactTagViewModel> ArtifactTags { get; set; }

        /// <summary>
        /// Binary Tagged Items
        /// </summary>
        [NotMapped]
        public ICollection<RTagSum> RTagSums { get; set; }

        /// <summary>
        /// Titles of Items in Contents
        /// </summary>
        [NotMapped]
        public ICollection<RTitleInContents> Contents { get; set; }

        /// <summary>
        /// ocred
        /// </summary>
        public bool OCRed { get; set; }

        /// <summary>
        /// ocr date time
        /// </summary>
        public DateTime OCRTime { get; set; }

        /// <summary>
        /// book text
        /// </summary>
        public string BookText { get; set; }
    }
}
