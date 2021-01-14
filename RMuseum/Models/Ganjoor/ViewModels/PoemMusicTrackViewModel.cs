namespace RMuseum.Models.Ganjoor.ViewModels
{
    /// <summary>
    /// Ganjoor Poem related music track model
    /// </summary>
    public class PoemMusicTrackViewModel
    {
        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }

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
        /// description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// broken link
        /// </summary>
        public bool BrokenLink { get; set; }

        /// <summary>
        /// to string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return TrackType == PoemMusicTrackType.Golha ? $"{AlbumName} » {TrackName}" : $"{ArtistName} » {AlbumName} » {TrackName}";
        }
    }
}
