using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// Ganjoor Poet
    /// </summary>
    /// <remarks>
    /// cat_id field is removed, it is retrievable by querying <see cref="RMuseum.Models.Ganjoor.GanjoorCat"/> 
    /// where <see cref="RMuseum.Models.Ganjoor.GanjoorCat.PoetId"/> == <see cref="RMuseum.Models.Ganjoor.GanjoorPoet.Id"/> and
    /// <see cref="RMuseum.Models.Ganjoor.GanjoorCat.Parent"/> == null
    /// </remarks>
    public class GanjoorPoet
    {
        /// <summary>
        /// id
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        /// <summary>
        /// name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// description
        /// </summary>
        public string Description { get; set; }
    }
}
