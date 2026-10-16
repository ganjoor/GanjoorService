using System;

namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// a concept row in the management list: the concept with its usage and parent name
    /// </summary>
    public class LiteraryConceptListItem
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
        /// other names, separated by « | »
        /// </summary>
        public string Aliases { get; set; }

        /// <summary>
        /// parent concept id
        /// </summary>
        public int? ParentId { get; set; }

        /// <summary>
        /// parent concept name
        /// </summary>
        public string ParentName { get; set; }

        /// <summary>
        /// number of tag parts which use this concept
        /// </summary>
        public int UsageCount { get; set; }

        /// <summary>
        /// number of child concepts
        /// </summary>
        public int ChildCount { get; set; }
    }

    /// <summary>
    /// a page of the concept list
    /// </summary>
    public class LiteraryConceptListPage
    {
        /// <summary>
        /// total number of matching concepts
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// concepts of the requested page
        /// </summary>
        public LiteraryConceptListItem[] Items { get; set; }
    }

    /// <summary>
    /// concepts whose names are the same after normalization (letter forms, ZWNJ, spaces, simple plural endings)
    /// </summary>
    public class LiteraryConceptDuplicateGroup
    {
        /// <summary>
        /// the normalized key shared by all members
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// members, the most used first
        /// </summary>
        public LiteraryConceptListItem[] Items { get; set; }
    }
}
