using RSecurityBackend.Models.Auth.Db;
using System;

namespace RMuseum.Models.Note
{

    /// <summary>
    /// (Public) User Notes Abuse Report
    /// </summary>
    public class RUserNoteAbuseReport
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Note Id
        /// </summary>
        public Guid NoteId { get; set; }

        /// <summary>
        /// Note
        /// </summary>
        public RUserNote Note { get; set; }

        /// <summary>
        /// some explanotory text provided by reporter
        /// </summary>
        public string ReasonText { get; set; }

        /// <summary>
        /// reporting user Id (nullable to allow deleting users and keeping this part of data if needed)
        /// </summary>
        public Guid? ReporterId { get; set; }

        /// <summary>
        /// reporting user
        /// </summary>
        public virtual RAppUser Reporter { get; set; }
    }
}
