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
        /// get translations for a poem
        /// </summary>
        /// <param name="poemId"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoemTranslationViewModel[]>> GetTranslationsAsync(int poemId);

        /// <summary>
        /// get translation
        /// </summary>
        /// <param name="langId"></param>
        /// <param name="poemId"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoemTranslationViewModel>> GetTranslationAsync(int langId, int poemId);
    }
}
