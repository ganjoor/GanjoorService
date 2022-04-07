namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// poem section type
    /// </summary>
    public enum PoemSectionType
    {
        /// <summary>
        /// sections like paragraphs or poems within paragraphs of text
        /// </summary>
        WholePoem = 0,
        /// <summary>
        /// bands of a multi-band poem
        /// </summary>
        Band = 1,
        /// <summary>
        /// band couplets (virtual) some are virtual because they contain verse from different parts of poems (like verses from all band couplets)
        /// </summary>
        BandCouplets = 2,
        /// <summary>
        /// couplets (virtual) for Masnavi
        /// </summary>
        Couplet = 3,
    }
}
