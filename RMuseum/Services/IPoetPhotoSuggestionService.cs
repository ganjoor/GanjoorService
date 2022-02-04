using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Generic;
using System;
using System.IO;
using System.Threading.Tasks;

namespace RMuseum.Services
{
    /// <summary>
    /// poet photo suggestion service
    /// </summary>
    public interface IPoetPhotoSuggestionService
    {
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
    }
}
