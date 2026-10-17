using System;
using System.Collections.Generic;

namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// where a tag comes from
    /// </summary>
    public enum LiteraryTagOrigin
    {
        /// <summary>
        /// a user suggestion approved by a moderator
        /// </summary>
        Editor = 0,

        /// <summary>
        /// imported from an academic corpus (OriginRef names it)
        /// </summary>
        AcademicCorpus = 1,

        /// <summary>
        /// produced by an algorithm
        /// </summary>
        Algorithm = 2,

        /// <summary>
        /// produced by AI
        /// </summary>
        AI = 3
    }

    /// <summary>
    /// a literary device tag on a poem
    /// </summary>
    public class PoemLiteraryTag
    {
        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// poem id
        /// </summary>
        public int PoemId { get; set; }

        /// <summary>
        /// poem
        /// </summary>
        public GanjoorPoem Poem { get; set; }

        /// <summary>
        /// main couplet index (0 based like GanjoorVerse.CoupletIndex, -1 = whole poem)
        /// </summary>
        public int CoupletIndex { get; set; }

        /// <summary>
        /// device id
        /// </summary>
        public int DeviceId { get; set; }

        /// <summary>
        /// device
        /// </summary>
        public LiteraryDevice Device { get; set; }

        /// <summary>
        /// the tagger was not sure (counts can be reported with or without these)
        /// </summary>
        public bool Uncertain { get; set; }

        /// <summary>
        /// note
        /// </summary>
        public string Note { get; set; }

        /// <summary>
        /// origin
        /// </summary>
        public LiteraryTagOrigin Origin { get; set; }

        /// <summary>
        /// for imported tags: which corpus/run (e.g. "persian-literary-corpus-1.0.0 (MIT)")
        /// </summary>
        public string OriginRef { get; set; }

        /// <summary>
        /// the user who suggested it
        /// </summary>
        public Guid? CreatedByUserId { get; set; }

        /// <summary>
        /// creation date
        /// </summary>
        public DateTime CreateDate { get; set; }

        /// <summary>
        /// slot values
        /// </summary>
        public ICollection<PoemLiteraryTagPart> Parts { get; set; }
    }

    /// <summary>
    /// the value of one slot of a tag. Positions are exact: a couplet, a hemistich and a range of words,
    /// because a word can occur several times in one couplet.
    /// </summary>
    public class PoemLiteraryTagPart
    {
        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// tag id
        /// </summary>
        public int TagId { get; set; }

        /// <summary>
        /// slot id
        /// </summary>
        public int SlotId { get; set; }

        /// <summary>
        /// couplet index (0 based like GanjoorVerse.CoupletIndex, -1 = not located in a particular couplet)
        /// </summary>
        public int CoupletIndex { get; set; }

        /// <summary>
        /// hemistich in the couplet: 0 = not specified, 1 = first, 2 = second
        /// </summary>
        public int Misra { get; set; }

        /// <summary>
        /// first word (1 based, counted by spaces in the hemistich), null when the part is not located in the text
        /// </summary>
        public int? WordStart { get; set; }

        /// <summary>
        /// last word (1 based, inclusive)
        /// </summary>
        public int? WordEnd { get; set; }

        /// <summary>
        /// concept
        /// </summary>
        public int? ConceptId { get; set; }

        /// <summary>
        /// concept (navigation)
        /// </summary>
        public LiteraryConcept Concept { get; set; }

        /// <summary>
        /// free text
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// order among the parts of the tag
        /// </summary>
        public int Order { get; set; }
    }

    /// <summary>
    /// whether all literary devices of a poem have been tagged: only poems marked complete count in the
    /// statistics (a missing tag in an incomplete poem does not mean the device is absent)
    /// </summary>
    public class PoemLiteraryAnnotationStatus
    {
        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// poem id (unique)
        /// </summary>
        public int PoemId { get; set; }

        /// <summary>
        /// the annotation is complete
        /// </summary>
        public bool Completed { get; set; }

        /// <summary>
        /// the user whose approved correction set this
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// date
        /// </summary>
        public DateTime Date { get; set; }
    }
}
