using System;

namespace RMuseum.Models.GanjoorAudio.ViewModels
{
    /// <summary>
    /// RecitationPublishingTracker View Model
    /// </summary>
    public class RecitationPublishingTrackerViewModel
    {
         /// <summary>
        /// user email
        /// </summary>
        public string UserEmail { get; set; }

        /// <summary>
        /// Poem Full Title
        /// </summary>
        public string PoemFullTitle { get; set; }

        /// <summary>
        /// Artist Name
        /// </summary>
        public string ArtistName { get; set; }

        /// <summary>
        /// Operation
        /// </summary>
        public string Operation { get; set; }

        /// <summary>
        /// In Progress
        /// </summary>
        public bool InProgress { get; set; }

        /// <summary>
        /// XML File Copied
        /// </summary>
        public bool XmlFileCopied { get; set; }

        /// <summary>
        /// MP3 File Copied
        /// </summary>
        public bool Mp3FileCopied { get; set; }

        /// <summary>
        /// First MySql DB Updated
        /// </summary>
        public bool FirstDbUpdated { get; set; }

        /// <summary>
        /// Second DB Updated
        /// </summary>
        public bool SecondDbUpdated { get; set; }

        /// <summary>
        /// Succeeded
        /// </summary>
        public bool Succeeded { get; set; }

        /// <summary>
        /// Error
        /// </summary>
        public bool Error { get; set; }

        /// <summary>
        /// Exception
        /// </summary>
        public string LastException { get; set; }

        /// <summary>
        /// Start Date
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Finish Date
        /// </summary>
        public DateTime FinishDate { get; set; }


    }
}
