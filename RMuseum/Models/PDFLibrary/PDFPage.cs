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

    }
}
