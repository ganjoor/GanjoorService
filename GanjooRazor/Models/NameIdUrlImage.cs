namespace GSpotifyProxy.Models
{
    public class NameIdUrlImage
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public string Url { get; set; }
        public string Image { get; set; }
    }


    public class TrackQueryResult : NameIdUrlImage
    {
        public string ArtistName { get; set; }
        public string ArtistId { get; set; }
        public string ArtistUrl { get; set; }
        public string AlbumName { get; set; }
        public string AlbumId { get; set; }
        /// <summary>
        /// typo on service side, so let it be unfixed for now
        /// </summary>
        public string AlbunUrl { get; set; }
    }
}
