using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RMuseum.Models.PDFLibrary;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services.Implementation;
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
            if (string.IsNullOrEmpty(lang.NativeName))
                lang.NativeName = lang.Name;
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

                var translations = await _context.GanjoorPoemTranslations.Include(t => t.Verses).Where(l => l.LanguageId == id).ToListAsync();
                _context.RemoveRange(translations);

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
        public async Task<RServiceResult<GanjoorPoemTranslationViewModel>> AddPoemTranslation(Guid userId, GanjoorPoemTranslationViewModel translation)
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
                    Published = translation.Published,
                    Verses = new List<GanjoorVerseTranslation>(),
                    UserId = userId,
                    DateTime = DateTime.Now
                };

                var verses = await _context.GanjoorVerses.Where(v => v.PoemId == translation.PoemId).ToListAsync();
                foreach (var translatedVerse in translation.TranslatedVerses)
                {
                    var verse = verses.Where(v => v.VOrder == translatedVerse.Verse.VOrder).Single();
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

                translation.Id = dbTranslation.Id;

                return new RServiceResult<GanjoorPoemTranslationViewModel>(translation);
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorPoemTranslationViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get last language the user contributed to its translation
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorLanguage>> GetLastUserContributedLanguage(Guid userId)
        {
            var dbTranslation = await _context.GanjoorPoemTranslations.Where(t => t.UserId == userId).OrderByDescending(t => t.Id).FirstOrDefaultAsync();
            if (dbTranslation == null)
                return new RServiceResult<GanjoorLanguage>(null);
            return await GetLanguageAsync(dbTranslation.LanguageId);

        }

        /// <summary>
        /// get all poem translations (for export utility)
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="langId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, GanjoorPoemTranslationViewModel[] Translations)>> GetAllPoemsTranslations(PagingParameterModel paging, int langId)
        {
            try
            {
                var source = _context.GanjoorPoemTranslations.AsNoTracking()
                            .Where(t => t.LanguageId == langId)
                            .Include(t => t.Language).Include(t => t.User)
                            .OrderBy(t => t.Id)
                            .Select(
                    dbTranslation =>
                    new GanjoorPoemTranslationViewModel()
                    {
                        Id = dbTranslation.Id,
                        Language = dbTranslation.Language,
                        PoemId = dbTranslation.PoemId,
                        Title = dbTranslation == null ? null : dbTranslation.Title,
                        Description = dbTranslation.Description,
                        Published = dbTranslation.Published,
                        ContributerName = string.IsNullOrEmpty(dbTranslation.User.NickName) ? dbTranslation.User.Id.ToString() : dbTranslation.User.NickName,
                        ContributerId = dbTranslation.UserId,
                        DateTime = dbTranslation.DateTime,
                    }) 
                            .AsQueryable();
                return new RServiceResult<(PaginationMetadata PagingMeta, GanjoorPoemTranslationViewModel[] Translations)>
                    (await QueryablePaginator<GanjoorPoemTranslationViewModel>.Paginate(source, paging));

            }
            catch (Exception exp)
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, GanjoorPoemTranslationViewModel[] Translations)>((null, null), exp.ToString());
            }
        }

        /// <summary>
        /// get translation by id
        /// </summary>
        ///<param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPoemTranslationViewModel>> GetPoemTranslationById(int id)
        {
            try
            {
                var dbTranslation = await _context.GanjoorPoemTranslations.Include(t => t.Verses).Where(t => t.Id == id).Include(t => t.Language).Include(t => t.User).OrderByDescending(t => t.Id).SingleAsync();
                var verses = await _context.GanjoorVerses.Where(v => v.PoemId == dbTranslation.PoemId).ToListAsync();
                var verseIds = verses.Select(v => v.Id).ToList();
               
                return new RServiceResult<GanjoorPoemTranslationViewModel>
                    (
                    new GanjoorPoemTranslationViewModel()
                    {
                        Id = dbTranslation.Id,
                        Language = dbTranslation.Language,
                        PoemId = dbTranslation.PoemId,
                        Title = dbTranslation == null ? null : dbTranslation.Title,
                        Description = dbTranslation.Description,
                        Published = dbTranslation.Published,
                        ContributerName =  string.IsNullOrEmpty(dbTranslation.User.NickName) ? dbTranslation.User.Id.ToString() : dbTranslation.User.NickName,
                        ContributerId = dbTranslation.UserId,
                        DateTime = dbTranslation.DateTime,
                        TranslatedVerses = dbTranslation.Verses.Select(v =>
                        new GanjoorVerseTranslationViewModel()
                        {
                            Verse = verses.Where(pv => pv.Id == v.VerseId)
                                .Select(pv =>
                                new GanjoorVerseViewModel()
                                {
                                    Id = pv.Id,
                                    VersePosition = pv.VersePosition,
                                    CoupletIndex = pv.CoupletIndex,
                                    VOrder = pv.VOrder,
                                    Text = pv.Text,
                                    SectionIndex1 = pv.SectionIndex1,
                                    SectionIndex2 = pv.SectionIndex2,
                                    SectionIndex3 = pv.SectionIndex3,
                                    SectionIndex4 = pv.SectionIndex4,
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
        /// get translation
        /// </summary>
        /// <param name="langId"></param>
        /// <param name="poemId"></param>
        /// <param name="onlyPublished"></param>
        /// <param name="includeUserInfo"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPoemTranslationViewModel[]>> GetPoemTranslationsAsync(int langId, int poemId, bool onlyPublished, bool includeUserInfo)
        {
            try
            {
                var dbTranslations = await _context.GanjoorPoemTranslations.Include(t => t.Verses).Where(t => t.PoemId == poemId && (langId == -1 || t.LanguageId == langId) && (onlyPublished == false || t.Published == true)).Include(t => t.Language).Include(t => t.User).OrderByDescending(t => t.Id).ToArrayAsync();
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
                                Id = dbTranslation.Id,
                                Language = dbTranslation.Language,
                                PoemId = poemId,
                                Title = dbTranslation == null ? null : dbTranslation.Title,
                                Description = dbTranslation.Description,
                                Published = dbTranslation.Published,
                                ContributerName = includeUserInfo ? string.IsNullOrEmpty(dbTranslation.User.NickName) ? dbTranslation.User.Id.ToString() : dbTranslation.User.NickName : null,
                                ContributerId = includeUserInfo ? dbTranslation.UserId : Guid.Empty,
                                DateTime = dbTranslation.DateTime,
                                TranslatedVerses = dbTranslation.Verses.Select(v =>
                                new GanjoorVerseTranslationViewModel()
                                {
                                    Verse = verses.Where(pv => pv.Id == v.VerseId)
                                        .Select(pv =>
                                        new GanjoorVerseViewModel()
                                        {
                                            Id = pv.Id,
                                            VersePosition = pv.VersePosition,
                                            CoupletIndex = pv.CoupletIndex,
                                            VOrder = pv.VOrder,
                                            Text = pv.Text,
                                            SectionIndex1 = pv.SectionIndex1,
                                            SectionIndex2 = pv.SectionIndex2,
                                            SectionIndex3 = pv.SectionIndex3,
                                            SectionIndex4 = pv.SectionIndex4,
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
