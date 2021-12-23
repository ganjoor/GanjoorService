namespace RMuseum.Models.GanjoorAudio.ViewModels
{
    /// <summary>
    /// approved mistake view model
    /// </summary>
    public class RecitationMistakeViewModel
    {
        /// <summary>
        /// mistake
        /// </summary>
        public string Mistake { get; set; }

        /// <summary>
        /// number of verses affected
        /// </summary>
        public int NumberOfLinesAffected { get; set; }

        /// <summary>
        /// couplet index
        /// </summary>
        public int CoupletIndex { get; set; }
    }
}
