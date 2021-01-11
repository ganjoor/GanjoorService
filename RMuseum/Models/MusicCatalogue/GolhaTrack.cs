namespace RMuseum.Models.MusicCatalogue
{
    public class GolhaTrack
    {
        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// GolhaProgram Id
        /// </summary>
        public int GolhaProgramId { get; set; }

        /// <summary>
        /// GolhaProgram
        /// </summary>
        public GolhaProgram GolhaProgram { get; set; }

        /// <summary>
        /// track no
        /// </summary>
        public int TrackNo { get; set; }

        /// <summary>
        /// timing
        /// </summary>
        public string Timing { get; set; }

        /// <summary>
        /// title
        /// </summary>
        public string Title { get; set; }
    }
}
