using RSecurityBackend.Models.Auth.Db;
using System;

namespace RMuseum.Models.GanjoorAudio
{
    /// <summary>
    /// Error Report for recitations
    /// </summary>
    public class RecitationErrorReport
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
        /// Recitation
        /// </summary>
        public Recitation Recitation { get; set; }

        /// <summary>
        /// Reason Text
        /// </summary>
        public string ReasonText { get; set; }

        /// <summary>
        /// Reporter User Id
        /// </summary>
        public Guid? ReporterId { get; set; }

        /// <summary>
        /// Reporter User
        /// </summary>
        public virtual RAppUser Reporter { get; set; }

        /// <summary>
        /// DateTime
        /// </summary>
        public DateTime DateTime { get; set; }

        /// <summary>
        /// number of verses affected
        /// </summary>
        public int NumberOfLinesAffected { get; set; }

        /// <summary>
        /// couplet index
        /// </summary>
        public int CoupletIndex { get; set; }
    }
}
