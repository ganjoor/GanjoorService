namespace RMuseum.Models.Ganjoor.ViewModels
{
    /// <summary>
    /// merge two people: everything attached to SourceId moves to TargetId and SourceId is deleted
    /// </summary>
    public class PersonMergeViewModel
    {
        /// <summary>
        /// the duplicate person, deleted at the end of the merge
        /// </summary>
        public int SourceId { get; set; }

        /// <summary>
        /// the person who stays
        /// </summary>
        public int TargetId { get; set; }
    }

    /// <summary>
    /// replace the aliases of a person
    /// </summary>
    public class PersonAliasesViewModel
    {
        /// <summary>
        /// names separated by ، (Persian comma), a comma or a new line
        /// </summary>
        public string Aliases { get; set; }
    }
}
