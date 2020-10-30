using System;

namespace RMuseum.Models.UploadSession.ViewModels
{
    /// <summary>
    /// Uploaded Item View Model
    /// </summary>
    public class UploadedItemViewModel
    {
        /// <summary>
        /// filename
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// process result
        /// </summary>
        public bool ProcessResult { get; set; }

        /// <summary>
        /// process result message
        /// </summary>
        public string ProcessResultMsg { get; set; }

        /// <summary>
        /// Upload End Time
        /// </summary>
        public DateTime UploadEndTime { get; set; }

        /// <summary>
        /// Upload User Name
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Process Start Time
        /// </summary>
        public DateTime ProcessStartTime { get; set; }

        /// <summary>
        /// progress progress
        /// </summary>
        public int ProcessProgress { get; set; }

        /// <summary>
        /// process end time
        /// </summary>
        public DateTime ProcessEndTime { get; set; }
    }
}
