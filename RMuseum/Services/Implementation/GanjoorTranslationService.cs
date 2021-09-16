using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Generic;
using System;
using System.Collections.Generic;
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
        /// <param name="lang"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorLanguage>> AddLanguageAsync(GanjoorLanguage lang)
        {
            lang.Name = lang.Name == null ? lang.Name : lang.Name.Trim();
            lang.Code = lang.Code == null ? lang.Code : lang.Code.Trim();
            if (string.IsNullOrEmpty(lang.Name) || string.IsNullOrEmpty(lang.Code))
                return new RServiceResult<GanjoorLanguage>(null, "ورود کد و نام زبان اجباری است.");
            var existing = await _context.GanjoorLanguages.Where(l => l.Name == lang.Name || l.Code == lang.Code).FirstOrDefaultAsync();
            if (existing != null)
                return new RServiceResult<GanjoorLanguage>(null, "اطلاعات تکراری است.");
            try
            {
                _context.GanjoorLanguages.Add(lang);
                await _context.SaveChangesAsync();
                return new RServiceResult<GanjoorLanguage>(lang);
            }
            catch (Exception exp)
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
                lang.Code = updated.Code;
                lang.NativeName = updated.NativeName;
                lang.Description = updated.Description;
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
        /// delete language
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
        /// <param name="userId"></param>
        /// <param name="translation"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> AddPoemTranslation(Guid userId, GanjoorPoemTranslationViewModel translation)
        {
            try
            {
                if (translation.Published)
                {
                    var oldPublished = await _context.GanjoorPoemTranslations.Include(t => t.Language).Where(t => t.Language.Id == translation.Language.Id && t.PoemId == translation.PoemId && t.Published == true).ToListAsync();
                    foreach (var item in oldPublished)
                    {
                        item.Published = false;
                        _context.GanjoorPoemTranslations.Update(item);
                    }
                }
                var dbTranslation = new GanjoorPoemTranslation()
                {
                    LanguageId = translation.Language.Id,
                    PoemId = translation.PoemId,
                    Title = translation.Title,
                    Published = true,
                    Verses = new List<GanjoorVerseTranslation>(),
                    UserId = userId,
                    DateTime = DateTime.Now
                };

                var verses = await _context.GanjoorVerses.Where(v => v.PoemId == translation.PoemId).ToListAsync();
                foreach (var translatedVerse in translation.TranslatedVerses)
                {
                    var verse = verses.Where(v => v.VOrder == translatedVerse.Verse.Id).Single();
                    dbTranslation.Verses
                    .Add(
                        new GanjoorVerseTranslation()
                        {
                            VerseId = verse.Id,
                            TText = translatedVerse.TText
                        });
                }

                _context.GanjoorPoemTranslations.Add(dbTranslation);

                await _context.SaveChangesAsync();

                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
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
                var dbTranslation = await _context.GanjoorPoemTranslations.Include(t => t.Verses).Where(t => t.LanguageId == langId && t.PoemId == poemId && t.Published == true).Include(t => t.Language).SingleOrDefaultAsync();
                if (dbTranslation == null)
                    return new RServiceResult<GanjoorPoemTranslationViewModel>(null); //not found
                var verses = await _context.GanjoorVerses.Where(v => v.PoemId == poemId).ToListAsync();
                var verseIds = verses.Select(v => v.Id).ToList();

                return new RServiceResult<GanjoorPoemTranslationViewModel>
                    (
                    new GanjoorPoemTranslationViewModel()
                    {
                        Language = dbTranslation.Language,
                        PoemId = poemId,
                        Title = dbTranslation.Title,
                        Description = dbTranslation.Description,
                        ContributerName = string.IsNullOrEmpty(dbTranslation.User.NickName) ? dbTranslation.User.Id.ToString() : dbTranslation.User.NickName,
                        TranslatedVerses = dbTranslation.Verses.Select(v =>
                        new GanjoorVerseTranslationViewModel()
                        {
                            Verse = verses.Where(pv => pv.Id == v.VerseId)
                                .Select(pv =>
                                new GanjoorVerseViewModel()
                                {
                                    Id = pv.Id,
                                    VersePosition = pv.VersePosition,
                                    VOrder = pv.VOrder,
                                    Text = pv.Text
                                }
                                ).Single(),
                            TText = v.TText
                        }
                        ).ToArray()
                    }
                    );

            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorPoemTranslationViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get translations for a poem
        /// </summary>
        /// <param name="poemId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPoemTranslationViewModel[]>> GetTranslationsAsync(int poemId)
        {
            try
            {
                var dbTranslations = await _context.GanjoorPoemTranslations.Include(t => t.Verses).Where(t => t.PoemId == poemId && t.Published == true).Include(t => t.Language).Include(t => t.User).OrderBy(t => t.LanguageId).ToArrayAsync();
                List<GanjoorPoemTranslationViewModel> res = new List<GanjoorPoemTranslationViewModel>();
                if (dbTranslations.Length > 0)
                {
                    var verses = await _context.GanjoorVerses.Where(v => v.PoemId == poemId).ToListAsync();
                    var verseIds = verses.Select(v => v.Id).ToList();
                    foreach (var dbTranslation in dbTranslations)
                    {

                        res.Add(
                            new GanjoorPoemTranslationViewModel()
                            {
                                Language = dbTranslation.Language,
                                PoemId = poemId,
                                Title = dbTranslation == null ? null : dbTranslation.Title,
                                Description = dbTranslation.Description,
                                ContributerName = string.IsNullOrEmpty(dbTranslation.User.NickName) ? dbTranslation.User.Id.ToString() : dbTranslation.User.NickName,
                                TranslatedVerses = dbTranslation.Verses.Select(v =>
                                new GanjoorVerseTranslationViewModel()
                                {
                                    Verse = verses.Where(pv => pv.Id == v.VerseId)
                                        .Select(pv =>
                                        new GanjoorVerseViewModel()
                                        {
                                            Id = pv.Id,
                                            VersePosition = pv.VersePosition,
                                            VOrder = pv.VOrder,
                                            Text = pv.Text
                                        }
                                        ).Single(),
                                    TText = v.TText
                                }
                                ).ToArray()
                            }
                            );
                    }
                }
                return new RServiceResult<GanjoorPoemTranslationViewModel[]>(res.ToArray());

            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorPoemTranslationViewModel[]>(null, exp.ToString());
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
