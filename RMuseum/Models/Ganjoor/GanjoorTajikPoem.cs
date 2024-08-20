using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace RMuseum.Models.Ganjoor
{
    public class GanjoorTajikPoem
    {
        /// <summary>
        /// id
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        /// <summary>
        /// cat id
        /// </summary>
        public int CatId { get; set; }

        /// <summary>
        /// title
        /// </summary>
        public string TajikTitle { get; set; }

        /// <summary>
        /// verses text
        /// </summary>
        public string TajikPlainText { get; set; }

        /// <summary>
        /// sample: /hafez/ghazal/sh1
        /// </summary>
        public string FullUrl { get; set; }

        /// <summary>
        /// cat + parent cats title + title
        /// </summary>
        public string FullTitle { get; set; }

        /// <summary>
        /// cat
        /// </summary>
        public GanjoorTajikCat Cat { get; set; }

    }
}
