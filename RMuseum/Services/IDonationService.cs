using RSecurityBackend.Models.Generic;
using System.Threading.Tasks;

namespace RMuseum.Services
{
    /// <summary>
    /// donation service
    /// </summary>
    public interface IDonationService
    {
        /// <summary>
        /// parse html of https://ganjoor.net/donate/ and fill the records
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<bool>> InitializeRecords();
    }
}
