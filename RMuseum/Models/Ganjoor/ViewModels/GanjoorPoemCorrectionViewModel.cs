using System;

namespace RMuseum.Models.Ganjoor.ViewModels
{
    /// <summary>
    /// poem correction view model
    /// </summary>
    public class GanjoorPoemCorrectionViewModel
    {
        /// <summary>
        /// Correction Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// poem Id
        /// </summary>
        public int PoemId { get; set; }

        /// <summary>
        /// modified verses
        /// </summary>
        public GanjoorVerseVOrderText[] VerseOrderText { get; set; }

        /// <summary>
        /// title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// original title
        /// </summary>
        public string OriginalTitle { get; set; }

        /// <summary>
        /// rhythm
        /// </summary>
        public string Rhythm { get; set; }

        /// <summary>
        /// original rhythm
        /// </summary>
        public string OriginalRhythm { get; set; }

        /// <summary>
        /// note
        /// </summary>
        public string Note { get; set; }

        /// <summary>
        /// date
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// user Id
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// nickname
        /// </summary>
        public string UserNickname { get; set; }

        /// <summary>
        /// reviewed
        /// </summary>
        public bool Reviewed { get; set; }

        /// <summary>
        /// reviewdate
        /// </summary>
        public DateTime ReviewDate { get; set; }

        /// <summary>
        /// review note
        /// </summary>
        public string ReviewNote { get; set; }

        /// <summary>
        /// review result for title and rhythm
        /// </summary>
        public CorrectionReviewResult Result { get; set; }
    }
}
