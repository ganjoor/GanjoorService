using RMuseum.Models.FAQ;
using RSecurityBackend.Models.Generic;
using System.Threading.Tasks;


namespace RMuseum.Services
{
    /// <summary>
    /// faq service interface
    /// </summary>
    public interface IFAQService
    {
        /// <summary>
        /// add faq category
        /// </summary>
        /// <param name="cat"></param>
        /// <returns></returns>
        Task<RServiceResult<FAQCategory>> AddCategoryAsync(FAQCategory cat);

        /// <summary>
        /// update faq cateory
        /// </summary>
        /// <param name="cat"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> UpdateCategoryAsync(FAQCategory cat);

        /// <summary>
        /// delete faq category
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeleteCategoryAsync(int id);
    }
}
