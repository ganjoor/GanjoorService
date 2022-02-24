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
        /// add a new faq category
        /// </summary>
        /// <param name="cat"></param>
        /// <returns></returns>
        Task<RServiceResult<FAQCategory>> AddCategoryAsync(FAQCategory cat);

        /// <summary>
        /// update an existing faq category
        /// </summary>
        /// <param name="cat"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> UpdateCategoryAsync(FAQCategory cat);

        /// <summary>
        /// delete a faq category
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeleteCategoryAsync(int id);

        /// <summary>
        /// add a new faq item
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        Task<RServiceResult<FAQItem>> AddItemAsync(FAQItem item);

        /// <summary>
        /// update an existing faq item
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> UpdateItemAsync(FAQItem item);

        /// <summary>
        /// delete a faq item
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeleteItemAsync(int id);

    }
}
