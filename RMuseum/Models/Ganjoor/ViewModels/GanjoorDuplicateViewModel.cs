namespace RMuseum.Models.Ganjoor.ViewModels
{
    /// <summary>
    /// Ganjoor Duplicate View Model
    /// </summary>
    public class GanjoorDuplicateViewModel
    {
        /// <summary>
        /// Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Source Poem Id
        /// </summary>
        public int SrcPoemId { get; set; }

        /// <summary>
        /// Source Poem cat + parent cats title + title
        /// </summary>
        public string SrcPoemFullTitle { get; set; }

        /// <summary>
        /// Source Poem sample: /hafez/ghazal/sh1
        /// </summary>
        public string SrcPoemFullUrl { get; set; }

        /// <summary>
        /// Source Poem First Verse
        /// </summary>
        public string FirstVerse { get; set; }

        /// <summary>
        /// destination poem id
        /// </summary>
        public int? DestPoemId { get; set; }

        /// <summary>
        /// destination Poem cat + parent cats title + title
        /// </summary>
        public string DestPoemFullTitle { get; set; }

        /// <summary>
        /// destination Poem sample: /hafez/ghazal/sh1
        /// </summary>
        public string DestPoemFullUrl { get; set; }

        /// <summary>
        /// destination poem first verse
        /// </summary>
        public string DestPoemFirstVerse { get; set; }
    }
}
