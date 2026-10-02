namespace RMuseum.Models.Ganjoor.ViewModels
{
    /// <summary>
    /// A GanjoorCat whose CatType is Book - a minimal view model for listing/searching books
    /// (e.g. the home page's book search box), not the full category details
    /// </summary>
    public class GanjoorBookViewModel
    {
        /// <summary>
        /// id (GanjoorCat.Id)
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// book name (GanjoorCat.Title)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// full url, sample: /hafez/ghazal
        /// </summary>
        public string FullUrl { get; set; }

        /// <summary>
        /// poet id (GanjoorCat.PoetId)
        /// </summary>
        public int PoetId { get; set; }

        /// <summary>
        /// poet short name (GanjoorPoet.Nickname)
        /// </summary>
        public string PoetName { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
