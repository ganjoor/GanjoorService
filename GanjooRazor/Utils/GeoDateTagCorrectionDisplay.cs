using Newtonsoft.Json;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;

namespace GanjooRazor.Utils
{
    /// <summary>
    /// GanjoorPoemGeoDateTagCorrection (Editor.cshtml's combined geo/date/person tag suggestion row)
    /// carries both kinds of tag in one flat shape - see that class's own doc comment for why. The
    /// review-history pages (Edits.cshtml, PoemCorrectionsHistory.cshtml) need to tell the two kinds
    /// apart to render each sensibly (a place+date vs. a person's name), so that little bit of shared
    /// logic lives here once instead of being duplicated - and previously drifting out of sync - in
    /// both .cshtml files.
    /// </summary>
    public static class GeoDateTagCorrectionDisplay
    {
        /// <summary>
        /// true if this suggestion is a person (شخصیت) tag rather than a geo/date tag - mirrors the
        /// client-side _getSuggestionUiKind() helper in Editor.cshtml's script block, but only for the
        /// "adding a new tag" case: a MarkForDelete row carries neither PersonId nor a location, so its
        /// kind can't be recovered from the correction alone (the callers fall back to the generic
        /// "برچسب موجود" wording for those, same as before this helper existed).
        /// </summary>
        public static bool IsPersonTag(GanjoorPoemGeoDateTagCorrection tag)
        {
            if (tag == null) return false;
            return tag.PersonId != null || !string.IsNullOrEmpty(tag.SuggestedPersonGraphJson);
        }

        /// <summary>
        /// best-effort display name for a brand-new person suggested via SuggestedPersonGraphJson -
        /// just the name typed into the "new person" panel, ignoring any relatives/relations also
        /// carried in that JSON (those aren't relevant to a one-line history row). Returns null if the
        /// JSON is missing, malformed, or (defensively) has no person node.
        /// </summary>
        public static string GetSuggestedPersonName(string suggestedPersonGraphJson)
        {
            if (string.IsNullOrEmpty(suggestedPersonGraphJson)) return null;
            try
            {
                var suggestion = JsonConvert.DeserializeObject<PersonGraphSuggestion>(suggestedPersonGraphJson);
                return suggestion?.Person?.Name;
            }
            catch (JsonException)
            {
                // malformed JSON shouldn't blow up a history page - just fall back to no name
                return null;
            }
        }
    }
}
