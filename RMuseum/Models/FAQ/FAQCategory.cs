namespace RMuseum.Models.FAQ
{
    /// <summary>
    /// FAQ Category
    /// </summary>
    public class FAQCategory
    {
        /// <summary>
        /// Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// order
        /// </summary>
        public int CatOrder { get; set; }

        /// <summary>
        /// description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// published
        /// </summary>
        public bool Published { get; set; }
    }
}
