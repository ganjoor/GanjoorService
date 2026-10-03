namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// how editorially prominent a GanjoorRelatedPerson is treated as - purely a display/sizing
    /// hint (see GanjoorRelatedPerson.Importance, GanjoorPersonGraphNode.Importance and
    /// peoplegraph.js's nodeRadius()), never used to filter, rank or otherwise affect query
    /// results. New values can be appended safely later (stored as int).
    /// </summary>
    public enum PersonImportance
    {
        /// <summary>
        /// default - no special prominence, drawn at the normal node size
        /// </summary>
        Normal = 0,

        /// <summary>
        /// somewhat prominent (e.g. a well-known minister or local ruler) - drawn larger than Normal
        /// </summary>
        Important = 1,

        /// <summary>
        /// most prominent (e.g. a major poet or king) - drawn largest
        /// </summary>
        VeryImportant = 2,
    }
}
