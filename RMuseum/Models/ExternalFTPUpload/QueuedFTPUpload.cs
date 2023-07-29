using System;

namespace RMuseum.Models.ExternalFTPUpload
{
    /// <summary>
    /// Queued FTP Upload
    /// </summary>
    public class QueuedFTPUpload
    {
        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// local file path
        /// </summary>
        public string LocalFilePath { get; set; }

        /// <summary>
        /// remote file path
        /// </summary>
        public string RemoteFilePath { get; set; }

        /// <summary>
        /// delete file after upload
        /// </summary>
        public bool DeleteFileAfterUpload { get; set; }

        /// <summary>
        /// queue date
        /// </summary>
        public DateTime QueueDate { get; set; }

        /// <summary>
        /// processing
        /// </summary>
        public bool Processing { get; set; }

        /// <summary>
        /// process date
        /// </summary>
        public DateTime? ProcessDate { get; set; }

        /// <summary>
        /// error
        /// </summary>
        public string Error { get; set; }
    }
}
