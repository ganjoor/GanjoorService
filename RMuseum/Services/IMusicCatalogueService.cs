using RMuseum.Models.MusicCatalogue.ViewModels;
using RSecurityBackend.Models.Generic;
using System.Threading.Tasks;

namespace RMuseum.Services
{
    /// <summary>
    /// Music Catalogue Service
    /// </summary>
    public interface IMusicCatalogueService 
    {
        /// <summary>
        /// get golha collection programs
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<GolhaProgramViewModel[]>> GetGolhaCollectionPrograms(int id);

        /// <summary>
        /// get golha program tracks
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<GolhaTrackViewModel[]>> GetGolhaProgramTracks(int id);
    }
}
