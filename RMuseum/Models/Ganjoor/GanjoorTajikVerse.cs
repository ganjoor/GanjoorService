using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace RMuseum.Models.Ganjoor
{
    public class GanjoorTajikVerse
    {
        /// <summary>
        /// id
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        /// <summary>
        /// poem_id
        /// </summary>
        public int PoemId { get; set; }

        /// <summary>
        /// vorder
        /// </summary>
        public int VOrder { get; set; }


        /// <summary>
        /// tajik text
        /// </summary>
        public string TajikText { get; set; }

    }
}
