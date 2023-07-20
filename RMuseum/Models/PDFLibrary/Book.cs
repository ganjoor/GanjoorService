using RMuseum.Models.Artifact;
using System.Collections.Generic;

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
        /// Attributes
        /// </summary>
        public ICollection<RTagValue> Tags { get; set; }
    }
}
