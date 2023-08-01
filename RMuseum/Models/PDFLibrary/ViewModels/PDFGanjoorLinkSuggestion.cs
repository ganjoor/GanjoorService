namespace RMuseum.Models.PDFLibrary.ViewModels
{
    /// <summary>
    /// PDFBook Ganjoor Link Suggestion
    /// </summary>
    public class PDFGanjoorLinkSuggestion
    {
        /// <summary>
        /// Ganjoor Post Id
        /// </summary>
        public int GanjoorPostId { get; set; }

        /// <summary>
        /// ganjoor url
        /// </summary>
        public string GanjoorUrl { get; set; }

        /// <summary>
        /// ganjoor title
        /// </summary>
        public string GanjoorTitle { get; set; }

        /// <summary>
        /// pdf book id
        /// </summary>
        public int PDFBookId { get; set; }

        /// <summary>
        /// page number
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// is this the text original source?
        /// </summary>
        public bool IsTextOriginalSource { get; set; }
    }
}
