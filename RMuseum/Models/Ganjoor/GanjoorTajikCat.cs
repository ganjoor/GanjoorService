using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace RMuseum.Models.Ganjoor
{
    public class GanjoorTajikCat
    {
        /// <summary>
        /// id
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        /// <summary>
        /// tajik title
        /// </summary>
        public string TajikTitle { get; set; }

        /// <summary>
        /// additional description or note in Tajik
        /// </summary>
        public string TajikDescription { get; set; }

        /// <summary>
        /// poet_id
        /// </summary>
        public int PoetId { get; set; }

        /// <summary>
        /// poet
        /// </summary>
        public GanjoorTajikPoet Poet { get; set; }


    }
}
