using System.Collections.Generic;

namespace RMuseum.Models.Ganjoor.ViewModels
{
    /// <summary>
    /// cat view model
    /// </summary>
    public class GanjoorCatViewModel
    {
        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// text
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// url => slug
        /// </summary>
        public string UrlSlug { get; set; }

        /// <summary>
        /// ancestors
        /// </summary>
        public ICollection<GanjoorCatViewModel> Ancestors { get; set; }

        /// <summary>
        /// cat children
        /// </summary>
        public ICollection<GanjoorCatViewModel> Children { get; set; }

        /// <summary>
        /// poems
        /// </summary>
        public ICollection<GanjoorPoemSummaryViewModel> Poems { get; set; }

       
    }
}
