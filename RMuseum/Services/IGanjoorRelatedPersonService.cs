using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Generic;
using System;
using System.Threading.Tasks;

namespace RMuseum.Services
{
    /// <summary>
    /// related people (family tree / person tagging) service. A new person is normally created via
    /// the geo/date/person tag correction's SuggestedPersonGraphJson, materialized on moderator
    /// approval (see GanjoorService-ModeratePoemCorrection.cs). Editing an already-approved person's
    /// own fields (e.g. adding a FamilyTreeCaption after the fact) goes through the suggest/review
    /// queue below (SuggestPersonEditAsync / ModeratePersonEditSuggestionAsync) - there is no
    /// direct-edit path; nothing ever writes to a GanjoorRelatedPerson's fields except that approval
    /// step and the original creation-on-approval in GanjoorService-ModeratePoemCorrection.cs.
    /// </summary>
    public interface IGanjoorRelatedPersonService
    {
        /// <summary>
        /// get all people (for the search-as-you-type person picker)
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<GanjoorRelatedPerson[]>> GetPeopleAsync();

        /// <summary>
        /// get person by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorRelatedPerson>> GetPersonAsync(int id);

        /// <summary>
        /// get people who caption a family tree (GanjoorRelatedPerson.FamilyTreeCaption not empty) -
        /// used as the entry points for browsing family trees
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<GanjoorRelatedPerson[]>> GetFamilyTreeRootsAsync();

        /// <summary>
        /// get a person along with all their kinship/affiliation edges (resolved with the other
        /// side's name), for the read-only person/family-tree browsing page
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPersonRelationsViewModel>> GetPersonRelationsAsync(int id);

        /// <summary>
        /// get the (approved, materialized) poem geo/date tags that name this person, each carrying
        /// enough of its Poem to link to it
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<PoemGeoDateTag[]>> GetPoemsByPersonAsync(int id);

        /// <summary>
        /// get the whole connected kinship component reachable from this person (ancestors,
        /// descendants, spouses, siblings - whichever edges connect to it, transitively), for the
        /// interactive family-tree chart at /FamilyTree/{id}
        /// </summary>
        /// <param name="rootId"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorFamilyTreeViewModel>> GetFamilyTreeAsync(int rootId);

        /// <summary>
        /// submit a suggested edit to an already-approved person's own fields - goes into the
        /// pending queue, does not change the person itself until a moderator approves it
        /// </summary>
        /// <param name="suggestion"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPersonEditSuggestion>> SuggestPersonEditAsync(GanjoorPersonEditSuggestion suggestion);

        /// <summary>
        /// get the next unreviewed person-edit suggestion (for the moderator queue), including the
        /// target person's current fields (for a before/after diff) and the suggester's nickname
        /// </summary>
        /// <param name="skip"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPersonEditSuggestion>> GetNextUnreviewedPersonEditSuggestionAsync(int skip);

        /// <summary>
        /// unreviewed person-edit suggestion count
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<int>> GetUnreviewedPersonEditSuggestionCountAsync();

        /// <summary>
        /// apply a moderator's decision to a pending person-edit suggestion. On Approved, copies the
        /// suggestion's Suggested* fields onto the target GanjoorRelatedPerson (Id and
        /// MachineGenerated on the person are left untouched); any other result just marks the
        /// suggestion reviewed/rejected without touching the person.
        /// </summary>
        /// <param name="moderatorUserId"></param>
        /// <param name="suggestionId"></param>
        /// <param name="result"></param>
        /// <param name="reviewNote"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPersonEditSuggestion>> ModeratePersonEditSuggestionAsync(Guid moderatorUserId, int suggestionId, CorrectionReviewResult result, string reviewNote);

        /// <summary>
        /// get a single kinship edge by its own id, with both sides' names resolved - used by
        /// /SuggestPersonRelationEdit/{relationId} to show what it's about
        /// </summary>
        /// <param name="relationId"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPersonRelation>> GetRelationByIdAsync(int relationId);

        /// <summary>
        /// submit a suggested addition, change or removal of a kinship edge - goes into the
        /// pending queue, does not change anything until a moderator approves it
        /// </summary>
        /// <param name="suggestion"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPersonRelationEditSuggestion>> SuggestPersonRelationEditAsync(GanjoorPersonRelationEditSuggestion suggestion);

        /// <summary>
        /// get the next unreviewed relation-edit suggestion (for the moderator queue)
        /// </summary>
        /// <param name="skip"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPersonRelationEditSuggestion>> GetNextUnreviewedPersonRelationEditSuggestionAsync(int skip);

        /// <summary>
        /// unreviewed relation-edit suggestion count
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<int>> GetUnreviewedPersonRelationEditSuggestionCountAsync();

        /// <summary>
        /// apply a moderator's decision to a pending relation-edit suggestion. On Approved: Add
        /// creates a new GanjoorPersonRelation, Modify updates the existing one ExistingRelationId
        /// points to, Remove deletes it (and auto-rejects any other still-pending suggestion that
        /// also targeted that same now-gone relation, so it doesn't dangle).
        /// </summary>
        /// <param name="moderatorUserId"></param>
        /// <param name="suggestionId"></param>
        /// <param name="result"></param>
        /// <param name="reviewNote"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPersonRelationEditSuggestion>> ModeratePersonRelationEditSuggestionAsync(Guid moderatorUserId, int suggestionId, CorrectionReviewResult result, string reviewNote);

        /// <summary>
        /// get the whole known network of people (every person with at least one kinship edge or
        /// non-family tie, plus every one of those edges/ties) for the force-directed "ontology"
        /// explorer opened via the PeopleExplorer.open() modal (formerly the standalone page
        /// /PeopleGraph) - see GanjoorPersonGraphViewModel
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPersonGraphViewModel>> GetPersonGraphAsync();

        /// <summary>
        /// get the network of people relevant to one work/category (a poet's whole corpus, one book
        /// like the Shahnameh, or a narrower story within it) - every person tagged in a poem under
        /// catId's subtree, plus their relatives/affiliates one hop out even if never tagged
        /// themselves - see GanjoorPersonGraphNode.DirectlyTagged
        /// </summary>
        /// <param name="catId"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPersonGraphViewModel>> GetCatPersonGraphAsync(int catId);
    }
}
