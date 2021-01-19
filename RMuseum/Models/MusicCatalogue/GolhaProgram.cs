using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RMuseum.Models.MusicCatalogue
{
    /// <summary>
    /// Golha Program
    /// </summary>
    public class GolhaProgram
    {
        /// <summary>
        /// id
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        /// <summary>
        /// GolhaCollection Id
        /// </summary>
        public int GolhaCollectionId { get; set; }

        /// <summary>
        /// GolhaCollection
        /// </summary>
        public GolhaCollection GolhaCollection { get; set; }

        /// <summary>
        /// title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// program order
        /// </summary>
        public int ProgramOrder { get; set; }

        /// <summary>
        /// url
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// mp3
        /// </summary>
        public string Mp3 { get; set; }

        /// <summary>
        /// tracks
        /// </summary>
        public ICollection<GolhaTrack> Tracks { get; set; }
    }
}
