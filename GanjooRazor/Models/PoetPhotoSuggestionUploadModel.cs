using Microsoft.AspNetCore.Http;

namespace GanjooRazor.Models
{
    /// <summary>
    /// poet photo suggestion upload model
    /// </summary>
    public class PoetPhotoSuggestionUploadModel
    {
        /// <summary>
        /// image
        /// </summary>
        public IFormFile Image { get; set; }

        /// <summary>
        /// poet id
        /// </summary>
        public int PoetId { get; set; }

        /// <summary>
        /// title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// source url
        /// </summary>
        public string SrcUrl { get; set; }

    }
}
