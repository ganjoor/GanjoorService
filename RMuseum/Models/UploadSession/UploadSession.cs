using RSecurityBackend.Models.Auth.Db;
using System;
using System.Collections.Generic;

namespace RMuseum.Models.UploadSession
{
    /// <summary>
    /// Upload Session
    /// </summary>
    public class UploadSession
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Session Type
        /// </summary>
        public UploadSessionType SessionType { get; set; }

        /// <summary>
        /// User
        /// </summary>
        public RAppUser User { get; set; }

        /// <summary>
        /// User Id
        /// </summary>
        public Guid UseId { get; set; }

        /// <summary>
        /// Upload Start Time
        /// </summary>
        public DateTime UploadStartTime { get; set; }

        /// <summary>
        /// Upload End Time
        /// </summary>
        public DateTime UploadEndTime { get; set; }

        /// <summary>
        /// Process Start Time
        /// </summary>
        public DateTime ProcessStartTime { get; set; }

        /// <summary>
        /// Process End Time
        /// </summary>
        public DateTime ProcessEndTime { get; set; }

        /// <summary>
        /// Process Progress
        /// </summary>
        public int ProcessProgress { get; set; }

        /// <summary>
        /// Status
        /// </summary>
        public UploadSessionProcessStatus Status { get; set; }

        /// <summary>
        /// Uploaded Files
        /// </summary>
        public ICollection<UploadSessionFile> UploadedFiles { get; set; }


    }
}
