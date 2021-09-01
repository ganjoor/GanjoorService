namespace RMuseum.Models.Ganjoor.ViewModels
{
    /// <summary>
    /// history of suggested songs by a user
    /// </summary>
    public class UserSongSuggestionsHistory
    {
        /// <summary>
        /// approved
        /// </summary>
        public int Approved { get; set; }

        /// <summary>
        /// rejected
        /// </summary>
        public int Rejected { get; set; }
    }
}
