namespace RMuseum.Models.Ganjoor
{
    public class GanjoorCachedRelatedPoem
    {
        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// poem id
        /// </summary>
        public int PoemId { get; set; }

        /// <summary>
        /// poet id
        /// </summary>
        public int PoetId { get; set; }

        /// <summary>
        /// poem
        /// </summary>
        public GanjoorPoem Poem { get; set; }

        /// <summary>
        /// order
        /// </summary>
        public int RelationOrder { get; set; }

        /// <summary>
        /// poet name
        /// </summary>
        public string PoetName { get; set; }

        /// <summary>
        /// poet image url
        /// </summary>
        public string PoetImageUrl { get; set; }

        /// <summary>
        /// poem full url
        /// </summary>
        public string FullUrl { get; set; }

        /// <summary>
        /// poem full title
        /// </summary>
        public string FullTitle { get; set; }

        /// <summary>
        /// excerpt
        /// </summary>
        public string HtmlExcerpt { get; set; }

        /// <summary>
        /// other poems
        /// </summary>
        public int PoetMorePoemsLikeThisCount { get; set; }
    }
}
