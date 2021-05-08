namespace GanjooRazor.Models.BeepTunes
{
    /// <summary>
    /// beeptunes artist model
    /// </summary>
    public class BpArtistModel
    {
        /// <summary>
        /// id
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// artist name
        /// </summary>
        public string ArtisticName { get; set; }

        /// <summary>
        /// url
        /// </summary>
        public string Url { get { return $"https://beeptunes.com/artist/{Id}"; } }

        /// <summary>
        /// picture
        /// </summary>
        public string Picture { get; set; }
    }

    /// <summary>
    /// beeptunes track model
    /// </summary>
    public class BpTrackModel
    {
        /// <summary>
        /// id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Url
        /// </summary>
        public string Url { get { return $"https://beeptunes.com/track/{Id}"; } }

        /// <summary>
        /// album id
        /// </summary>
        public string Album_Id { get; set; }

        /// <summary>
        /// primary image
        /// </summary>
        public string PrimaryImage { get; set; }

        /// <summary>
        /// first artists
        /// </summary>
        public BpArtistModel[] FirstArtists { get; set; }
    }

    /// <summary>
    /// beeptunes search response model
    /// </summary>
    public class BpSearchResponseModel
    {
        /// <summary>
        /// tracks
        /// </summary>
        public BpTrackModel[] Tracks { get; set; }
        /// <summary>
        /// artists
        /// </summary>
        public BpArtistModel[] Artists { get; set; }
    }
}
