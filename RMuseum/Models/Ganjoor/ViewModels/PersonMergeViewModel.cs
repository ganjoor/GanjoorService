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
    /// split a person who is really two people with the same name: everything of PersonId located at or after
    /// (FromPoemId, FromCoupletIndex) - and at or before (ToPoemId, ToCoupletIndex) when ToPoemId is given - in the
    /// book of FromPoemId moves to a new person
    /// </summary>
    public class PersonSplitViewModel
    {
        /// <summary>
        /// the person to split
        /// </summary>
        public int PersonId { get; set; }

        /// <summary>
        /// name of the new person; empty = same name as the original
        /// </summary>
        public string NewName { get; set; }

        /// <summary>
        /// description of the new person (the description of the original is not copied, it describes the other one)
        /// </summary>
        public string NewDescription { get; set; }

        /// <summary>
        /// first poem (inclusive) of the part that belongs to the new person
        /// </summary>
        public int FromPoemId { get; set; }

        /// <summary>
        /// first couplet (inclusive) of FromPoemId that belongs to the new person
        /// </summary>
        public int FromCoupletIndex { get; set; }

        /// <summary>
        /// last poem (inclusive) of the part that belongs to the new person; null = up to the end of the book
        /// </summary>
        public int? ToPoemId { get; set; }

        /// <summary>
        /// last couplet (inclusive) of ToPoemId; null = the whole poem
        /// </summary>
        public int? ToCoupletIndex { get; set; }

        /// <summary>
        /// only report what would be moved, change nothing
        /// </summary>
        public bool DryRun { get; set; }
    }

    /// <summary>
    /// result of splitting a person
    /// </summary>
    public class PersonSplitResultViewModel
    {
        /// <summary>
        /// id of the new person (0 for a dry run)
        /// </summary>
        public int NewPersonId { get; set; }

        /// <summary>
        /// was this a dry run
        /// </summary>
        public bool DryRun { get; set; }

        /// <summary>
        /// what was (or would be) moved
        /// </summary>
        public string Report { get; set; }
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
