using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RSecurityBackend.DbContext;
using RSecurityBackend.Models.Auth.Db;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Models.Image;

namespace RSecurityBackend.Services.Implementation
{
    /// <summary>
    /// Image File Service
    /// </summary>
    public class ImageFileServiceEF : IImageFileService
    {
        /// <summary>
        /// Add Image File
        /// </summary>
        /// <param name="file"></param>
        /// <param name="stream"></param>
        /// <param name="originalFileNameForStreams"></param>
        /// <param name="imageFolderName"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RImage>> Add(IFormFile file, Stream stream, string originalFileNameForStreams, string imageFolderName)
        {
            RServiceResult<RImage>
                pictureFile =
                await ProcessImage
                (
                    file,
                    new RImage()
                    {
                        DataTime = DateTime.Now,
                        LastModified = DateTime.Now,
                        FolderName = string.IsNullOrEmpty(imageFolderName) ? DateTime.Now.ToString("yyyy-MM") : imageFolderName
                    },
                    stream,
                    originalFileNameForStreams
                    );
            if (pictureFile == null)
                return new RServiceResult<RImage>(null, pictureFile.ExceptionString);

            return new RServiceResult<RImage>(pictureFile.Result);
        }

        /// <summary>
        /// store added image
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RImage>> Store(RImage image)
        {
            _context.GeneralImages.Add(image);
            await _context.SaveChangesAsync();

            return new RServiceResult<RImage>(image);
        }

        private async Task<RServiceResult<RImage>> ProcessImage(IFormFile uploadedImage, RImage pictureFile, Stream stream, string originalFileNameForStreams)
        {
            if (uploadedImage == null && stream == null)
            {
                return new RServiceResult<RImage>(null, "ProcessImage: uploadedImage == null && stream == null");
            }

            pictureFile.ContentType = uploadedImage == null ? "image/jpeg" : uploadedImage.ContentType;
            pictureFile.FileSizeInBytes = uploadedImage == null ? stream.Length : uploadedImage.Length;
            pictureFile.OriginalFileName = uploadedImage == null ? originalFileNameForStreams : uploadedImage.FileName;



            string fullDirStorePath = Path.Combine(ImageStoragePath, pictureFile.FolderName);


            if (!Directory.Exists(fullDirStorePath))
            {
                try
                {
                    Directory.CreateDirectory(fullDirStorePath);
                }
                catch
                {
                    return new RServiceResult<RImage>(null, $"ProcessImage: create dir failed {fullDirStorePath}");
                }
            }




            string ext = uploadedImage == null ? ".jpg" : Path.GetExtension(uploadedImage.FileName);
            pictureFile.StoredFileName = Path.GetFileNameWithoutExtension(pictureFile.OriginalFileName) + ext;

            string originalFileStorePath = Path.Combine(fullDirStorePath, pictureFile.StoredFileName);
            while (File.Exists(originalFileStorePath))
            {
                pictureFile.StoredFileName = Path.GetFileNameWithoutExtension(pictureFile.OriginalFileName) + "-" + Guid.NewGuid().ToString() + ext;
                originalFileStorePath = Path.Combine(fullDirStorePath, pictureFile.StoredFileName);
            }
            using (FileStream fsMain = new FileStream(originalFileStorePath, FileMode.Create))
            {
                if (uploadedImage != null)
                    await uploadedImage.CopyToAsync(fsMain);
                else
                    await stream.CopyToAsync(fsMain);
            }

            using (MemoryStream ms = new MemoryStream())
            {
                if (uploadedImage != null)
                    await uploadedImage.CopyToAsync(ms);
                else
                {
                    stream.Position = 0;
                    await stream.CopyToAsync(ms);
                }


                using (Image img = Bitmap.FromStream(ms))
                {
                    pictureFile.ImageWidth = img.Width;
                    pictureFile.ImageHeight = img.Height;
                }
            }
            return new RServiceResult<RImage>(pictureFile);
        }


        /// <summary>
        /// returns image info
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RImage>> GetImage(Guid id)
        {
            return new RServiceResult<RImage>(
                await _context.GeneralImages.AsNoTracking()
                     .Where(p => p.Id == id)
                     .SingleOrDefaultAsync()
                     );
        }

        /// <summary>
        /// delete image (from database and file system)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeleteImage(Guid id)
        {
            RServiceResult<RImage> img = await GetImage(id);
            if (!string.IsNullOrEmpty(img.ExceptionString))
                return new RServiceResult<bool>(false, img.ExceptionString);
            if (img.Result == null)
            {
                return new RServiceResult<bool>(false, "image not found");
            }
            File.Delete(GetImagePath(img.Result).Result);
            _context.GeneralImages.Remove(img.Result);
            await _context.SaveChangesAsync();
            return new RServiceResult<bool>(true);
        }


        /// <summary>
        /// Get Image Storage Path
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public RServiceResult<string> GetImagePath(RImage image)
        {
            return new RServiceResult<string>(Path.Combine(ImageStoragePath, image.FolderName, image.StoredFileName));
        }

        /// <summary>
        /// Image Storage Path
        /// </summary>
        public string ImageStoragePath { get { return $"{Configuration.GetSection("PictureFileService")["StoragePath"]}"; } }

        /// <summary>
        /// Database Contetxt
        /// </summary>
        protected readonly RSecurityDbContext<RAppUser, RAppRole, Guid> _context;

        /// <summary>
        /// Configuration
        /// </summary>
        protected IConfiguration Configuration { get; }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="context"></param>
        /// <param name="configuration"></param>
        public ImageFileServiceEF(RSecurityDbContext<RAppUser, RAppRole, Guid> context, IConfiguration configuration)
        {
            _context = context;
            Configuration = configuration;
        }

    }
}
