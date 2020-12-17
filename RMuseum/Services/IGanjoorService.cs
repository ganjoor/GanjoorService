using RMuseum.Models.Ganjoor;
using RMuseum.Models.GanjoorAudio.ViewModels;
using RSecurityBackend.Models.Generic;
using System.Threading.Tasks;

namespace RMuseum.Services
{
    /// <summary>
    /// Ganjoor Poems Content Privider Service
    /// </summary>
    public interface IGanjoorService
    {
        /// <summary>
        /// get poem by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoem>> GetPoemById(int id);

        /// <summary>
        /// get poem recitations (PlainText/HtmlText are intentionally empty)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<PublicRecitationViewModel[]>> GetPoemRecitations(int id);

        /// <summary>
        /// imports unimported poem data from a locally accessible ganjoor SqlLite database
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<bool>> ImportLocalSQLiteDb();

        /// <summary>
        /// updates poems text
        /// </summary>
        /// <returns></returns>
        RServiceResult<bool> UpdatePoemsText();
    }
}
