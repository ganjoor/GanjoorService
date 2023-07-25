namespace RMuseum.Models.PDFLibrary.ViewModels
{
    /// <summary>
    /// New PDFBook view model
    /// </summary>
    public class NewPDFBookViewModel
    {
        /// <summary>
        /// PDF File Path
        /// </summary>
        public string LocalImportingPDFFilePath { get; set; }

        /// <summary>
        /// skip uploading to external FTP Site
        /// </summary>
        public bool SkipUpload { get; set; }

        /// <summary>
        /// book id
        /// </summary>
        public int BookId { get; set; }

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
        /// publishing number (تیراژ)
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
        /// Volume Order
        /// </summary>
        public int VolumeOrder { get; set; }

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
        /// writer id
        /// </summary>
        public int? WriterId { get; set; }

        /// <summary>
        /// writer 2 id
        /// </summary>
        public int? Writer2Id { get; set; }

        /// <summary>
        /// writer 3 id
        /// </summary>
        public int? Writer3Id { get; set; }

        /// <summary>
        /// writer 4 id
        /// </summary>
        public int? Writer4Id { get; set; }

        /// <summary>
        /// translator id
        /// </summary>
        public int? TranslatorId { get; set; }

        /// <summary>
        /// translator 2 id
        /// </summary>
        public int? Translator2Id { get; set; }

        /// <summary>
        /// translator 3 id
        /// </summary>
        public int? Translator3Id { get; set; }

        /// <summary>
        /// translator 4 id
        /// </summary>
        public int? Translator4Id { get; set; }

        /// <summary>
        /// collector id (مصحح)
        /// </summary>
        public int? CollectorId { get; set; }

        /// <summary>
        /// collector 2 id (مصحح)
        /// </summary>
        public int? Collector2Id { get; set; }

        /// <summary>
        /// Other contributing role
        /// </summary>
        public string OtherContributerRole { get; set; }

        /// <summary>
        /// other contributing role
        /// </summary>
        public int? OtherContributerId { get; set; }

        /// <summary>
        /// Other contributing role 2
        /// </summary>
        public string OtherContributer2Role { get; set; }

        /// <summary>
        /// other contributing role 2
        /// </summary>
        public int? OtherContributer2Id { get; set; }

        /// <summary>
        /// PDF Source Id
        /// </summary>
        public int? PDFSourceId { get; set; }

        /// <summary>
        /// Book Script Type
        /// </summary>
        public BookScriptType BookScriptType { get; set; }

    }
}
