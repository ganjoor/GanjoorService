using RMuseum.Models.MusicCatalogue;

namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// poem related track
    /// </summary>
    public class PoemMusicTrack
    {
        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// poem id
        /// </summary>
        public int PoemId { get; set; }

        /// <summary>
        /// poem
        /// </summary>
        public GanjoorPoem Poem { get; set; }

        /// <summary>
        /// track type
        /// </summary>
        public PoemMusicTrackType TrackType { get; set; }

        /// <summary>
        /// artist name
        /// </summary>
        public string ArtistName { get; set; }

        /// <summary>
        /// artist url
        /// </summary>
        public string ArtistUrl { get; set; }

        /// <summary>
        /// album name
        /// </summary>
        public string AlbumName { get; set; }

        /// <summary>
        /// album url
        /// </summary>
        public string AlbumUrl { get; set; }

        /// <summary>
        /// track name
        /// </summary>
        public string TrackName { get; set; }

        /// <summary>
        /// track url
        /// </summary>
        public string TrackUrl { get; set; }

        /// <summary>
        /// GanjoorTrack Id
        /// </summary>
        public int? GanjoorTrackId { get; set; }

        /// <summary>
        /// GanjoorTrack
        /// </summary>
        public virtual GanjoorTrack GanjoorTrack { get; set; }

        /// <summary>
        /// GolhaTrack Id
        /// </summary>
        public int? GolhaTrackId { get; set; }

        /// <summary>
        /// GolhaTrack
        /// </summary>
        public virtual GolhaTrack GolhaTrack { get; set; }

        /// <summary>
        /// description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// broken link
        /// </summary>
        public bool BrokenLink { get; set; }
    }
}
