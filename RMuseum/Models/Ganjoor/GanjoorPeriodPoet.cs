namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// half century
    /// </summary>
    public class GanjoorPeriodPoet
    {
        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// order
        /// </summary>
        public int PoetOrder { get; set; }

        /// <summary>
        /// poet id
        /// </summary>
        public int PoetId { get; set; }

        /// <summary>
        /// poet
        /// </summary>
        public GanjoorPoet Poet { get; set; }
    }
}
