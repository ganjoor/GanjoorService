namespace RMuseum.Models.GanjoorAudio.ViewModels
{
    /// <summary>
    /// Recitation Ordering View Model
    /// </summary>
    public class RecitationOrderingViewModel
    {
        /// <summary>
        /// recitation
        /// </summary>
        public int RecitationId { get; set; }

        /// <summary>
        /// earlyness advatage
        /// </summary>
        public int EarlynessAdvantage { get; set; }

        /// <summary>
        /// initial score
        /// </summary>
        public int InitialScore { get; set; }

        /// <summary>
        /// upvotes from users other than the owner
        /// </summary>
        public int UpVotes { get; set; }

        /// <summary>
        /// approved mistaked
        /// </summary>
        public int Mistakes { get; set; }

        /// <summary>
        /// total scores
        /// </summary>
        public int TotalScores { get; set; }

        /// <summary>
        /// computed order
        /// </summary>
        public int ComputedOrder { get; set; }

        /// <summary>
        /// recitation (it is null by default and you should fill it using a separate api call based on RecitationId)
        /// </summary>
        public PublicRecitationViewModel Recitation { get; set; }
    }
}
