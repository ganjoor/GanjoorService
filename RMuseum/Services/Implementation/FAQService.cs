using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.FAQ;
using RSecurityBackend.Models.Generic;
using System;
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
        /// add faq category
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
        /// update faq cateory
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
        /// delete faq category
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
