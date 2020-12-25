namespace RMuseum.Models.Ganjoor.ViewModels
{
    /// <summary>
    /// Ganjoor Poet
    /// </summary>
    public class GanjoorPoetViewModel
    {
        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// urlslug
        /// </summary>
        public string FullUrl { get; set; }

        /// <summary>
        /// root cat id
        /// </summary>
        public int RootCatId { get; set; }
    }
}
