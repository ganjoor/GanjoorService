namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// Temporary Model contianing duplicated poems information
    /// </summary>
    public class GanjoorDuplicate
    {
        /// <summary>
        /// Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// source category id (this category is about to be deleted and redirected)
        /// </summary>
        public int SrcCatId { get; set; }

        /// <summary>
        /// source poem id
        /// </summary>
        public int SrcPoemId { get; set; }

        /// <summary>
        /// source poem
        /// </summary>
        public GanjoorPoem SrcPoem { get; set; }

        /// <summary>
        /// destination poem id
        /// </summary>
        public int? DestPoemId { get; set; }

        /// <summary>
        /// destination poem
        /// </summary>
        public virtual GanjoorPoem DestPoem { get; set; }
    }
}
