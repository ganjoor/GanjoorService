using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Generic;
using System.Threading.Tasks;

namespace RMuseum.Services
{
    /// <summary>
    /// numbering service 
    /// </summary>
    public interface IGanjoorNumberingService
    {
        /// <summary>
        /// add numbering
        /// </summary>
        /// <param name="numbering"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorNumbering>> AddNumberingAsync(GanjoorNumbering numbering);

        /// <summary>
        /// update an existing numbering (only name)
        /// </summary>
        /// <param name="updated"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> UpdateNumberingAsync(GanjoorNumbering updated);

        /// <summary>
        /// delete numbering
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeleteNumberingAsync(int id);

        /// <summary>
        /// get numbering by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorNumbering>> GetNumberingAsync(int id);

        /// <summary>
        /// get all numberings
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<GanjoorNumbering[]>> GetNumberingsAsync();

        /// <summary>
        /// get numberings for a cat
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<GanjoorNumbering[]>> GetNumberingsForCatAsync(int catId);

        /// <summary>
        /// get numberings for direct subcats of parent cat
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<GanjoorNumbering[]>> GetNumberingsForDirectSubCatsAsync(int parentCatId);

        /// <summary>
        /// get all numbering patterns for a couplet
        /// </summary>
        /// <param name="poemId"></param>
        /// <param name="coupletIndex"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorCoupletNumberViewModel[]>> GetNumberingsForCouplet(int poemId, int coupletIndex);

        /// <summary>
        /// start counting
        /// </summary>
        /// <returns></returns>
        RServiceResult<bool> Recount(int numberingId);

        /// <summary>
        /// generate missing default numberings and start counting
        /// </summary>
        /// <returns></returns>
        RServiceResult<bool> GenerateMissingDefaultNumberings();
    }
}
