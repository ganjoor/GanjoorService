using System;

namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// how many tags/poems a device has
    /// </summary>
    public class LiteraryStatsDevice
    {
        /// <summary>
        /// device id
        /// </summary>
        public int DeviceId { get; set; }

        /// <summary>
        /// number of tags
        /// </summary>
        public int TagCount { get; set; }

        /// <summary>
        /// number of distinct poems
        /// </summary>
        public int PoemCount { get; set; }
    }

    /// <summary>
    /// how many times a concept is used
    /// </summary>
    public class LiteraryStatsConcept
    {
        /// <summary>
        /// concept id
        /// </summary>
        public int ConceptId { get; set; }

        /// <summary>
        /// concept name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// number of tag parts which use it
        /// </summary>
        public int UsageCount { get; set; }
    }

    /// <summary>
    /// how many tags a poet's poems have
    /// </summary>
    public class LiteraryStatsPoet
    {
        /// <summary>
        /// poet id
        /// </summary>
        public int PoetId { get; set; }

        /// <summary>
        /// poet name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// number of tags
        /// </summary>
        public int TagCount { get; set; }
    }

    /// <summary>
    /// summary statistics of literary tags (optionally for one poet)
    /// </summary>
    public class LiteraryStatsOverview
    {
        /// <summary>
        /// total tags
        /// </summary>
        public int TotalTags { get; set; }

        /// <summary>
        /// total distinct poems with tags
        /// </summary>
        public int TotalPoems { get; set; }

        /// <summary>
        /// per device counts, the most used first
        /// </summary>
        public LiteraryStatsDevice[] Devices { get; set; }

        /// <summary>
        /// the most used concepts
        /// </summary>
        public LiteraryStatsConcept[] TopConcepts { get; set; }

        /// <summary>
        /// total number of concept uses in the counted tags (the base of the concepts' percentages)
        /// </summary>
        public int TotalConceptUses { get; set; }

        /// <summary>
        /// poets with tags (not filtered by the requested poet, for a filter list)
        /// </summary>
        public LiteraryStatsPoet[] Poets { get; set; }
    }

    /// <summary>
    /// one slot value of a listed tag
    /// </summary>
    public class LiteraryStatsTagPart
    {
        /// <summary>
        /// slot name
        /// </summary>
        public string SlotName { get; set; }

        /// <summary>
        /// the located words (when the part is located in the couplet) or the free text
        /// </summary>
        public string Words { get; set; }

        /// <summary>
        /// concept name (if any)
        /// </summary>
        public string ConceptName { get; set; }

        /// <summary>
        /// concept id (if any)
        /// </summary>
        public int? ConceptId { get; set; }

        /// <summary>
        /// 1 or 2 when located in a hemistich
        /// </summary>
        public int Misra { get; set; }

        /// <summary>
        /// first word of the located range (1 based)
        /// </summary>
        public int? WordStart { get; set; }

        /// <summary>
        /// last word of the located range (1 based)
        /// </summary>
        public int? WordEnd { get; set; }
    }

    /// <summary>
    /// a tag in a listing: where it is, its couplet and its parts
    /// </summary>
    public class LiteraryStatsTagItem
    {
        /// <summary>
        /// tag id
        /// </summary>
        public int TagId { get; set; }

        /// <summary>
        /// device id
        /// </summary>
        public int DeviceId { get; set; }

        /// <summary>
        /// poem id
        /// </summary>
        public int PoemId { get; set; }

        /// <summary>
        /// poem full title
        /// </summary>
        public string PoemFullTitle { get; set; }

        /// <summary>
        /// poem url
        /// </summary>
        public string PoemFullUrl { get; set; }

        /// <summary>
        /// poet id
        /// </summary>
        public int PoetId { get; set; }

        /// <summary>
        /// poet name
        /// </summary>
        public string PoetName { get; set; }

        /// <summary>
        /// couplet index (0 based like GanjoorVerse.CoupletIndex), -1 = whole poem
        /// </summary>
        public int CoupletIndex { get; set; }

        /// <summary>
        /// first hemistich of the couplet
        /// </summary>
        public string Hemistich1 { get; set; }

        /// <summary>
        /// second hemistich of the couplet
        /// </summary>
        public string Hemistich2 { get; set; }

        /// <summary>
        /// not certain
        /// </summary>
        public bool Uncertain { get; set; }

        /// <summary>
        /// note
        /// </summary>
        public string Note { get; set; }

        /// <summary>
        /// parts
        /// </summary>
        public LiteraryStatsTagPart[] Parts { get; set; }
    }

    /// <summary>
    /// a page of listed tags
    /// </summary>
    public class LiteraryStatsTagPage
    {
        /// <summary>
        /// total matching tags
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// items of the page
        /// </summary>
        public LiteraryStatsTagItem[] Items { get; set; }
    }
}
