namespace RMuseum.Models.FAQ
{
    /// <summary>
    /// FAQ Item
    /// </summary>
    public class FAQItem
    {
        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// question
        /// </summary>
        public string Question { get; set; }

        /// <summary>
        /// excerpt
        /// </summary>
        public string AnswerExcerpt { get; set; }

        /// <summary>
        /// answer
        /// </summary>
        public string FullAnswer { get; set; }

        /// <summary>
        /// pinned
        /// </summary>
        public bool Pinned { get; set; }

        /// <summary>
        /// item order
        /// </summary>
        public int PinnedItemOrder { get; set; }

        /// <summary>
        /// category id
        /// </summary>
        public int CategoryId { get; set; }

        /// <summary>
        /// category
        /// </summary>
        public virtual FAQCategory Category { get; set; }

        /// <summary>
        /// item order in category
        /// </summary>
        public int ItemOrderInCategory { get; set; }

        /// <summary>
        /// content search
        /// </summary>
        public string ContentForSearch { get; set; }

        /// <summary>
        /// hashtag 1
        /// </summary>
        public string HashTag1 { get; set; }

        /// <summary>
        /// hashtag 2
        /// </summary>
        public string HashTag2 { get; set; }

        /// <summary>
        /// hashtag 3
        /// </summary>
        public string HashTag3 { get; set; }

        /// <summary>
        /// hashtag 4
        /// </summary>
        public string HashTag4 { get; set; }

        /// <summary>
        /// published
        /// </summary>
        public bool Published { get; set; }
    }
}
