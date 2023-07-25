using RMuseum.Models.Artifact;
using RSecurityBackend.Models.Image;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace RMuseum.Models.PDFLibrary
{
    /// <summary>
    /// a book might have different PDFBook or MultiVolumePDFCollection instances
    /// </summary>
    public class Book
    {
        /// <summary>
        /// Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// book name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// description
        /// </summary>
        public string Description { get; set; }

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
        /// Last Modified 
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// Authors
        /// </summary>
        public ICollection<AuthorRole> Authors { get; set; }

        /// <summary>
        /// Attributes
        /// </summary>
        public ICollection<RTagValue> Tags { get; set; }

        /// <summary>
        /// pdf books
        /// </summary>
        [NotMapped]
        public ICollection<PDFBook> PDFBooks { get; set; }
    }
}
