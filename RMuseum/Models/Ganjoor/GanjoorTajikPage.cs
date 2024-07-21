using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace RMuseum.Models.Ganjoor
{
    public class GanjoorTajikPage
    {
        /// <summary>
        /// id
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        /// <summary>
        /// verses text as html (ganjoor.net format)
        /// </summary>
        public string TajikHtmlText { get; set; }

    }
}
