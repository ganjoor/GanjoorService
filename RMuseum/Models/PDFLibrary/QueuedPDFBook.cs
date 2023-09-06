using System;

namespace RMuseum.Models.PDFLibrary
{
    /// <summary>
    /// a fake PDFBook for scheduling downloads
    /// </summary>
    public class QueuedPDFBook
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Download Order
        /// </summary>
        public int DownloadOrder { get; set; }

        /// <summary>
        /// Title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// authors (descriptive)
        /// </summary>
        public string AuthorsLine { get; set; }

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
        /// processed
        /// </summary>
        public bool Processed { get; set; }

        /// <summary>
        /// process result
        /// </summary>
        public string ProcessResult { get; set; }

        /// <summary>
        /// result id
        /// </summary>
        public int ResultId { get; set; }
    }
}
