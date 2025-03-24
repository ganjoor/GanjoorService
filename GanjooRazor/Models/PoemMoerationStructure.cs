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
    }
}
