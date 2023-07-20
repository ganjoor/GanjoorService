namespace RMuseum.Models.PDFLibrary
{
    /// <summary>
    /// Multi Volume Book
    /// </summary>
    public class MultiVolumePDFCollection
    {
        /// <summary>
        /// Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// book id
        /// </summary>
        public int BookId { get; set; }

        /// <summary>
        /// book
        /// </summary>
        public Book Book { get; set; }

        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; }
    }
}
