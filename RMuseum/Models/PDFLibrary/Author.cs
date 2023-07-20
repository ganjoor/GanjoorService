using RSecurityBackend.Models.Image;
using System;

namespace RMuseum.Models.PDFLibrary
{
    /// <summary>
    /// Author
    /// </summary>
    public class Author
    {
        /// <summary>
        /// Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Name in original language
        /// </summary>
        public string NameInOriginalLanguage { get; set; }

        /// <summary>
        /// bio
        /// </summary>
        public string Bio { get; set; }

        /// <summary>
        /// Cover Image
        /// </summary>
        public virtual RImage Image { get; set; }

        /// <summary>
        /// Cover Image Id
        /// </summary>
        public Guid? ImageId { get; set; }

        /// <summary>
        /// external image url
        /// </summary>
        public string ExtenalImageUrl { get; set; }

        /// <summary>
        /// Last Modified 
        /// </summary>
        public DateTime LastModified { get; set; }
    }
}
