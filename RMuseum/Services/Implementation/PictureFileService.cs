using FluentFTP;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using RMuseum.DbContext;
using RMuseum.Models.Artifact;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Models.Image;
using RSecurityBackend.Services;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// manipulating picture files 
    /// </summary>
    public class PictureFileService : IPictureFileService
    {

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
        public async Task<RServiceResult<RPictureFile>> RecoverFromeFiles
            (
            string title, string description, int order, string srcurl, string orignalFilePath, string normalFilePath, string thumbFilePath, string originalFileNameForStreams, string imageFolderName
            )
        {
            RPictureFile pictureFile =
                new RPictureFile()
                {
                    Title = title,
                    TitleInEnglish = title,
                    Description = description,
                    DescriptionInEnglish = description,
                    Order = order,
                    SrcUrl = srcurl,
                    Status = PublishStatus.Draft,
                    DataTime = DateTime.Now,
                    LastModified = DateTime.Now,
                    LastModifiedMeta = DateTime.Now,
                    FolderName = string.IsNullOrEmpty(imageFolderName) ? DateTime.Now.ToString("yyyy-MM") : imageFolderName,
                    ContentType = "image/jpeg",
                    OriginalFileName = originalFileNameForStreams,
                    StoredFileName = orignalFilePath,
                    NormalSizeImageStoredFileName = normalFilePath,
                    ThumbnailImageStoredFileName = thumbFilePath,
                    FileSizeInBytes = (await File.ReadAllBytesAsync(orignalFilePath)).Length

                };

            using (Image img = Image.FromFile(orignalFilePath))
            {
                pictureFile.ImageWidth = img.Width;
                pictureFile.ImageHeight = img.Height;
            }

            using (Image img = Image.FromFile(normalFilePath))
            {
                pictureFile.NormalSizeImageWidth = img.Width;
                pictureFile.NormalSizeImageHeight = img.Height;
            }

            using (Image img = Image.FromFile(thumbFilePath))
            {
                pictureFile.ThumbnailImageWidth = img.Width;
                pictureFile.ThumbnailImageHeight = img.Height;
            }


            //here is a problem, this method could be called from a background service where _context is disposed, so I need to renew it
            /*using(RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
            {
                await context.PictureFiles.AddAsync(pictureFile.Result);
                await context.SaveChangesAsync();
            }*/
            return new RServiceResult<RPictureFile>(pictureFile);
        }

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
        public async Task<RServiceResult<RPictureFile>> Add
            (
            string title, string description, int order, IFormFile file, string srcurl, Stream stream, string originalFileNameForStreams, string imageFolderName
            )
        {
            RServiceResult<RPictureFile>
                pictureFile =
                await ProcessImage
                (
                    file,
                    new RPictureFile()
                    {
                        Title = title,
                        TitleInEnglish = title,
                        Description = description,
                        DescriptionInEnglish = description,
                        Order = order,
                        SrcUrl = srcurl,
                        Status = PublishStatus.Draft,
                        DataTime = DateTime.Now,
                        LastModified = DateTime.Now,
                        LastModifiedMeta = DateTime.Now,
                        FolderName = string.IsNullOrEmpty(imageFolderName) ? DateTime.Now.ToString("yyyy-MM") : imageFolderName

                    },
                    stream,
                    originalFileNameForStreams
                    );
            if (!string.IsNullOrEmpty(pictureFile.ExceptionString))
                return new RServiceResult<RPictureFile>(null, pictureFile.ExceptionString);

            //here is a problem, this method could be called from a background service where _context is disposed, so I need to renew it
            /*using(RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
            {
                await context.PictureFiles.AddAsync(pictureFile.Result);
                await context.SaveChangesAsync();
            }*/
            return new RServiceResult<RPictureFile>(pictureFile.Result);
        }

        /// <summary>
        /// returns image info
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RPictureFile>> GetImage(Guid id)
        {
            var cachKey = $"PictureFileService::GetImage::{id}";
            if (!ImageInfoCacheEnabled || !_memoryCache.TryGetValue(cachKey, out RPictureFile pictureFile))
            {
                pictureFile = await _context.PictureFiles.AsNoTracking()
                     .Where(p => p.Id == id)
                     .SingleOrDefaultAsync();
                if (ImageInfoCacheEnabled)
                {
                    _memoryCache.Set(cachKey, pictureFile);
                }
            }

            return new RServiceResult<RPictureFile>(
               pictureFile
                     );
        }

        /// <summary>
        /// Rotate Image in 90 deg. multiplicants: 90, 180 or 270
        /// </summary>
        /// <param name="id"></param>
        /// <param name="degIn90mul"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RPictureFile>> RotateImage(Guid id, int degIn90mul)
        {
            if (degIn90mul != 90 && degIn90mul != 180 && degIn90mul != 270)
            {
                return new RServiceResult<RPictureFile>(null, "degIn90mul could only be equal to these 3 values: 90, 180 and 270");
            }
            RPictureFile rPictureFile =
                await _context.PictureFiles
                     .Where(p => p.Id == id)
                     .SingleOrDefaultAsync();

            string origPath = GetImagePath(rPictureFile).Result;
            using (MemoryStream msRotated = new MemoryStream())
            {
                using (Image img = Image.FromFile(origPath))
                {
                    img.RotateFlip(degIn90mul == 90 ? RotateFlipType.Rotate90FlipNone : degIn90mul == 180 ? RotateFlipType.Rotate180FlipNone : RotateFlipType.Rotate270FlipNone);

                    ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);

                    Encoder myEncoder =
                        Encoder.Quality;
                    EncoderParameters jpegParameters = new EncoderParameters(1);

                    EncoderParameter qualityParameter = new EncoderParameter(myEncoder, 92L);
                    jpegParameters.Param[0] = qualityParameter;

                    img.Save(msRotated, jpgEncoder, jpegParameters);
                }

                File.Move(GetImagePath(rPictureFile, "thumb").Result, GetImagePath(rPictureFile, "thumb").Result + ".bak");
                File.Move(GetImagePath(rPictureFile, "norm").Result, GetImagePath(rPictureFile, "norm").Result + ".bak");
                File.Move(origPath, origPath + ".bak");

                RServiceResult<RPictureFile>
                   result =
                   await ProcessImage
                   (
                       null,
                       rPictureFile,
                       msRotated,
                       rPictureFile.OriginalFileName
                       );
                if (!string.IsNullOrEmpty(result.ExceptionString))
                    return new RServiceResult<RPictureFile>(null, result.ExceptionString);
                result.Result.LastModified = DateTime.Now;
                _context.PictureFiles.Update(result.Result);
                await _context.SaveChangesAsync();

                File.Delete(GetImagePath(rPictureFile, "thumb").Result + ".bak");
                File.Delete(GetImagePath(rPictureFile, "norm").Result + ".bak");
                File.Delete(origPath + ".bak");

                if (bool.Parse(Configuration.GetSection("ExternalFTPServer")["UploadEnabled"]))
                {
                    var ftpClient = new AsyncFtpClient
                    (
                        Configuration.GetSection("ExternalFTPServer")["Host"],
                        Configuration.GetSection("ExternalFTPServer")["Username"],
                        Configuration.GetSection("ExternalFTPServer")["Password"]
                    );
                    ftpClient.ValidateCertificate += FtpClient_ValidateCertificate;
                    await ftpClient.AutoConnect();
                    ftpClient.Config.RetryAttempts = 3;

                    foreach (var imageSizeString in new string[] { "orig", "norm", "thumb" })
                    {
                        var localFilePath = GetImagePath(rPictureFile, imageSizeString).Result;
                        var remoteFilePath = $"{Configuration.GetSection("ExternalFTPServer")["RootPath"]}/images/{rPictureFile.FolderName}/{imageSizeString}/{Path.GetFileName(localFilePath)}";
                        await ftpClient.UploadFile(localFilePath, remoteFilePath, createRemoteDir: true);
                    }

                    await ftpClient.Disconnect();
                }

                return result;
            }
        }

        private void FtpClient_ValidateCertificate(FluentFTP.Client.BaseClient.BaseFtpClient control, FtpSslValidationEventArgs e)
        {
            e.Accept = true;
        }




        /// <summary>
        /// find image encodder
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }


        /// <summary>
        /// returns image file stream
        /// </summary>
        /// <param name="image"></param>
        /// <param name="sz"></param>
        /// <returns></returns>        
        public RServiceResult<string> GetImagePath(RPictureFile image, string sz = "orig")
        {
            string fileName = sz == "thumb" ? image.ThumbnailImageStoredFileName : sz == "norm" ? image.NormalSizeImageStoredFileName : image.StoredFileName;
            if (string.IsNullOrEmpty(fileName))
                return new RServiceResult<string>(null);
            return new RServiceResult<string>(Path.Combine(ImageStoragePath, image.FolderName, fileName));
        }

        /// <summary>
        /// فرمت تصاویر بندانگشتی و ...
        /// </summary>
        /// <example>image/jpeg</example>
        protected string ResizedImageContentType { get { return $"{Configuration.GetSection("PictureFileService")["ResizedImageContentType"]}"; } }

        /// <summary>
        /// عرض تصویر با اندازه نرمال - جهت نمایش در صفحات
        /// </summary>
        protected int NormalImageMaxWidth { get { return int.Parse($"{Configuration.GetSection("PictureFileService")["NormalImageMaxWidth"]}"); } }

        /// <summary>
        /// /// طول تصویر با اندازه نرمال - جهت نمایش در صفحات
        /// </summary>
        protected int NormalImageMaxHeight { get { return int.Parse($"{Configuration.GetSection("PictureFileService")["NormalImageMaxHeight"]}"); } }

        /// <summary>
        /// عرض تصویر بندانگشتی
        /// </summary>
        protected int ThumbnailImageWidth { get { return int.Parse($"{Configuration.GetSection("PictureFileService")["ThumbnailImageWidth"]}"); } }

        /// <summary>
        /// طول تصویر بندانگشتی
        /// </summary>
        protected int ThumbnailImageMaxHeight { get { return int.Parse($"{Configuration.GetSection("PictureFileService")["ThumbnailMaxHeight"]}"); } }

        /// <summary>
        /// image info cache
        /// </summary>
        private bool ImageInfoCacheEnabled { get { return bool.Parse($"{Configuration.GetSection("PictureFileService")["ImageInfoCacheEnabled"]}"); } }


        /// <summary>
        /// Image Storage Path
        /// </summary>
        public string ImageStoragePath { get { return $"{Configuration.GetSection("PictureFileService")["StoragePath"]}"; } }

        private async Task<RServiceResult<RPictureFile>> ProcessImage(IFormFile uploadedImage, RPictureFile pictureFile, Stream stream, string originalFileNameForStreams)
        {
            if (uploadedImage == null && stream == null)
            {
                return new RServiceResult<RPictureFile>(null, "ProcessImage: uploadedImage == null && stream == null");
            }

            pictureFile.ContentType = uploadedImage == null ? "image/jpeg" : uploadedImage.ContentType;
            pictureFile.FileSizeInBytes = uploadedImage == null ? stream.Length : uploadedImage.Length;
            pictureFile.OriginalFileName = uploadedImage == null ? originalFileNameForStreams : uploadedImage.FileName;



            string fullDirStorePath = Path.Combine(ImageStoragePath, pictureFile.FolderName);
            string originalStorePath = Path.Combine(fullDirStorePath, "orig");
            string normalStorePath = Path.Combine(fullDirStorePath, "norm");
            string thumbStorePath = Path.Combine(fullDirStorePath, "thumb");


            foreach (string path in new string[]
            {
                fullDirStorePath,
                originalStorePath,
                normalStorePath,
                thumbStorePath
            })
            {
                if (!Directory.Exists(path))
                {
                    try
                    {
                        Directory.CreateDirectory(path);
                    }
                    catch
                    {
                        return new RServiceResult<RPictureFile>(null, $"ProcessImage: create dir failed {path}");
                    }
                }
            }




            string ext = uploadedImage == null ? ".jpg" : Path.GetExtension(uploadedImage.FileName);
            pictureFile.StoredFileName = Path.GetFileNameWithoutExtension(pictureFile.OriginalFileName) + ext;

            string originalFileStorePath = Path.Combine(originalStorePath, pictureFile.StoredFileName);
            while (File.Exists(originalFileStorePath))
            {
                pictureFile.StoredFileName = Path.GetFileNameWithoutExtension(pictureFile.OriginalFileName) + "-" + Guid.NewGuid().ToString() + ext;
                originalFileStorePath = Path.Combine(originalStorePath, pictureFile.StoredFileName);
            }
            pictureFile.StoredFileName = $@"orig\{pictureFile.StoredFileName}";
            using (FileStream fsMain = new FileStream(originalFileStorePath, FileMode.Create))
            {
                if (uploadedImage != null)
                    await uploadedImage.CopyToAsync(fsMain);
                else
                {
                    stream.Position = 0;
                    await stream.CopyToAsync(fsMain);
                }
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
                    if (img.Width <= NormalImageMaxWidth && img.Height <= NormalImageMaxHeight && ext.Equals(".jpg", StringComparison.InvariantCultureIgnoreCase))
                    {
                        pictureFile.NormalSizeImageStoredFileName = pictureFile.StoredFileName;
                        pictureFile.NormalSizeImageWidth = img.Width;
                        pictureFile.NormalSizeImageHeight = img.Height;
                    }
                    else
                    {
                        pictureFile.NormalSizeImageWidth = NormalImageMaxWidth;
                        pictureFile.NormalSizeImageHeight = NormalImageMaxWidth * pictureFile.ImageHeight / pictureFile.ImageWidth;

                        if (pictureFile.NormalSizeImageHeight > NormalImageMaxHeight)
                        {
                            pictureFile.NormalSizeImageHeight = NormalImageMaxHeight;
                            pictureFile.NormalSizeImageWidth = NormalImageMaxHeight * pictureFile.ImageWidth / pictureFile.ImageHeight;
                        }

                        //روی خود img تأثیر می گذارد
                        Image resized = new Bitmap(pictureFile.NormalSizeImageWidth, pictureFile.NormalSizeImageHeight);
                        using (Graphics gResized = Graphics.FromImage(resized))
                        {
                            gResized.DrawImage(img, 0, 0, pictureFile.NormalSizeImageWidth, pictureFile.NormalSizeImageHeight);
                        }
                        using (MemoryStream msNormal = new MemoryStream())
                        {
                            resized.Save(msNormal, ImageFormat.Jpeg);
                        }

                        pictureFile.NormalSizeImageStoredFileName = Path.GetFileNameWithoutExtension(pictureFile.OriginalFileName) + ".jpg";
                        string normalFileStorePath = Path.Combine(normalStorePath, pictureFile.NormalSizeImageStoredFileName);

                        while (File.Exists(normalFileStorePath))
                        {
                            pictureFile.NormalSizeImageStoredFileName = Path.GetFileNameWithoutExtension(pictureFile.OriginalFileName) + "-" + Guid.NewGuid().ToString() + ".jpg";
                            normalFileStorePath = Path.Combine(normalStorePath, pictureFile.NormalSizeImageStoredFileName);
                        }
                        pictureFile.NormalSizeImageStoredFileName = $@"norm\{pictureFile.NormalSizeImageStoredFileName}";

                        using (FileStream fsNormal = new FileStream(normalFileStorePath, FileMode.Create))
                        {
                            resized.Save(fsNormal, ImageFormat.Jpeg);
                        }

                    }

                    pictureFile.ThumbnailImageWidth = ThumbnailImageWidth;
                    pictureFile.ThumbnailImageHeight = ThumbnailImageWidth * pictureFile.ImageHeight / pictureFile.ImageWidth;

                    if (pictureFile.ThumbnailImageHeight > ThumbnailImageMaxHeight)
                    {
                        pictureFile.ThumbnailImageHeight = ThumbnailImageMaxHeight;
                        pictureFile.ThumbnailImageWidth = pictureFile.ThumbnailImageHeight * pictureFile.ImageWidth / pictureFile.ImageHeight;
                    }

                    //روی خود img تأثیر می گذارد
                    // به احتمال قوی داریم روی تصویر resize کار می‌کنیم
                    Image thumbnail = new Bitmap(pictureFile.ThumbnailImageWidth, pictureFile.ThumbnailImageHeight);
                    using (Graphics gThumbnail = Graphics.FromImage(thumbnail))
                    {
                        gThumbnail.DrawImage(img, 0, 0, pictureFile.ThumbnailImageWidth, pictureFile.ThumbnailImageHeight);
                    }
                    using (MemoryStream msThumbnail = new MemoryStream())
                    {
                        thumbnail.Save(msThumbnail, ImageFormat.Jpeg);
                    }

                    pictureFile.ThumbnailImageStoredFileName = Path.GetFileNameWithoutExtension(pictureFile.OriginalFileName) + ".jpg";
                    string thumbFileStorePath = Path.Combine(thumbStorePath, pictureFile.ThumbnailImageStoredFileName);

                    while (File.Exists(thumbFileStorePath))
                    {
                        pictureFile.ThumbnailImageStoredFileName = Path.GetFileNameWithoutExtension(pictureFile.OriginalFileName) + "-" + Guid.NewGuid().ToString() + ".jpg";
                        thumbFileStorePath = Path.Combine(thumbStorePath, pictureFile.ThumbnailImageStoredFileName);
                    }

                    pictureFile.ThumbnailImageStoredFileName = $@"thumb\{pictureFile.ThumbnailImageStoredFileName}";

                    using (FileStream fsThumb = new FileStream(thumbFileStorePath, FileMode.Create))
                    {
                        thumbnail.Save(fsThumb, ImageFormat.Jpeg);
                    }

                }
            }
            return new RServiceResult<RPictureFile>(pictureFile);
        }

        /// <summary>
        /// Generated Cropped Image Based On ThumbnailCoordinates For Notes
        /// </summary>
        /// <param name="id"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RImage>> GenerateCroppedImageBasedOnThumbnailCoordinates(Guid id, decimal left, decimal top, decimal width, decimal height)
        {
            try
            {
                RPictureFile rPictureFile =
                    await _context.PictureFiles
                         .Where(p => p.Id == id)
                         .SingleOrDefaultAsync();

                int adjustedImageWidth = (int)(width * rPictureFile.NormalSizeImageWidth / rPictureFile.ThumbnailImageWidth);
                if (adjustedImageWidth > rPictureFile.ThumbnailImageWidth)
                    adjustedImageWidth = rPictureFile.ThumbnailImageWidth;

                int adjustedImageHeight = (int)(height * adjustedImageWidth / width);
                int adjusttedLeft = (int)(left * adjustedImageWidth / width);
                int adjusttedTop = (int)(top * adjustedImageHeight / height);

                string normalImagePath = GetImagePath(rPictureFile, "norm").Result;
                using (Image targetImage = new Bitmap(adjustedImageWidth, adjustedImageHeight))
                {
                    using (Graphics g = Graphics.FromImage(targetImage))
                    {
                        using (Image img = Image.FromFile(normalImagePath))
                        {
                            g.DrawImage(img, new Rectangle(0, 0, adjustedImageWidth, adjustedImageHeight),
                                (int)(left * rPictureFile.NormalSizeImageWidth / rPictureFile.ThumbnailImageWidth),
                                (int)(top * rPictureFile.NormalSizeImageHeight / rPictureFile.ThumbnailImageHeight),
                                (int)(width * rPictureFile.NormalSizeImageWidth / rPictureFile.ThumbnailImageWidth),
                                (int)(height * rPictureFile.NormalSizeImageHeight / rPictureFile.ThumbnailImageHeight),
                                GraphicsUnit.Pixel,
                                new ImageAttributes()
                                );
                        }
                    }
                    using (MemoryStream ms = new MemoryStream())
                    {
                        targetImage.Save(ms, ImageFormat.Jpeg);
                        ms.Position = 0;
                        RServiceResult<RImage> res = await _simpleImageStorage.Add(null, ms, $"{Path.GetFileNameWithoutExtension(rPictureFile.OriginalFileName)}-cropped-{left}-{top}-{width}-{height}.jpg", "CroppedImages");
                        if (string.IsNullOrEmpty(res.ExceptionString))
                        {
                            RImage image = res.Result;
                            _context.GeneralImages.Add(image);
                            await _context.SaveChangesAsync();
                            if (bool.Parse(Configuration.GetSection("ExternalFTPServer")["UploadEnabled"]))
                            {
                                var ftpClient = new AsyncFtpClient
                                (
                                    Configuration.GetSection("ExternalFTPServer")["Host"],
                                    Configuration.GetSection("ExternalFTPServer")["Username"],
                                    Configuration.GetSection("ExternalFTPServer")["Password"]
                                );
                                ftpClient.ValidateCertificate += FtpClient_ValidateCertificate;
                                await ftpClient.AutoConnect();
                                ftpClient.Config.RetryAttempts = 3;

                                var localFilePath = _simpleImageStorage.GetImagePath(image).Result;
                                var remoteFilePath = $"{Configuration.GetSection("ExternalFTPServer")["RootPath"]}/images/CroppedImages/{Path.GetFileName(localFilePath)}";
                                await ftpClient.UploadFile(localFilePath, remoteFilePath, createRemoteDir: true);

                                await ftpClient.Disconnect();
                            }
                            return new RServiceResult<RImage>(image);
                        }
                        return res;
                    }
                }
            }
            catch (Exception exp)
            {
                return new RServiceResult<RImage>(null, exp.ToString());
            }
        }



        /// <summary>
        /// Database Contetxt
        /// </summary>
        protected readonly RMuseumDbContext _context;

        /// <summary>
        /// Configuration
        /// </summary>
        protected IConfiguration Configuration { get; }

        /// <summary>
        /// Simple Image Storage (not storing multiple image sizes) for creating small cropped images
        /// </summary>
        protected readonly IImageFileService _simpleImageStorage;

        /// <summary>
        /// memory cache
        /// </summary>
        private readonly IMemoryCache _memoryCache;


        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="context"></param>
        /// <param name="configuration"></param>
        /// <param name="simpleImageStorage"></param>
        /// <param name="memoryCache"></param>
        public PictureFileService(RMuseumDbContext context, IConfiguration configuration, IImageFileService simpleImageStorage, IMemoryCache memoryCache)
        {
            _context = context;
            Configuration = configuration;
            _simpleImageStorage = simpleImageStorage;
            _memoryCache = memoryCache;
        }
    }
}
