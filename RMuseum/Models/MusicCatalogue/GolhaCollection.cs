using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RMuseum.Models.MusicCatalogue
{
    /// <summary>
    /// http://www.golha.co.uk offline catalogue
    /// </summary>
    public class GolhaCollection
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
        /// url
        /// </summary>
        public string Url { get { return $"http://www.golha.co.uk/fa/search_basic/{Id}"; } }
    }
}
