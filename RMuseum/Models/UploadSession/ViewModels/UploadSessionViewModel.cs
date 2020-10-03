using Org.BouncyCastle.Crypto.Tls;
using RSecurityBackend.Models.Auth.ViewModels;
using System;
using System.Collections.Generic;

namespace RMuseum.Models.UploadSession.ViewModels
{
    /// <summary>
    /// Upload session view model
    /// </summary>
    public class UploadSessionViewModel
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="src"></param>
        public UploadSessionViewModel(UploadSession src)
        {
            Id = src.Id;
            SessionType = src.SessionType;
            User = new PublicRAppUser(src.User);
            UploadStartTime = src.UploadStartTime;
            UploadEndTime = src.UploadEndTime;
            ProcessStartTime = src.ProcessStartTime;
            ProcessEndTime = src.ProcessEndTime;
            ProcessProgress = src.ProcessProgress;
            Status = src.Status;
            UploadedFiles = src.UploadedFiles;
        }
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
        public PublicRAppUser User { get; set; }


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
