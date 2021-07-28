namespace RMuseum.Models.Ganjoor.ViewModels
{
    /// <summary>
    /// poem correction view model
    /// </summary>
    public class GanjoorPoemCorrectionViewModel
    {
        /// <summary>
        /// poem Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// modified verses
        /// </summary>
        public GanjoorVerseVOrderTextViewModel[] VerseOrderText { get; set; }

        /// <summary>
        /// rhythm
        /// </summary>
        public string Rhythm { get; set; }

        /// <summary>
        /// note
        /// </summary>
        public string Note { get; set; }
    }

    /// <summary>
    /// Verse Vorder / Text
    /// </summary>
    public class GanjoorVerseVOrderTextViewModel
    {
        /// <summary>
        /// verse order
        /// </summary>
        public int VORder { get; set; }

        /// <summary>
        /// text
        /// </summary>
        public string Text { get; set; }
    }
}
