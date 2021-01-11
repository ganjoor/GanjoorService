using System.Collections.Generic;

namespace RMuseum.Models.MusicCatalogue
{
    /// <summary>
    /// music catalogue album
    /// </summary>
    public class GanjoorAlbum
    {
        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// singer id
        /// </summary>
        public int SingerId { get; set; }

        /// <summary>
        /// singer
        /// </summary>
        public GanjoorSinger Singer { get; set; }

        /// <summary>
        /// name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// url
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// tracks
        /// </summary>
        public ICollection<GanjoorTrack> Tracks { get; set; }
    }
}
