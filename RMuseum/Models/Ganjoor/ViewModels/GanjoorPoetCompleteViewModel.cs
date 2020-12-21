using System.Collections.Generic;

namespace RMuseum.Models.Ganjoor.ViewModels
{
    /// <summary>
    /// Poet Complete Information 
    /// </summary>
    public class GanjoorPoetCompleteViewModel
    {
        /// <summary>
        /// poet info
        /// </summary>
        public GanjoorPoet Poet { get; set; }

        /// <summary>
        /// poet cat info
        /// </summary>
        public GanjoorCat Cat { get; set; }

        /// <summary>
        /// poet cat children
        /// </summary>
        public ICollection<GanjoorCat> Children { get; set; }

        /// <summary>
        /// poems
        /// </summary>
        public ICollection<GanjoorPoem> Poems { get; set; }
    }
}
