namespace GanjooRazor.Models
{
    public class PoemMoerationStructure
    {
        public int correctionId { get; set; }
        public string titleReviewResult { get; set; }
        public string summaryReviewResult { get; set; }
        public string rhythmReviewResult { get; set; }
        public string rhythm2ReviewResult { get; set; }
        public string rhymeReviewResult { get; set; }
        public string titleReviewNote { get; set; }
        public string[] verseReviewResult { get; set; }
        public string[] versePosReviewResult { get; set; }
        public string[] verseSummaryResults { get; set; }
        public string[] verseLanguageReviewResult { get; set; }
        public string[] verseReviewNotes { get; set; }
        public string poemformatReviewResult { get; set; }
        public string poemformatReviewNote { get; set; }
        public string[] geoTagReviewResult { get; set; }
        public string[] geoTagReviewNotes { get; set; }
        /// <summary>
        /// moderator's corrected location for each geo tag, if they picked an existing catalog location
        /// instead of what the user suggested (empty/"0" means no change to LocationId)
        /// </summary>
        public string[] geoTagLocationId { get; set; }
        /// <summary>
        /// moderator's corrected new-location name (fixes a typo in the user's suggestion), if any
        /// </summary>
        public string[] geoTagLocationName { get; set; }
        /// <summary>
        /// moderator's corrected new-location latitude/longitude (fixes wrong coordinates), if any
        /// </summary>
        public string[] geoTagLatitude { get; set; }
        public string[] geoTagLongitude { get; set; }
    }
}
