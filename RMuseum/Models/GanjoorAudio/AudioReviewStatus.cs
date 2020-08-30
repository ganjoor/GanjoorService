namespace RMuseum.Models.GanjoorAudio
{
    /// <summary>
    /// Audio Review Status
    /// </summary>
    public enum AudioReviewStatus
    {
        /// <summary>
        /// All / Unfiltered (for queris)
        /// </summary>
        All = -1,
        /// <summary>
        /// Draft
        /// </summary>
        Draft = 0,
        /// <summary>
        /// pending for review
        /// </summary>
        Pending = 1,

        /// <summary>
        /// approved
        /// </summary>
        Approved = 2,

        /// <summary>
        /// Rejected
        /// </summary>
        Rejected = 3
    }
}
