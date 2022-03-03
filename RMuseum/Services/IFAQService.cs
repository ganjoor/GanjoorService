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
        /// get categories
        /// </summary>
        /// <param name="onlyPublished"></param>
        /// <returns></returns>
        Task<RServiceResult<FAQCategory[]>> GetCategoriesAsync(bool onlyPublished);

        /// <summary>
        /// get category by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<FAQCategory>> GetCategoryByIdAsync(int id);

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
        /// get pinned items
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<FAQCategory[]>> GetPinnedItemsAsync();

        /// <summary>
        /// get category items
        /// </summary>
        /// <param name="categoryId"></param>
        /// <param name="onlyPublished"></param>
        /// <returns></returns>
        Task<RServiceResult<FAQItem[]>> GetCategoryItemsAsync(int categoryId, bool onlyPublished);

        /// <summary>
        /// get item by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<FAQItem>> GetItemByIdAsync(int id);

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
