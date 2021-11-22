using System;

namespace RMuseum.Models.Note.ViewModels
{
    /// <summary>
    /// user note report view model
    /// </summary>
    public class PostRUserNoteAbuseReportViewModel
    {
        /// <summary>
        /// note Id
        /// </summary>
        public Guid NoteId { get; set; }

        /// <summary>
        /// reason text
        /// </summary>
        public string ReasonText { get; set; }
    }
}
