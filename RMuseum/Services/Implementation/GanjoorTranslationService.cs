using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RSecurityBackend.Models.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    public class GanjoorTranslationService
    {
        /// <summary>
        /// add language
        /// </summary>
        /// <param name="name"></param>
        /// <param name="rtl"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorLanguage>> AddLanguageAsync(string name, bool rtl)
        {
            var existing = await _context.GanjoorLanguages.Where(l => l.Name == name).FirstOrDefaultAsync();
            if (existing != null)
                return new RServiceResult<GanjoorLanguage>(null, "نام تکراری است.");
            try
            {
                var lang = new GanjoorLanguage()
                {
                    Name = name,
                    RightToLeft = rtl
                };
                _context.GanjoorLanguages.Add(lang);
                await _context.SaveChangesAsync();
                return new RServiceResult<GanjoorLanguage>(lang);
            }
            catch(Exception exp)
            {
                return new RServiceResult<GanjoorLanguage>(null, exp.ToString());
            }
        }

        /// <summary>
        /// update an existing language
        /// </summary>
        /// <param name="updated"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> UpdateLangaugeAsync(GanjoorLanguage updated)
        {
            try
            {
                var lang = await _context.GanjoorLanguages.Where(l => l.Id == updated.Id).SingleOrDefaultAsync();
                if (lang == null)
                    return new RServiceResult<bool>(false, "اطلاعات زبان یافت نشد.");

                lang.Name = updated.Name;
                lang.RightToLeft = updated.RightToLeft;
                _context.Update(lang);

                await _context.SaveChangesAsync();
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// حذف زبان
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeleteLangaugeAsync(int id)
        {
            try
            {
                var lang = await _context.GanjoorLanguages.Where(l => l.Id == id).SingleOrDefaultAsync();
                if (lang == null)
                    return new RServiceResult<bool>(false, "اطلاعات زبان یافت نشد.");

                _context.Remove(lang);

                await _context.SaveChangesAsync();
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// get langauge by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorLanguage>> GetLanguageAsync(int id)
        {
            try
            {
                return new RServiceResult<GanjoorLanguage>(await _context.GanjoorLanguages.Where(l => l.Id == id).SingleOrDefaultAsync());
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorLanguage>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get all languages
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorLanguage[]>> GetLanguagesAsync()
        {
            try
            {
                return new RServiceResult<GanjoorLanguage[]>(await _context.GanjoorLanguages.OrderBy(l => l.Id).ToArrayAsync());
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorLanguage[]>(null, exp.ToString());
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
        public GanjoorTranslationService(RMuseumDbContext context)
        {
            _context = context;
        }
    }
}
