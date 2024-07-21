using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace RMuseum.Models.Ganjoor
{
    public class TajikPoet
    {
        /// <summary>
        /// id
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        /// <summary>
        /// tajik nick name
        /// </summary>
        public string TajikNickName { get; set; }

        /// <summary>
        /// additional descripion or note in Tajik
        /// </summary>
        public string TajikDescription { get; set; }
    }
}
