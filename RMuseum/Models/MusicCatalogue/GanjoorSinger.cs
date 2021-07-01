using RSecurityBackend.Models.Image;
using System;
using System.Collections.Generic;

namespace RMuseum.Models.MusicCatalogue
{
    /// <summary>
    /// music catalogue singer
    /// </summary>
    public class GanjoorSinger
    {
        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// url
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// poet image
        /// </summary>
        public virtual RImage RImage { get; set; }

        /// <summary>
        /// user image id
        /// </summary>
        public Guid? RImageId { get; set; }

        /// <summary>
        /// albums
        /// </summary>
        public ICollection<GanjoorAlbum> Albums { get; set; }

    }
}
