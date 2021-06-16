namespace RMuseum.Models.Ganjoor.ViewModels
{
    /// <summary>
    /// poem summary
    /// </summary>
    public class GanjoorPoemSummaryViewModel
    {
        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// url => slug
        /// </summary>
        public string UrlSlug { get; set; }

        /// <summary>
        /// excerpt text
        /// </summary>
        public string Excerpt { get; set; }

        /// <summary>
        /// Rythm
        /// </summary>
        /// <example>مفاعیلن مفاعیلن فعولن</example>
        public string Rhythm { get; set; }

        /// <summary>
        /// rhyme letters
        /// </summary>
        public string RhymeLetters { get; set; }


    }
}
