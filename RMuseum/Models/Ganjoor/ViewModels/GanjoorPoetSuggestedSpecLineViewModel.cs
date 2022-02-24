using System;

namespace RMuseum.Models.Ganjoor.ViewModels
{
    /// <summary>
    /// Ganjoor Poet Specification view model
    /// </summary>
    public class GanjoorPoetSuggestedSpecLineViewModel
    {
        /// <summary>
        /// Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Poet Id
        /// </summary>
        public int PoetId { get; set; }

        /// <summary>
        /// order
        /// </summary>
        public int LineOrder { get; set; }

        /// <summary>
        /// Contents
        /// </summary>
        public string Contents { get; set; }

        /// <summary>
        /// published
        /// </summary>
        public bool Published { get; set; }

        /// <summary>
        /// suggested by id
        /// </summary>
        public Guid? SuggestedById { get; set; }

        /// <summary>
        /// rejection cause
        /// </summary>
        public string RejectionCause { get; set; }
    }
}
