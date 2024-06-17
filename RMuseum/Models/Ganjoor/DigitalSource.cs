namespace RMuseum.Models.Ganjoor
{
    public class DigitalSource
    {
        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// source url slug
        /// </summary>
        public string UrlSlug { get; set; }

        /// <summary>
        /// short name
        /// </summary>
        public string ShortName { get; set; }

        /// <summary>
        /// full name
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// source category
        /// </summary>
        public string SourceCategory { get; set; }

        /// <summary>
        /// couplets count
        /// </summary>
        public int CoupletsCount { get; set; }
    }
}
