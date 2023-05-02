using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Generic;
using System;
using System.IO;
using System.Threading.Tasks;

namespace RMuseum.Services
{
    /// <summary>
    /// poet photo suggestion service;
    /// </summary>
    public interface IPoetPhotoSuggestionService
    {
        /// <summary>
        /// return list of suggested photos for a poet
        /// </summary>
        /// <param name="poetId"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoetSuggestedPictureViewModel[]>> GetPoetSuggestedPhotosAsync(int poetId);

        /// <summary>
        /// returns a single suggested photo
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoetSuggestedPictureViewModel>> GetPoetSuggestedPhotoByIdAsync(int id);
        /// <summary>
        /// suggest a new photo for a poet
        /// </summary>
        /// <param name="poetId"></param>
        /// <param name="userId"></param>
        /// <param name="imageStream"></param>
        /// <param name="fileName"></param>
        /// <param name="title"></param>
        /// <param name="description"></param>
        /// <param name="srcUrl"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoetSuggestedPictureViewModel>> SuggestPhotoForPoet(int poetId, Guid userId, Stream imageStream, string fileName, string title, string description, string srcUrl);

        /// <summary>
        /// next unpublished suggested photo for poets
        /// </summary>
        /// <param name="skip"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoetSuggestedPictureViewModel>> GetNextUnmoderatedPoetSuggestedPhotoAsync(int skip);

        /// <summary>
        /// unpublished suggested photos count for poets
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<int>> GetNextUnmoderatedPoetSuggestedPhotosCountAsync();

        /// <summary>
        /// modify a suggested photo for poets
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> ModifyPoetSuggestedPhotoAsync(GanjoorPoetSuggestedPictureViewModel model);

        /// <summary>
        /// delete  a suggested photo for poets
        /// </summary>
        /// <param name="id"></param>
        /// <param name="deleteUserId"></param>
        /// <param name="rejectionCause"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> RejectPoetSuggestedPhotosAsync(int id, Guid deleteUserId, string rejectionCause);

        /// <summary>
        /// delete published suggested photo
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeletePoetSuggestedPhotoAsync(int id);


    }
}
