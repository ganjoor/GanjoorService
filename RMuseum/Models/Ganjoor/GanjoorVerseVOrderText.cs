namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// Verse Vorder / Text
    /// </summary>
    public class GanjoorVerseVOrderText
    {
        /// <summary>
        /// record id
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// verse order
        /// </summary>
        public int VORder { get; set; }

        /// <summary>
        /// text
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// original text
        /// </summary>
        public string OriginalText { get; set; }

        /// <summary>
        /// review result
        /// </summary>
        public CorrectionReviewResult Result { get; set; }

        /// <summary>
        /// note
        /// </summary>
        public string ReviewNote { get; set; }

        /// <summary>
        /// couplet indexs
        /// </summary>
        public int? CoupletIndex { get; set; }

        /// <summary>
        /// verse position
        /// </summary>
        public VersePosition VersePosition { get; set; }

        /// <summary>
        /// original verse position
        /// </summary>
        public VersePosition OriginalVersePosition { get; set; }


        /// <summary>
        /// verse position result
        /// </summary>
        public CorrectionReviewResult VersePositionResult { get; set; }

        /// <summary>
        /// mark for delete
        /// </summary>
        public bool MarkForDelete { get; set; }

        /// <summary>
        /// mark for delete result
        /// </summary>
        public CorrectionReviewResult MarkForDeleteResult { get; set; }
    }
}
