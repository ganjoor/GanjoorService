using System;

namespace RMuseum.Models.Note.ViewModels
{
    /// <summary>
    /// (Public) User Notes Abuse Report View Model
    /// </summary>
    public class RUserNoteAbuseReportViewModel
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// some explanotory text provided by reporter
        /// </summary>
        public string ReasonText { get; set; }

        /// <summary>
        /// note
        /// </summary>
        public RUserNoteViewModel Note { get; set; }
    }
}
