using System;

namespace RMuseum.Models.Ganjoor.ViewModels
{
    /// <summary>
    /// moderation view model
    /// </summary>
    public class GanjoorQuotedPoemModerationViewModel
    {
        /// <summary>
        /// record id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// approved
        /// </summary>
        public bool Approved { get; set; }

        /// <summary>
        /// review note
        /// </summary>
        public string ReviewNote { get; set; }
    }
}
