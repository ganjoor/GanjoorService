using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Generic;
using System.IO;
using System.Threading.Tasks;

namespace RMuseum.Services
{
    public interface ISiteBannersService
    {
        /// <summary>
        /// Add site banner
        /// </summary>
        /// <param name="imageStream"></param>
        /// <param name="fileName"></param>
        /// <param name="alternateText"></param>
        /// <param name="targetUrl"></param>
        /// <param name="active"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorSiteBannerViewModel>> AddSiteBanner(Stream imageStream, string fileName, string alternateText, string targetUrl, bool active);

        /// <summary>
        /// modify site banner
        /// </summary>
        /// <param name="id"></param>
        /// <param name="alternateText"></param>
        /// <param name="targetUrl"></param>
        /// <param name="active"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> ModifySiteBanner(int id, string alternateText, string targetUrl, bool active);

        /// <summary>
        /// delete site banner
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeleteSiteBanner(int id);

        /// <summary>
        /// get site banners
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<GanjoorSiteBannerViewModel[]>> GetSiteBanners();

        /// <summary>
        /// get a random site banner
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<GanjoorSiteBannerViewModel>> GetARandomActiveSiteBanner();
    }
}
