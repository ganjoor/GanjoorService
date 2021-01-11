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
        /// albums
        /// </summary>
        public ICollection<GanjoorAlbum> Albums { get; set; }
    }
}
