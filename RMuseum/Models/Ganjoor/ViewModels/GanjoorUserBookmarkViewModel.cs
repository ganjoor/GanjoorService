using System;


namespace RMuseum.Models.Ganjoor.ViewModels
{
    public class GanjoorUserBookmarkViewModel
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// poet name
        /// </summary>
        public string PoetName { get; set; }

        /// <summary>
        /// poet image url
        /// </summary>
        public string PoetImageUrl { get; set; }

        /// <summary>
        /// poem full title
        /// </summary>
        public string PoemFullTitle { get; set; }

        /// <summary>
        /// sample: /hafez/ghazal/sh1
        /// </summary>
        public string PoemFullUrl { get; set; }

        /// <summary>
        /// couplet index
        /// </summary>
        public int CoupletIndex { get; set; }

        /// <summary>
        /// verse1 text
        /// </summary>
        public string VerseText { get; set; }

        /// <summary>
        /// verse2 text
        /// </summary>
        public string Verse2Text { get; set; }

        /// <summary>
        /// DateTime
        /// </summary>
        public DateTime DateTime { get; set; }

        /// <summary>
        /// private note for bookmark
        /// </summary>
        public string PrivateNote { get; set; }
    }
}
