using System;

namespace RMuseum.Models.GanjoorAudio
{
    /// <summary>
    /// Narration Publishing Tracker
    /// </summary>
    public class RecitationPublishingTracker
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Poem Narration Id
        /// </summary>
        public int PoemNarrationId { get; set; }

        /// <summary>
        /// Poem Narration
        /// </summary>
        public Recitation PoemNarration { get; set; }

        /// <summary>
        /// Start Date
        /// </summary>
        public DateTime StartDate { get; set; }

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
        /// Finished
        /// </summary>
        public bool Finished { get; set; }

        /// <summary>
        /// Finish Date
        /// </summary>
        public DateTime FinishDate { get; set; }

        /// <summary>
        /// Last Excecption
        /// </summary>
        public string LastException { get; set; }
    }
}
