namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// Ganjoor Verse
    /// </summary>
    public class GanjoorVerse
    {
        /// <summary>
        /// global id, auto generated (missing in Ganjoor Desktop database)
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// poem_id
        /// </summary>
        public int PoemId { get; set; }

        /// <summary>
        /// poem
        /// </summary>
        public GanjoorPoem Poem { get; set; }

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

        /// <summary>
        /// couplet index
        /// </summary>
        public int? CoupletIndex { get; set; }

        /// <summary>
        /// language  (this is intended to affect html page encodings and not determine actuallly accents and ....), so it is unusable for verses I guess
        /// </summary>
        public string Language { get; set; }

        public override string ToString()
        {
            return Text;
        }
    }
}
