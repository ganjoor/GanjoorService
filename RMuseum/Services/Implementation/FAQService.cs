using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.FAQ;
using RSecurityBackend.Models.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// faq service implementation
    /// </summary>
    public class FAQService : IFAQService
    {

        /// <summary>
        /// get categories
        /// </summary>
        /// <param name="onlyPublished"></param>
        /// <returns></returns>
        public async Task<RServiceResult<FAQCategory[]>> GetCategoriesAsync(bool onlyPublished)
        {
            return new RServiceResult<FAQCategory[]>
                (
                await _context.FAQCategories.Where(c => c.Published == true || onlyPublished == false).OrderBy(c => c.CatOrder).ToArrayAsync()
                );
        }

        /// <summary>
        /// get category by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<FAQCategory>> GetCategoryByIdAsync(int id)
        {
            return new RServiceResult<FAQCategory>
                (
                await _context.FAQCategories.Where(c => c.Id == id).SingleOrDefaultAsync()
                );
        }

        /// <summary>
        /// add a new faq category
        /// </summary>
        /// <param name="cat"></param>
        /// <returns></returns>
        public async Task<RServiceResult<FAQCategory>> AddCategoryAsync(FAQCategory cat)
        {
            try
            {
                _context.FAQCategories.Add(cat);
                await _context.SaveChangesAsync();

                return new RServiceResult<FAQCategory>(cat);
            }
            catch (Exception exp)
            {
                return new RServiceResult<FAQCategory>(null, exp.ToString());
            }
        }

        /// <summary>
        /// update an existing faq category
        /// </summary>
        /// <param name="cat"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> UpdateCategoryAsync(FAQCategory cat)
        {
            try
            {
                var dbModel = await _context.FAQCategories.Where(c => c.Id == cat.Id).SingleAsync();
                dbModel.Title = cat.Title;
                dbModel.CatOrder = cat.CatOrder;
                dbModel.Description = cat.Description;
                dbModel.Published = cat.Published;
                _context.Update(dbModel);
                await _context.SaveChangesAsync();

                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// delete a faq category
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeleteCategoryAsync(int id)
        {
            try
            {
                var dbModel = await _context.FAQCategories.Where(c => c.Id == id).SingleAsync();
                _context.Remove(dbModel);
                await _context.SaveChangesAsync();

                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// get pinned items
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<FAQCategory[]>> GetPinnedItemsAsync()
        {
            var items = await _context.FAQItems.Include(i => i.Category).Where(c => c.Pinned && c.Published == true).OrderBy(c => c.Category.CatOrder).ThenBy(c => c.PinnedItemOrder).ThenBy(c => c.ItemOrderInCategory).ToArrayAsync();
            List<FAQCategory> result = new List<FAQCategory>();
            FAQCategory cat = null;
            int catId = -1;
            foreach (var item in items)
            {
                if(item.CategoryId != catId)
                {
                    if(cat != null)
                    {
                        result.Add(cat);
                    }
                    cat = item.Category;
                    catId = item.CategoryId;
                    cat.Items = new List<FAQItem>();
                }
                cat.Items.Add(item);
            }
            if (cat != null)
            {
                result.Add(cat);
            }

            foreach (var resultCat in result)
            {
                foreach (var item in resultCat.Items)
                {
                    item.Category = null;
                }
            }
            
            return new RServiceResult<FAQCategory[]>(result.ToArray());
        }

        /// <summary>
        /// get category items
        /// </summary>
        /// <param name="categoryId"></param>
        /// <param name="onlyPublished"></param>
        /// <returns></returns>
        public async Task<RServiceResult<FAQItem[]>> GetCategoryItemsAsync(int categoryId, bool onlyPublished)
        {
            var items = await _context.FAQItems.Where(c => c.CategoryId == categoryId && (c.Published == onlyPublished || c.Published == true)).OrderBy(c => c.ItemOrderInCategory).ToArrayAsync();
            return new RServiceResult<FAQItem[]>
                (
                items
                );
        }

        /// <summary>
        /// get item by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<FAQItem>> GetItemByIdAsync(int id)
        {
            return new RServiceResult<FAQItem>
                (
                await _context.FAQItems.Where(c => c.Id == id).SingleOrDefaultAsync()
                );
        }

        /// <summary>
        /// add a new faq item
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public async Task<RServiceResult<FAQItem>> AddItemAsync(FAQItem item)
        {
            try
            {
                _context.FAQItems.Add(item);
                await _context.SaveChangesAsync();

                return new RServiceResult<FAQItem>(item);
            }
            catch (Exception exp)
            {
                return new RServiceResult<FAQItem>(null, exp.ToString());
            }
        }

        /// <summary>
        /// update an existing faq item
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> UpdateItemAsync(FAQItem item)
        {
            try
            {
                var dbModel = await _context.FAQItems.Where(i => i.Id == item.Id).SingleAsync();
                dbModel.Question = item.Question;
                dbModel.AnswerExcerpt = item.AnswerExcerpt;
                dbModel.FullAnswer = item.FullAnswer;
                dbModel.Pinned = item.Pinned;
                dbModel.PinnedItemOrder = item.PinnedItemOrder;
                dbModel.CategoryId = item.CategoryId;
                dbModel.ItemOrderInCategory = item.ItemOrderInCategory;
                dbModel.ContentForSearch = item.ContentForSearch;
                dbModel.HashTag1 = item.HashTag1;
                dbModel.HashTag2 = item.HashTag2;
                dbModel.HashTag3 = item.HashTag3;
                dbModel.HashTag4 = item.HashTag4;
                dbModel.Published = item.Published;
                _context.Update(dbModel);
                await _context.SaveChangesAsync();

                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// delete a faq item
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeleteItemAsync(int id)
        {
            try
            {
                var dbModel = await _context.FAQItems.Where(i => i.Id == id).SingleAsync();
                _context.Remove(dbModel);
                await _context.SaveChangesAsync();

                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }


        /// <summary>
        /// Database Context
        /// </summary>
        protected readonly RMuseumDbContext _context;


        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="context"></param>
        public FAQService(RMuseumDbContext context)
        {
            _context = context;
        }
    }
}
