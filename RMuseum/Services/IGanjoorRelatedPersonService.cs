using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Generic;
using System.Threading.Tasks;

namespace RMuseum.Services
{
    /// <summary>
    /// related people (family tree / person tagging) service - read-only for now: the only way a
    /// new person is created is via the geo/date/person tag correction's SuggestedPersonGraphJson,
    /// materialized on moderator approval (see GanjoorService-ModeratePoemCorrection.cs)
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
    }
}
