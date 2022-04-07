namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// Ganjoor Poem Section
    /// </summary>
    public class GanjoorPoemSection
    {
        /// <summary>
        /// record id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Poem Id
        /// </summary>
        public int PoemId { get; set; }

        /// <summary>
        /// Poem
        /// </summary>
        public GanjoorPoem Poem { get; set; }

        /// <summary>
        /// poet id might be different from GanjoorPoem.PoetId
        /// </summary>
        public int? PoetId { get; set; }

        /// <summary>
        /// poet might be different from GanjoorPoem.PoetId
        /// </summary>
        public virtual GanjoorPoet Poet { get; set; }

        /// <summary>
        /// use this field instead of Id for referencing to ease record deletion for verses, Index starts at 0, each poem should have at least one non-virtual part ordered by Index without break
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// for lots of poems Number equals to Index + 1, but for rare ones such as a paragraph containing multi-band poem this could be different, 0 means no visible numbers
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// section type
        /// </summary>
        public PoemSectionType SectionType { get; set; }

        /// <summary>
        /// how GanjoorPoemSection is linked to GanjoorVerse
        /// </summary>
        public VersePoemSectionType VerseType { get; set; }

        /// <summary>
        /// prosody information
        /// </summary>
        /// <remarks>
        /// in fact this should be a many-to-many relationship, but our current dataset lacks such a relationship instance
        /// </remarks>
        public int? GanjoorMetreId { get; set; }

        /// <summary>
        /// metre
        /// </summary>
        public virtual GanjoorMetre GanjoorMetre { get; set; }

        /// <summary>
        /// rhyme letters
        /// </summary>
        public string RhymeLetters { get; set; }
    }
}
