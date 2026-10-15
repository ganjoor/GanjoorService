using System.Collections.Generic;

namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// one slot value inside GanjoorPoemLiteraryTagCorrection.PartsJson
    /// </summary>
    public class LiteraryTagPartSuggestion
    {
        /// <summary>
        /// slot id
        /// </summary>
        public int SlotId { get; set; }

        /// <summary>
        /// couplet index (1 based, 0 = not located)
        /// </summary>
        public int CoupletIndex { get; set; }

        /// <summary>
        /// hemistich: 0 = not specified, 1, 2
        /// </summary>
        public int Misra { get; set; }

        /// <summary>
        /// first word (1 based)
        /// </summary>
        public int? WordStart { get; set; }

        /// <summary>
        /// last word (1 based, inclusive)
        /// </summary>
        public int? WordEnd { get; set; }

        /// <summary>
        /// an existing, approved concept - set this OR SuggestedConceptName
        /// </summary>
        public int? ConceptId { get; set; }

        /// <summary>
        /// name of a concept that is not in the list yet; when the tag is approved the concept is
        /// created (or an approved concept with the same normalized name/alias is used)
        /// </summary>
        public string SuggestedConceptName { get; set; }

        /// <summary>
        /// free text
        /// </summary>
        public string Text { get; set; }
    }

    /// <summary>
    /// suggested literary tag for a poem, pending moderation as part of a GanjoorPoemCorrection
    /// (same pattern as GanjoorPoemGeoDateTagCorrection)
    /// </summary>
    public class GanjoorPoemLiteraryTagCorrection
    {
        /// <summary>
        /// record id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// main couplet index (0 = whole poem)
        /// </summary>
        public int CoupletIndex { get; set; }

        /// <summary>
        /// device id (must be an approved device)
        /// </summary>
        public int DeviceId { get; set; }

        /// <summary>
        /// device (navigation)
        /// </summary>
        public virtual LiteraryDevice Device { get; set; }

        /// <summary>
        /// the contributor was not sure
        /// </summary>
        public bool Uncertain { get; set; }

        /// <summary>
        /// slot values, serialized List of LiteraryTagPartSuggestion (stored as JSON because the parts of one
        /// suggestion are only meaningful together and may carry concepts that do not exist yet)
        /// </summary>
        public string PartsJson { get; set; }

        /// <summary>
        /// contributor's reasoning
        /// </summary>
        public string SuggestionNote { get; set; }

        /// <summary>
        /// true if this suggestion is for removing an existing PoemLiteraryTag
        /// </summary>
        public bool MarkForDelete { get; set; }

        /// <summary>
        /// the existing tag to remove (MarkForDelete), or - after approval of an addition - the tag it created
        /// </summary>
        public int? ExistingTagId { get; set; }

        /// <summary>
        /// review result
        /// </summary>
        public CorrectionReviewResult Result { get; set; }

        /// <summary>
        /// reviewer's note
        /// </summary>
        public string ReviewNote { get; set; }
    }
}
