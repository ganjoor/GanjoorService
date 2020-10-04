using System;


namespace RMuseum.Models.UploadSession
{
    /// <summary>
    /// Upload Session File
    /// </summary>
    public class UploadSessionFile
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Upload Session Id
        /// </summary>
        public Guid UploadSessionId { get; set; }

        /// <summary>
        /// ContentDisposition
        /// </summary>
        public string ContentDisposition { get; set; }

        /// <summary>
        /// ContentType
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// FileName
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Length
        /// </summary>
        public long Length { get; set; }

        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// File Path
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// File check sum for mp3 files
        /// </summary>
        public string MP3FileCheckSum { get; set; }

        /// <summary>
        /// Process Result
        /// </summary>
        public bool ProcessResult { get; set; }

        /// <summary>
        /// Process Result Message
        /// </summary>
        public string ProcessResultMsg { get; set; }

    }
}
