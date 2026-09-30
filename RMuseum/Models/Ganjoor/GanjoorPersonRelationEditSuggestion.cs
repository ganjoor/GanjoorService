using RSecurityBackend.Models.Auth.Db;
using System;

namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// a contributor's suggestion to add a brand new kinship edge between two already-approved
    /// people, change an existing edge's type/degree/note, or remove an existing edge outright -
    /// the moderated counterpart of GanjoorPersonEditSuggestion, but for the edges between people
    /// rather than one person's own fields. Reviewed the same way (see
    /// GanjoorRelatedPersonService.ModeratePersonRelationEditSuggestionAsync).
    /// </summary>
    public class GanjoorPersonRelationEditSuggestion
    {
        /// <summary>
        /// suggestion id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// what this suggestion proposes doing
        /// </summary>
        public PersonRelationSuggestionAction Action { get; set; }

        /// <summary>
        /// whether this suggestion is about a kinship edge (Family, using SuggestedRelationType /
        /// ExistingRelationId / ExistingRelation) or a non-family affiliation edge (Affiliation, using
        /// SuggestedAffiliationType / ExistingAffiliationId / ExistingAffiliation instead). Defaults
        /// to Family so rows created before this field existed keep working unchanged.
        /// </summary>
        public PersonRelationSuggestionKind Kind { get; set; }

        /// <summary>
        /// for Modify/Remove when Kind is Family: the existing GanjoorPersonRelation row this
        /// suggestion targets. Null for Add, where there's nothing existing yet, and for any
        /// Kind == Affiliation suggestion (see ExistingAffiliationId instead).
        /// </summary>
        public int? ExistingRelationId { get; set; }

        /// <summary>
        /// the existing relation being modified/removed (for showing a before/after diff to the
        /// moderator) - null for Add, and for Kind == Affiliation
        /// </summary>
        public virtual GanjoorPersonRelation ExistingRelation { get; set; }

        /// <summary>
        /// for Modify/Remove when Kind is Affiliation: the existing GanjoorPersonAffiliation row this
        /// suggestion targets. Null for Add, and for any Kind == Family suggestion (see
        /// ExistingRelationId instead).
        /// </summary>
        public int? ExistingAffiliationId { get; set; }

        /// <summary>
        /// the existing affiliation being modified/removed (for showing a before/after diff to the
        /// moderator) - null unless Kind is Affiliation and Action is Modify/Remove
        /// </summary>
        public virtual GanjoorPersonAffiliation ExistingAffiliation { get; set; }

        /// <summary>
        /// the pair this suggestion is about. For Add, this and Person2Id define the new relation.
        /// For Modify/Remove, these are copied from ExistingRelation at submission time, so the
        /// suggestion always accurately reflects what it targets even if the underlying relation
        /// changes again before this is reviewed.
        /// </summary>
        public int Person1Id { get; set; }

        /// <summary>
        /// first person (navigation)
        /// </summary>
        public virtual GanjoorRelatedPerson Person1 { get; set; }

        /// <summary>
        /// second person id, see Person1Id
        /// </summary>
        public int Person2Id { get; set; }

        /// <summary>
        /// second person (navigation)
        /// </summary>
        public virtual GanjoorRelatedPerson Person2 { get; set; }

        /// <summary>
        /// for Add/Modify when Kind is Family: the relation type to create/change to. For Remove: a
        /// snapshot of the existing relation's type at submission time, purely for display (removing
        /// doesn't use it). Ignored when Kind is Affiliation - see SuggestedAffiliationType instead.
        /// </summary>
        public PersonRelationType SuggestedRelationType { get; set; }

        /// <summary>
        /// for Add/Modify when Kind is Affiliation: the affiliation type to create/change to. For
        /// Remove: a snapshot of the existing affiliation's type at submission time, purely for
        /// display. Null/ignored when Kind is Family - see SuggestedRelationType instead.
        /// </summary>
        public PersonAffiliationType? SuggestedAffiliationType { get; set; }

        /// <summary>
        /// see GanjoorPersonRelation.DegreeHint - same for/Add/Modify/Remove-display purpose as
        /// SuggestedRelationType above
        /// </summary>
        public int? SuggestedDegreeHint { get; set; }

        /// <summary>
        /// see GanjoorPersonRelation.Note - same Add/Modify/Remove-display purpose as
        /// SuggestedRelationType above
        /// </summary>
        public string SuggestedNote { get; set; }

        /// <summary>
        /// suggester's note to the moderator (e.g. why an existing relation is wrong)
        /// </summary>
        public string SuggestionNote { get; set; }

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
