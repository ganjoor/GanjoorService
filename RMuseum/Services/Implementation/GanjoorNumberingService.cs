using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RSecurityBackend.Models.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// numbering service
    /// </summary>
    public class GanjoorNumberingService
    {
        /// <summary>
        /// add numbering
        /// </summary>
        /// <param name="numbering"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorNumbering>> AddNumberingAsync(GanjoorNumbering numbering)
        {
            numbering.Name = numbering.Name == null ? numbering.Name : numbering.Name.Trim();
            if (string.IsNullOrEmpty(numbering.Name))
                return new RServiceResult<GanjoorNumbering>(null, "ورود نام طرح شماره‌گذاری اجباری است.");
            var existing = await _context.GanjoorNumberings.Where(l => l.Name == numbering.Name).FirstOrDefaultAsync();
            if (existing != null)
                return new RServiceResult<GanjoorNumbering>(null, "اطلاعات تکراری است.");
            try
            {
                _context.GanjoorNumberings.Add(numbering);
                await _context.SaveChangesAsync();
                return new RServiceResult<GanjoorNumbering>(numbering);
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorNumbering>(null, exp.ToString());
            }
        }

        /// <summary>
        /// update an existing numbering (only name)
        /// </summary>
        /// <param name="updated"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> UpdateNumberingAsync(GanjoorNumbering updated)
        {
            try
            {
                var numbering = await _context.GanjoorNumberings.Where(l => l.Id == updated.Id).SingleOrDefaultAsync();
                if (numbering == null)
                    return new RServiceResult<bool>(false, "اطلاعات طرح شماره گذاری یافت نشد.");

                numbering.Name = updated.Name;
                _context.Update(numbering);

                await _context.SaveChangesAsync();
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// delete numbering
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeleteNumberingAsync(int id)
        {
            try
            {
                var numbering = await _context.GanjoorNumberings.Where(l => l.Id == id).SingleOrDefaultAsync();
                if (numbering == null)
                    return new RServiceResult<bool>(false, "اطلاعات طرح شماره گذاری یافت نشد.");

                _context.Remove(numbering);

                await _context.SaveChangesAsync();
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// get numbering by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorNumbering>> GetNumberingAsync(int id)
        {
            try
            {
                return new RServiceResult<GanjoorNumbering>(await _context.GanjoorNumberings.Where(l => l.Id == id).SingleOrDefaultAsync());
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorNumbering>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get all numberings
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorNumbering[]>> GetNumberingsAsync()
        {
            try
            {
                return new RServiceResult<GanjoorNumbering[]>(await _context.GanjoorNumberings.OrderBy(l => l.Id).ToArrayAsync());
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorNumbering[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get numberings for a cat
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorNumbering[]>> GetNumberingsForCatAsync(int catId)
        {
            try
            {
                return new RServiceResult<GanjoorNumbering[]>(await _context.GanjoorNumberings.Where(n => n.StartCatId == catId).OrderBy(l => l.Id).ToArrayAsync());
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorNumbering[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get numberings for direct subcats of parent cat
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorNumbering[]>> GetNumberingsForDirectSubCatsAsync(int parentCatId)
        {
            try
            {
                var subcatIds = await _context.GanjoorCategories.Where(c => c.ParentId == parentCatId).Select(c => c.Id).ToListAsync();
                return new RServiceResult<GanjoorNumbering[]>(await _context.GanjoorNumberings.Where(n => subcatIds.Contains(n.StartCatId)).OrderBy(l => l.Id).ToArrayAsync());
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorNumbering[]>(null, exp.ToString());
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
        public GanjoorNumberingService(RMuseumDbContext context)
        {
            _context = context;
        }
    }
}
