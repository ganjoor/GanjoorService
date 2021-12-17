using System;

namespace RMuseum.Models.Artifact
{
    /// <summary>
    /// webp convertion log
    /// </summary>
    public class WebpConvertionLog
    {
        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// picture id
        /// </summary>
        public Guid PictureId { get; set; }

        /// <summary>
        /// picture
        /// </summary>
        public RPictureFile Picture { get; set; }

        /// <summary>
        /// original file size
        /// </summary>
        public long OriginalFileSizeInByte { get; set; }

        /// <summary>
        /// start time
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// target file size
        /// </summary>
        public long TargetFileSizeInByte { get; set; }

        /// <summary>
        /// finish time
        /// </summary>
        public DateTime FinishTime { get; set; }
    }
}
