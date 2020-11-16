namespace RMuseum.Models.GanjoorAudio.ViewModels
{
    /// <summary>
    /// Verse Sync Range
    /// </summary>
    public class RecitationVerseSync
    {
        /// <summary>
        /// Verse Order
        /// </summary>
        public int VerseOrder { get; set; }

        /// <summary>
        /// Verse Text
        /// </summary>
        public string VerseText { get; set; }

        /// <summary>
        /// Audio Start in Milliseconds
        /// </summary>
        public int AudioStartMilliseconds { get; set; }
    }
}
