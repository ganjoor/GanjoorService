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
    }
}
