using Microsoft.AspNetCore.Http;


namespace GanjooRazor.Models
{
    /// <summary>
    /// banner upload model
    /// </summary>
    public class BannerUploadModel
    {
        /// <summary>
        /// image
        /// </summary>
        public IFormFile Image { get; set; }

        /// <summary>
        /// altername text
        /// </summary>
        public string Alt { get; set; }

        /// <summary>
        /// target url
        /// </summary>
        public string Url { get; set; }
    }
}
