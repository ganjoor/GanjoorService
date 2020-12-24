namespace RMuseum.Models.Ganjoor.ViewModels
{
    /// <summary>
    /// Ganjoor Verse View Model
    /// </summary>
    public class GanjoorVerseViewModel
    {
        /// <summary>
        /// global id, auto generated (missing in Ganjoor Desktop database)
        /// </summary>
        public int Id { get; set; }


        /// <summary>
        /// vorder
        /// </summary>
        public int VOrder { get; set; }

        /// <summary>
        /// position
        /// </summary>
        public VersePosition VersePosition { get; set; }

        /// <summary>
        /// text
        /// </summary>
        public string Text { get; set; }
    }
}
