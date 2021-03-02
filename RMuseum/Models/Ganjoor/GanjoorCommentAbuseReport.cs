using RSecurityBackend.Models.Auth.Db;
using System;

namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// Comment Abuse Report
    /// </summary>
    public class GanjoorCommentAbuseReport
    {
        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// comment id
        /// </summary>
        public int GanjoorCommentId { get; set; }

        /// <summary>
        /// comment
        /// </summary>
        public GanjoorComment GanjoorComment { get; set; }

        /// <summary>
        /// reason code for better grouping: offensive, bogus, other
        /// </summary>
        public string ReasonCode { get; set; }

        /// <summary>
        /// some explanotory text provided by reporter
        /// </summary>
        public string ReasonText { get; set; }

        /// <summary>
        /// reported by id
        /// </summary>
        public Guid? ReportedById { get; set; }

        /// <summary>
        /// reported by user
        /// </summary>
        public virtual RAppUser ReportedBy { get; set; }

    }
}
