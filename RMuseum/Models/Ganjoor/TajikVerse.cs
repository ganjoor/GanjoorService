using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace RMuseum.Models.Ganjoor
{
    public class TajikVerse
    {
        /// <summary>
        /// id
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        /// <summary>
        /// tajik text
        /// </summary>
        public string TajikText { get; set; }

    }
}
