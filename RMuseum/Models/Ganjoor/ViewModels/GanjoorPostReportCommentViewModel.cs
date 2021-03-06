namespace RMuseum.Models.Ganjoor.ViewModels
{
    /// <summary>
    /// report comment view model
    /// </summary>
    public class GanjoorPostReportCommentViewModel
    {
        /// <summary>
        /// comment id
        /// </summary>
        public int CommentId { get; set; }
        /// <summary>
        /// reason code for better grouping: offensive, bogus, other
        /// </summary>
        public string ReasonCode { get; set; }

        /// <summary>
        /// some explanotory text provided by reporter
        /// </summary>
        public string ReasonText { get; set; }
    }
}
