using System.Collections.Generic;

namespace RMuseum.Models.Ganjoor.ViewModels
{
    /// <summary>
    /// the "who is mentioned in the same stretch of the book" view of one person: the sections of a book
    /// (its direct sub-categories - in the Shahnameh the reigns) are the columns, and every tagged person
    /// has a presence span from the first to the last section in which they are mentioned. Uses only the
    /// poem tags and the book's structure - no birth/death years and no assumed lifespans.
    /// </summary>
    public class GanjoorPresenceTimelineViewModel
    {
        /// <summary>
        /// the person the view was opened for
        /// </summary>
        public int RootId { get; set; }

        /// <summary>
        /// name of the person the view was opened for
        /// </summary>
        public string RootName { get; set; }

        /// <summary>
        /// the books (master categories) in which this person is tagged, most tagged first
        /// </summary>
        public List<GanjoorPresenceBook> Books { get; set; } = new List<GanjoorPresenceBook>();

        /// <summary>
        /// the book shown (null when the person is not tagged anywhere)
        /// </summary>
        public int? MasterCatId { get; set; }

        /// <summary>
        /// the sections of the shown book, in book order
        /// </summary>
        public List<GanjoorPresenceColumn> Columns { get; set; } = new List<GanjoorPresenceColumn>();

        /// <summary>
        /// everyone tagged in the shown book
        /// </summary>
        public List<GanjoorPresencePerson> People { get; set; } = new List<GanjoorPresencePerson>();
    }

    /// <summary>
    /// a book in which the person is tagged
    /// </summary>
    public class GanjoorPresenceBook
    {
        /// <summary>
        /// master category id
        /// </summary>
        public int MasterCatId { get; set; }

        /// <summary>
        /// title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// number of the person's tags in it
        /// </summary>
        public int TagCount { get; set; }
    }

    /// <summary>
    /// a section (column)
    /// </summary>
    public class GanjoorPresenceColumn
    {
        /// <summary>
        /// category id
        /// </summary>
        public int CatId { get; set; }

        /// <summary>
        /// title
        /// </summary>
        public string Title { get; set; }
    }

    /// <summary>
    /// a person and the columns (indexes into Columns) in which they are tagged
    /// </summary>
    public class GanjoorPresencePerson
    {
        /// <summary>
        /// person id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// sorted column indexes
        /// </summary>
        public List<int> Columns { get; set; } = new List<int>();

        /// <summary>
        /// directly related to the chart's person by a recorded kinship or affiliation
        /// (listed even when the two never share a section, e.g. father and son in successive reigns)
        /// </summary>
        public bool Related { get; set; }
    }
}
