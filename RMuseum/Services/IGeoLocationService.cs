using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Generic;
using System.Threading.Tasks;

namespace RMuseum.Services
{
    /// <summary>
    /// geo location service
    /// </summary>
    public interface IGeoLocationService
    {
        /// <summary>
        /// add new location
        /// </summary>
        /// <param name="name"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
       Task<RServiceResult<GanjoorGeoLocationViewModel>> AddLocationAsync(string name, double x, double y);

        /// <summary>
        /// update an existing location
        /// </summary>
        /// <param name="updated"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> UpdateLocationAsync(GanjoorGeoLocationViewModel updated);

        /// <summary>
        /// delete location
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeleteLocationAsync(int id);

        /// <summary>
        /// get location by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorGeoLocationViewModel>> GetLocationAsync(int id);

        /// <summary>
        /// get all locations
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<GanjoorGeoLocationViewModel[]>> GetLocationsAsync();
    }
}
