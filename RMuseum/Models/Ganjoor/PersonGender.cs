namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// a GanjoorRelatedPerson's gender - used only by the family-tree chart (familytree.js's
    /// buildLayout()) to break ties when a child has two recorded parents and both happen to have
    /// their own recorded ancestors (so neither side can be preferred on "continues a documented
    /// lineage" grounds alone): the father is preferred as the tree's spine parent, matching
    /// conventional patrilineal genealogy-chart practice, with the mother then drawn attached
    /// beside him. Never used to filter, rank or otherwise affect query results. New values can be
    /// appended safely later (stored as int).
    /// </summary>
    public enum PersonGender
    {
        /// <summary>
        /// default - not recorded. Falls back to the old ambiguous-tie-break behavior (ascending
        /// person id) when it matters.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// male
        /// </summary>
        Male = 1,

        /// <summary>
        /// female
        /// </summary>
        Female = 2,
    }
}
