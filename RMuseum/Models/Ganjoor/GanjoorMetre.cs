namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// Poem prosodic Metre
    /// </summary>
    public class GanjoorMetre
    {
        /// <summary>
        /// Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Url Slug
        /// </summary>
        public string UrlSlug { get; set; }

        /// <summary>
        /// Rythm
        /// </summary>
        /// <example>مفاعیلن مفاعیلن فعولن</example>
        public string Rhythm { get; set; }

        /// <summary>
        /// Name
        /// </summary>
        /// <example>
        /// هزج مسدس محذوف
        /// </example>
        public string Name { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Total Verse Count (its actually couplet count)
        /// </summary>
        public int VerseCount { get; set; }
    }
}
