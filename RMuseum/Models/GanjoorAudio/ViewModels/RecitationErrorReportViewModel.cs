using System;

namespace RMuseum.Models.GanjoorAudio.ViewModels
{
    /// <summary>
    /// Post Error Report for recitations
    /// </summary>
    public class RecitationErrorReportViewModel
    {
        /// <summary>
        /// Id
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Recitation Id
        /// </summary>
        public int RecitationId { get; set; }

        /// <summary>
        /// Reason Text
        /// </summary>
        public string ReasonText { get; set; }

        /// <summary>
        /// recitation
        /// </summary>
        public RecitationViewModel Recitation { get; set; }

        /// <summary>
        /// number of verses affected
        /// </summary>
        public int NumberOfLinesAffected { get; set; }

        /// <summary>
        /// couplet index
        /// </summary>
        public int CoupletIndex { get; set; }

        /// <summary>
        /// DateTime
        /// </summary>
        public DateTime DateTime { get; set; }
    }
}
