using Microsoft.AspNetCore.Http;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Models.Image;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace RSecurityBackend.Services
{
    /// <summary>
    /// Image File Service
    /// </summary>
    public interface IImageFileService
    {
        /// <summary>
        /// add new picture
        /// </summary>
        /// <param name="file"></param>
        /// <param name="stream"></param>
        /// <param name="originalFileNameForStreams"></param>
        /// <param name="imageFolderName">pass empty if you want a generic date based folder</param>
        /// <returns></returns>
        Task<RServiceResult<RImage>> Add
            (
            IFormFile file, Stream stream, string originalFileNameForStreams, string imageFolderName
            );

        /// <summary>
        /// store added image
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        Task<RServiceResult<RImage>> Store(RImage image);


        /// <summary>
        /// returns image info
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<RImage>> GetImage(Guid id);

        /// <summary>
        /// returns image file stream
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>        
        RServiceResult<string> GetImagePath(RImage image);

        /// <summary>
        /// delete image from database and file system
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeleteImage(Guid id);


        /// <summary>
        /// Image Storage Path
        /// </summary>
        string ImageStoragePath { get; }

    }
}
