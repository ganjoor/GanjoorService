using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RMuseum.Models.Artifact;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Models.Image;
using System;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace RMuseum.Services
{
    /// <summary>
    /// manipulating picture files 
    /// </summary>
    public interface IPictureFileService
    {
        /// <summary>
        /// add new picture
        /// </summary>
        /// <param name="title"></param>
        /// <param name="description"></param>
        /// <param name="order"></param>
        /// <param name="file"></param>
        /// <param name="srcurl"></param>
        /// <param name="stream"></param>
        /// <param name="originalFileNameForStreams"></param>
        /// <param name="imageFolderName">pass empty if you want a generic date based folder</param>
        /// <returns></returns>
        Task<RServiceResult<RPictureFile>> Add
            (
            string title, string description, int order, IFormFile file, string srcurl, Stream stream, string originalFileNameForStreams, string imageFolderName
            );

        /// <summary>
        /// recover from files
        /// </summary>
        /// <param name="title"></param>
        /// <param name="description"></param>
        /// <param name="order"></param>
        /// <param name="srcurl"></param>
        /// <param name="orignalFilePath"></param>
        /// <param name="normalFilePath"></param>
        /// <param name="thumbFilePath"></param>
        /// <param name="originalFileNameForStreams"></param>
        /// <param name="imageFolderName"></param>
        /// <returns></returns>
        Task<RServiceResult<RPictureFile>> RecoverFromeFiles
            (
            string title, string description, int order, string srcurl, string orignalFilePath, string normalFilePath, string thumbFilePath, string originalFileNameForStreams, string imageFolderName
            );

        /// <summary>
        /// returns image info
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<RPictureFile>> GetImage(Guid id);

        /// <summary>
        /// returns image file stream
        /// </summary>
        /// <param name="image"></param>
        /// <param name="sz"></param>
        /// <returns></returns>        
        RServiceResult<string> GetImagePath(RPictureFile image, string sz = "orig");


        /// <summary>
        /// Rotate Image in 90 deg. multiplicants: 90, 180 or 270
        /// </summary>
        /// <param name="id"></param>
        /// <param name="degIn90mul"></param>
        /// <returns></returns>
        Task<RServiceResult<RPictureFile>> RotateImage(Guid id, int degIn90mul);

        /// <summary>
        /// Get Encoder
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        ImageCodecInfo GetEncoder(ImageFormat format);

        /// <summary>
        /// Generate Cropped Image Based On ThumbnailCoordinates For Notes
        /// </summary>
        /// <param name="id"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        Task<RServiceResult<RImage>> GenerateCroppedImageBasedOnThumbnailCoordinates(Guid id, decimal left, decimal top, decimal width, decimal height);


        /// <summary>
        /// Image Storage Path
        /// </summary>
        string ImageStoragePath { get; }
    }
}
