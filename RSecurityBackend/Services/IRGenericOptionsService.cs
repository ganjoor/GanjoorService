using RSecurityBackend.Models.Generic;
using RSecurityBackend.Models.Generic.Db;
using System.Threading.Tasks;

namespace RSecurityBackend.Services
{
    /// <summary>
    /// generic options service
    /// </summary>
    public interface IRGenericOptionsService
    {
        /// <summary>
        /// modify an option or add a new one if an option with the requested name does not exist
        /// </summary>
        /// <param name="optionName"></param>
        /// <param name="optionValue"></param>
        /// <returns></returns>
        Task<RServiceResult<RGenericOption>> SetAsync(string optionName, string optionValue);

        /// <summary>
        /// get option value
        /// </summary>
        /// <param name="optionName"></param>
        /// <returns></returns>
        Task<RServiceResult<string>> GetValueAsync(string optionName);
    }
}
