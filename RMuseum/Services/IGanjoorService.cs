using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RMuseum.Models.GanjoorAudio.ViewModels;
using RMuseum.Models.GanjoorIntegration.ViewModels;
using RSecurityBackend.Models.Generic;
using System.Threading.Tasks;

namespace RMuseum.Services
{
    /// <summary>
    /// Ganjoor Poems Content Privider Service
    /// </summary>
    public interface IGanjoorService
    {
        /// <summary>
        /// Get List of poets
        /// </summary>
        /// <param name="websitePoets"></param>
        /// <param name="includeBio"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoetViewModel[]>> GetPoets(bool websitePoets, bool includeBio = true);

        /// <summary>
        /// get poet by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoetCompleteViewModel>> GetPoetById(int id);

        /// <summary>
        /// get poet by url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoetCompleteViewModel>> GetPoetByUrl(string url);

        /// <summary>
        /// get cat by id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="poems"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoetCompleteViewModel>> GetCatById(int id, bool poems = true);

        /// <summary>
        /// get cat by url
        /// </summary>
        /// <param name="url"></param>
        /// <param name="poems"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoetCompleteViewModel>> GetCatByUrl(string url, bool poems = true);


        /// <summary>
        /// Get Poem By Id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="catInfo"></param>
        /// <param name="catPoems"></param>
        /// <param name="rhymes"></param>
        /// <param name="recitations"></param>
        /// <param name="images"></param>
        /// <param name="songs"></param>
        /// <param name="comments"></param>
        /// <param name="verseDetails"></param>
        /// <param name="navigation"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoemCompleteViewModel>> GetPoemById(int id, bool catInfo = true, bool catPoems = false, bool rhymes = true, bool recitations = true, bool images = true, bool songs = true, bool comments = true, bool verseDetails = true, bool navigation = true);

        /// <summary>
        /// Get Poem By Url
        /// </summary>
        /// <param name="url"></param>
        /// <param name="catInfo"></param>
        /// <param name="catPoems"></param>
        /// <param name="rhymes"></param>
        /// <param name="recitations"></param>
        /// <param name="images"></param>
        /// <param name="songs"></param>
        /// <param name="comments"></param>
        /// <param name="verseDetails"></param>
        /// <param name="navigation"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoemCompleteViewModel>> GetPoemByUrl(string url, bool catInfo = true, bool catPoems = false, bool rhymes = true, bool recitations = true, bool images = true, bool songs = true, bool comments = true, bool verseDetails = true, bool navigation = true);

        /// <summary>
        /// get poem recitations (PlainText/HtmlText are intentionally empty)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<PublicRecitationViewModel[]>> GetPoemRecitations(int id);

        /// <summary>
        /// get poem images by id (some fields are intentionally field with blank or null),
        /// EntityImageId : the most important data field, image url is https://ganjgah.ir/api/images/thumb/{EntityImageId}.jpg or https://ganjgah.ir/api/images/norm/{EntityImageId}.jpg
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<PoemRelatedImage[]>> GetPoemImages(int id);

        /// <summary>
        /// get a random poem from hafez
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoemCompleteViewModel>> Faal();

        /// <summary>
        /// imports unimported poem data from a locally accessible ganjoor SqlLite database
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<bool>> ImportLocalSQLiteDb();

        /// <summary>
        /// updates poems text
        /// </summary>
        /// <returns></returns>
        RServiceResult<bool> UpdatePoemsText();
    }
}
