namespace RMuseum.Models.PDFLibrary
{
    /// <summary>
    /// PDF Source (Web Sites)
    /// </summary>
    public class PDFSource
    {
        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Url
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; }
    }
}
