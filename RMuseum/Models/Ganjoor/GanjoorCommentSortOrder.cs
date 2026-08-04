namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// how to sort a poem's comments
    /// </summary>
    public enum GanjoorCommentSortOrder
    {
        /// <summary>
        /// highest rating first (ties broken by oldest first)
        /// </summary>
        TopRated = 0,

        /// <summary>
        /// oldest comment first
        /// </summary>
        Oldest = 1,

        /// <summary>
        /// newest comment first
        /// </summary>
        Newest = 2,
    }
}
