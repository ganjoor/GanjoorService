using RMuseum.Models.Artifact;
using RSecurityBackend.Models.Image;
using System;
using System.Collections.Generic;

namespace RMuseum.Models.PDFLibrary
{
    /// <summary>
    /// PDF Page
    /// </summary>
    public class PDFPage
    {
        /// <summary>
        /// Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// PDF Book Id
        /// </summary>
        public int PDFBookId { get; set; }

        /// <summary>
        /// page number
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// Thumbnail Image
        /// </summary>
        public virtual RImage ThumbnailImage { get; set; }

        /// <summary>
        /// Thumbnail Image Id
        /// </summary>
        public Guid? ThumbnailImageId { get; set; }

        /// <summary>
        /// external thumbnail image url
        /// </summary>
        public string ExtenalThumbnailImageUrl { get; set; }

        /// <summary>
        /// description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Last Modified 
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// Attributes
        /// </summary>
        public ICollection<RTagValue> Tags { get; set; }

        /// <summary>
        /// full resolution image width
        /// </summary>
        public int FullResolutionImageWidth { get; set; }

        /// <summary>
        /// full resolution image height
        /// </summary>
        public int FullResolutionImageHeight { get; set; }

        /// <summary>
        /// ocred
        /// </summary>
        public bool OCRed { get; set; }

        /// <summary>
        /// ocr date time
        /// </summary>
        public DateTime OCRTime { get; set; }

        /// <summary>
        /// page text
        /// </summary>
        public string PageText { get; set; }

    }
}
