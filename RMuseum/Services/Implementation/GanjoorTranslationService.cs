using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// translation service implementation
    /// </summary>
    public class GanjoorTranslationService : IGanjoorTranslationService
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
        /// add or update poem translation
        /// </summary>
        /// <param name="translation"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> AddOrUpdatePoemTranslation(GanjoorPoemTranslationViewModel translation)
        {
            try
            {
                var dbTranslation = await _context.PoemTranslations.Where(t => t.LanguageId == translation.LanguageId && t.PoemId == translation.PoemId).SingleOrDefaultAsync();
                if(dbTranslation == null)
                {
                    dbTranslation = new GanjoorPoemTranslation()
                    {
                        LanguageId = translation.LanguageId,
                        PoemId = translation.PoemId,
                        Title = translation.Title,
                        Published = true
                    };
                    _context.PoemTranslations.Add(dbTranslation);
                }
                else
                {
                    dbTranslation.Title = translation.Title;
                    _context.PoemTranslations.Update(dbTranslation);
                }

                var verses = await _context.GanjoorVerses.Where(v => v.PoemId == translation.PoemId).ToListAsync();
                foreach (var translatedVerse in translation.TranslatedVerses)
                {
                    var verse = verses.Where(v => v.VOrder == translatedVerse.VOrder).Single();
                    var dbTranslatedVerse = await _context.VerseTranslations.Where(t => t.LanguageId == translation.LanguageId && t.VerseId == verse.Id).SingleOrDefaultAsync();
                    if (dbTranslatedVerse == null)
                    {
                        dbTranslatedVerse = new GanjoorVerseTranslation()
                        {
                            LanguageId = translation.LanguageId,
                            VerseId = verse.Id,
                            TText = translatedVerse.TText
                        };
                        _context.VerseTranslations.Add(dbTranslatedVerse);
                    }
                    else
                    {
                        dbTranslatedVerse.TText = translatedVerse.TText;
                        _context.VerseTranslations.Update(dbTranslatedVerse);
                    }
                }

                await _context.SaveChangesAsync();

                return new RServiceResult<bool>(true);
            }
            catch(Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// get translation
        /// </summary>
        /// <param name="langId"></param>
        /// <param name="poemId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPoemTranslationViewModel>> GetTranslationAsync(int langId, int poemId)
        {
            try
            {
                var dbTranslation = await _context.PoemTranslations.Where(t => t.LanguageId == langId && t.PoemId == poemId).SingleOrDefaultAsync();
                var verses = await _context.GanjoorVerses.Where(v => v.PoemId == poemId).ToListAsync();
                var verseIds = verses.Select(v => v.Id).ToList();
                var dbVerseTranslations = await _context.VerseTranslations.Where(t => t.LanguageId == langId && verseIds.Contains(t.VerseId)).OrderBy(t => t.VerseId).ToArrayAsync();

                return new RServiceResult<GanjoorPoemTranslationViewModel>
                    (
                    new GanjoorPoemTranslationViewModel()
                    {
                        LanguageId = langId,
                        PoemId = poemId,
                        Title = dbTranslation == null ? null : dbTranslation.Title,
                        TranslatedVerses = dbVerseTranslations.Select(v => 
                        new GanjoorVerseTranslationViewModel()
                        {
                            VOrder = verses.Where(pv => pv.Id == v.VerseId).Single().VOrder,
                            TText = v.TText
                        }
                        ).ToArray()
                    }
                    );

            }
            catch(Exception exp)
            {
                return new RServiceResult<GanjoorPoemTranslationViewModel>(null, exp.ToString());
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
