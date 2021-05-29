using System;

namespace GanjooRazor.Models.MuseumLink
{
    /// <summary>
    /// image link finalize model
    /// </summary>
    public class ImageLinkFinalizeModel
    {
        /// <summary>
        /// link id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// display on page
        /// </summary>
        public bool DisplayOnPage { get; set; }
    }
}
