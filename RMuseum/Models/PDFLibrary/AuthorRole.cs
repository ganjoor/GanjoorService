namespace RMuseum.Models.PDFLibrary
{
    /// <summary>
    /// Author Role
    /// </summary>
    public class AuthorRole
    {
        /// <summary>
        /// Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Author
        /// </summary>
        public Author Author { get; set; }

        /// <summary>
        /// Role
        /// </summary>
        public string Role { get; set; }
    }
}
