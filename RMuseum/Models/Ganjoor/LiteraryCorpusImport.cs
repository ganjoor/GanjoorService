using System.Collections.Generic;

namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// file format for importing literary tags from an academic corpus (see GanjoorService.ImportLiteraryCorpusAsync)
    /// </summary>
    public class LiteraryCorpusImportFile
    {
        /// <summary>
        /// where the data comes from (attribution)
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// stored on every imported tag as OriginRef, also used to make the import idempotent
        /// </summary>
        public string Ref { get; set; }

        /// <summary>
        /// couplets with tags
        /// </summary>
        public List<LiteraryCorpusImportItem> Items { get; set; }
    }

    /// <summary>
    /// a couplet of the corpus with its tags
    /// </summary>
    public class LiteraryCorpusImportItem
    {
        /// <summary>
        /// poem number in the corpus (used only to tell apart identical couplets)
        /// </summary>
        public int GhazalId { get; set; }

        /// <summary>
        /// couplet number in the corpus poem
        /// </summary>
        public int BaytId { get; set; }

        /// <summary>
        /// first hemistich
        /// </summary>
        public string M1 { get; set; }

        /// <summary>
        /// second hemistich
        /// </summary>
        public string M2 { get; set; }

        /// <summary>
        /// tags
        /// </summary>
        public List<LiteraryCorpusImportTag> Tags { get; set; }
    }

    /// <summary>
    /// a tag of an imported couplet
    /// </summary>
    public class LiteraryCorpusImportTag
    {
        /// <summary>
        /// device key (LiteraryDevice.Key)
        /// </summary>
        public string Device { get; set; }

        /// <summary>
        /// slot values
        /// </summary>
        public List<LiteraryCorpusImportPart> Parts { get; set; }
    }

    /// <summary>
    /// a slot value of an imported tag; Misra/WordStart/WordEnd are relative to the corpus text and are
    /// only used if the poem's text has exactly the same words
    /// </summary>
    public class LiteraryCorpusImportPart
    {
        /// <summary>
        /// slot key (LiteraryDeviceSlot.Key)
        /// </summary>
        public string Slot { get; set; }

        /// <summary>
        /// hemistich (1 or 2)
        /// </summary>
        public int? Misra { get; set; }

        /// <summary>
        /// first word (1 based)
        /// </summary>
        public int? WordStart { get; set; }

        /// <summary>
        /// last word (1 based, inclusive)
        /// </summary>
        public int? WordEnd { get; set; }

        /// <summary>
        /// concept name
        /// </summary>
        public string Concept { get; set; }

        /// <summary>
        /// free text
        /// </summary>
        public string Text { get; set; }
    }

    /// <summary>
    /// outcome of a corpus import
    /// </summary>
    public class LiteraryCorpusImportResult
    {
        /// <summary>
        /// nothing was written
        /// </summary>
        public bool DryRun { get; set; }

        /// <summary>
        /// couplets in the file
        /// </summary>
        public int Items { get; set; }

        /// <summary>
        /// couplets found in the database
        /// </summary>
        public int MatchedItems { get; set; }

        /// <summary>
        /// couplets found by poem number and couplet number (the usual way: the corpus is a few years older than the
        /// text but the order and number of couplets are the same)
        /// </summary>
        public int MatchedByPosition { get; set; }

        /// <summary>
        /// couplets found by exact text because the positional candidate was too different
        /// </summary>
        public int MatchedByText { get; set; }

        /// <summary>
        /// couplets whose positional candidate had a text too different from the corpus text (not imported)
        /// </summary>
        public int RejectedLowSimilarity { get; set; }

        /// <summary>
        /// couplets not found
        /// </summary>
        public int UnmatchedItems { get; set; }

        /// <summary>
        /// couplets found more than once and not decidable
        /// </summary>
        public int AmbiguousItems { get; set; }

        /// <summary>
        /// poems skipped because they already have tags from this import
        /// </summary>
        public int SkippedExistingPoems { get; set; }

        /// <summary>
        /// tags created (or to be created in a dry run)
        /// </summary>
        public int TagsCreated { get; set; }

        /// <summary>
        /// span slot values placed on exact words
        /// </summary>
        public int PartsLocated { get; set; }

        /// <summary>
        /// span slot values kept at couplet level only (phrase not found in the corpus text, or the poem's words differ)
        /// </summary>
        public int PartsUnlocated { get; set; }

        /// <summary>
        /// concepts created (or to be created)
        /// </summary>
        public int ConceptsCreated { get; set; }

        /// <summary>
        /// first problems: unmatched couplets etc.
        /// </summary>
        public List<string> Messages { get; set; } = new List<string>();
    }
}
