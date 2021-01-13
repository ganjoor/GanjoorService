using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RMuseum.Models.MusicCatalogue
{
    public class GolhaTrack
    {
        /// <summary>
        /// id
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
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

        /// <summary>
        /// singer id
        /// </summary>
        public int? SingerId { get; set; }

        /// <summary>
        /// singer
        /// </summary>
        public virtual GanjoorSinger Singer { get; set; }

        /// <summary>
        /// blocked from suggestion
        /// </summary>
        public bool Blocked { get; set; }

        /// <summary>
        /// reason
        /// </summary>
        /// <example>
        /// track is purely music and no singing is done through it
        /// </example>
        public string BlockReason { get; set; }
    }
}
