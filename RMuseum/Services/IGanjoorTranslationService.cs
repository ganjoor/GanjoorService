using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Generic;
using System;
using System.Threading.Tasks;

namespace RMuseum.Services
{
    /// <summary>
    /// translation service implementation
    /// </summary>
    public interface IGanjoorTranslationService
    {
        /// <summary>
        /// add language
        /// </summary>
        /// <param name="lang"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorLanguage>> AddLanguageAsync(GanjoorLanguage lang);


        /// <summary>
        /// update an existing language
        /// </summary>
        /// <param name="updated"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> UpdateLangaugeAsync(GanjoorLanguage updated);


        /// <summary>
        /// delete language
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeleteLangaugeAsync(int id);


        /// <summary>
        /// get langauge by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorLanguage>> GetLanguageAsync(int id);

        /// <summary>
        /// get all languages
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<GanjoorLanguage[]>> GetLanguagesAsync();

        /// <summary>
        /// add or update poem translation
        /// </summary>
        /// <param name="translation"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoemTranslationViewModel>> AddPoemTranslation(Guid userId, GanjoorPoemTranslationViewModel translation);

        /// <summary>
        /// get translation
        /// </summary>
        /// <param name="langId">-1 all languages</param>
        /// <param name="poemId"></param>
        /// <param name="onlyPublished"></param>
        /// <param name="includeUserInfo"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoemTranslationViewModel[]>> GetPoemTranslationsAsync(int langId, int poemId, bool onlyPublished, bool includeUserInfo);

        /// <summary>
        /// get last language the user contributed to its translation
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorLanguage>> GetLastUserContributedLanguage(Guid userId);

        /// <summary>
        /// get translation by id
        /// </summary>
        ///<param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoemTranslationViewModel>> GetPoemTranslationById(int id);

        /// <summary>
        /// get all poem translations (for export utility)
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="langId"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, GanjoorPoemTranslationViewModel[] Translations)>> GetAllPoemsTranslations(PagingParameterModel paging, int langId);


    }
}
