namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// Language or system of writing for translating poems
    /// </summary>
    public class GanjoorLanguage
    {
        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// is right to left
        /// </summary>
        public bool RightToLeft { get; set; }
    }
}
