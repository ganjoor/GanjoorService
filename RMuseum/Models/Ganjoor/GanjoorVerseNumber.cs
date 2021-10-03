namespace RMuseum.Models.Ganjoor
{
    public class GanjoorVerseNumber
    {
        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Numbering schema Id
        /// </summary>
        public int NumberingId { get; set; }

        /// <summary>
        /// Numbering schema
        /// </summary>
        public GanjoorNumbering Numbering { get; set; }

        /// <summary>
        /// verse id
        /// </summary>
        public int VerseId { get; set; }

        /// <summary>
        /// verse
        /// </summary>
        public GanjoorVerse Verse { get; set; }

        /// <summary>
        /// number
        /// </summary>
        public int Number { get; set; }
    }
}
