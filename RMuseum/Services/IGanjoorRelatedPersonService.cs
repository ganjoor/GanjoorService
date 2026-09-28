using RMuseum.Models.Ganjoor;
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
    }
}
