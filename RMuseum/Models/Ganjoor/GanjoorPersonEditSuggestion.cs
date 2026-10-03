using RSecurityBackend.Models.Auth.Db;
using System;

namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// a contributor's suggested edit to an existing, already-approved GanjoorRelatedPerson's own
    /// fields (name, bio, birth/death info, family tree caption, ...). Reviewed/approved the same
    /// way a GanjoorPoemGeoDateTagCorrection is - see GanjoorService-ModeratePersonEditSuggestion.cs -
    /// but standalone: it targets a person directly, not a poem/couplet. There is no admin-only
    /// direct-edit path for GanjoorRelatedPerson; this suggestion queue is the only way any of its
    /// fields change after it is first created.
    /// </summary>
    public class GanjoorPersonEditSuggestion
    {
        /// <summary>
        /// suggestion id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// the person this suggestion proposes changes for
        /// </summary>
        public int PersonId { get; set; }

        /// <summary>
        /// the person this suggestion proposes changes for (their CURRENT, still-unchanged fields -
        /// for showing a before/after diff to the moderator)
        /// </summary>
        public virtual GanjoorRelatedPerson Person { get; set; }

        /// <summary>
        /// date
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// suggester's user id
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// suggester
        /// </summary>
        public virtual RAppUser User { get; set; }

        // suggested full replacement state of every editable field - the contributor's form is
        // pre-filled with the person's CURRENT values (see Person above), so these represent what
        // the record ends up with after their edits, not a partial diff of only changed fields

        /// <summary>
        /// suggested name
        /// </summary>
        public string SuggestedName { get; set; }

        /// <summary>
        /// suggested description
        /// </summary>
        public string SuggestedDescription { get; set; }

        /// <summary>
        /// suggested wikipedia url
        /// </summary>
        public string SuggestedWikiUrl { get; set; }

        /// <summary>
        /// suggested birth year in lunar hijri
        /// </summary>
        public int SuggestedBirthYearInLHijri { get; set; }

        /// <summary>
        /// suggested death year in lunar hijri
        /// </summary>
        public int SuggestedDeathYearInLHijri { get; set; }

        /// <summary>
        /// suggested ValidBirthDate
        /// </summary>
        public bool SuggestedValidBirthDate { get; set; }

        /// <summary>
        /// suggested ValidDeathDate
        /// </summary>
        public bool SuggestedValidDeathDate { get; set; }

        /// <summary>
        /// suggested birth location id
        /// </summary>
        public int? SuggestedBirthLocationId { get; set; }

        /// <summary>
        /// suggested birth location
        /// </summary>
        public virtual GanjoorGeoLocation SuggestedBirthLocation { get; set; }

        /// <summary>
        /// suggested death location id
        /// </summary>
        public int? SuggestedDeathLocationId { get; set; }

        /// <summary>
        /// suggested death location
        /// </summary>
        public virtual GanjoorGeoLocation SuggestedDeathLocation { get; set; }

        /// <summary>
        /// suggested family tree caption
        /// </summary>
        public string SuggestedFamilyTreeCaption { get; set; }

        /// <summary>
        /// suggested importance (see GanjoorRelatedPerson.Importance)
        /// </summary>
        public PersonImportance SuggestedImportance { get; set; }

        /// <summary>
        /// true if this suggestion is actually a request to delete the person outright, not to
        /// change their fields - when true, every Suggested* field above is ignored on approval
        /// (they still carry whatever the contributor's pre-filled form happened to hold) and
        /// ModeratePersonEditSuggestionAsync removes the person and every relation/affiliation/tag
        /// reference to them instead. Kept on this same entity rather than a separate one so the
        /// existing suggest/review UI and permissions are reused as-is.
        /// </summary>
        public bool SuggestedForDeletion { get; set; }

        /// <summary>
        /// suggester's note to the moderator
        /// </summary>
        public string SuggestionNote { get; set; }

        /// <summary>
        /// reviewed
        /// </summary>
        public bool Reviewed { get; set; }

        /// <summary>
        /// review result
        /// </summary>
        public CorrectionReviewResult Result { get; set; }

        /// <summary>
        /// review date
        /// </summary>
        public DateTime ReviewDate { get; set; }

        /// <summary>
        /// review note
        /// </summary>
        public string ReviewNote { get; set; }

        /// <summary>
        /// reviewer's user id
        /// </summary>
        public Guid? ReviewerUserId { get; set; }

        /// <summary>
        /// reviewer
        /// </summary>
        public virtual RAppUser ReviewerUser { get; set; }
    }
}
