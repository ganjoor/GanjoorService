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
    }
}
