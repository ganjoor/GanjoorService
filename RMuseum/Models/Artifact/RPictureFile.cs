using RSecurityBackend.Models.Image;
using System;
using static System.Windows.Forms.LinkLabel;


namespace RMuseum.Models.Artifact
{
    /// <summary>
    /// Artifact Picture Files
    /// </summary>
    public class RPictureFile : RImage
    {

        /// <summary>
        /// Title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Title in English
        /// </summary>
        public string TitleInEnglish { set; get; }

        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Description in English
        /// </summary>
        public string DescriptionInEnglish { get; set; }

        /// <summary>
        /// Publish status
        /// </summary>
        public PublishStatus Status { get; set; }

        /// <summary>
        /// order in the collection it belongs to it
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// نام فایل ذخیره شده با بالاترین کیفیت
        /// </summary>
        /// <example>
        /// e0d7050a-7e30-4a2f-b181-0daa4a1e1068.jpg
        /// </example>
        public string NormalSizeImageStoredFileName { get; set; }

        /// <summary>
        /// نام فایل ذخیره شده با بالاترین کیفیت
        /// </summary>
        /// <example>
        /// fbe9cc8e-12fa-4cb9-8f09-bbf353333383.jpg
        /// </example>
        public string ThumbnailImageStoredFileName { get; set; }

        /// <summary>
        /// عرض تصویر مناسب نمایش در صفحهٔ اختصاصی
        /// </summary>
        public int NormalSizeImageWidth { get; set; }

        /// <summary>
        /// طول تصویر مناسب نمایش در صفحهٔ اختصاصی
        /// </summary>
        public int NormalSizeImageHeight { get; set; }

        /// <summary>
        ///عرض تصویر با اندازه مناسب نمایش در صفحات لیستی
        /// </summary>
        public int ThumbnailImageWidth { get; set; }

        /// <summary>
        ///طول تصویر با اندازه مناسب نمایش در صفحات لیستی
        /// </summary>
        public int ThumbnailImageHeight { get; set; }

        /// <summary>
        /// source url
        /// </summary>
        public string SrcUrl { get; set; }

        /// <summary>
        /// Last Modified for caching purposes
        /// </summary>
        public DateTime LastModifiedMeta { get; set; }

        /// <summary>
        /// External Image Url Part (you should prefix it with the host url) sample output: folder1/thumb/0001.jpg
        /// </summary>
        /// <param name="size">
        /// thumb, norm, orig
        /// </param>
        private string GetExternalImageUrlPart(string size = "norm")
        {
            return $"{FolderName}/{size}/{OriginalFileName}";
        }

        /// <summary>
        /// Ganjoor Thumbnail Image Url
        /// </summary>
        public string GanjoorThumbnailImageUrl
        {
            get
            {
                return $"https://i.ganjoor.net/images/{GetExternalImageUrlPart("thumb")}";
            }
        }

        /// <summary>
        /// Ganjoor Normal Size Image Url
        /// </summary>
        public string GanjoorNormalSizeImageUrl
        {
            get
            {
                return $"https://i.ganjoor.net/images/{GetExternalImageUrlPart("norm")}";
            }
        }

        /// <summary>
        /// Ganjoor Original Size Image Url (Warning: high possibility of 404 error because of file deletion)
        /// </summary>
        public string GanjoorOriginalImageUrl
        {
            get
            {
                return $"https://i.ganjoor.net/images/{GetExternalImageUrlPart("orig")}";
            }
        }


        /// <summary>
        /// duplicated a picture record (Id is missing so you should store this to get a new Id)
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static RPictureFile Duplicate(RPictureFile src)
        {
            if (src == null)
                return null;
            return
                 new RPictureFile()
                 {
                     Title = src.Title,
                     TitleInEnglish = src.TitleInEnglish,
                     Description = src.Description,
                     DescriptionInEnglish = src.DescriptionInEnglish,
                     FolderName = src.FolderName,
                     DataTime = src.DataTime,
                     LastModified = src.LastModified,
                     LastModifiedMeta = src.LastModifiedMeta,
                     NormalSizeImageHeight = src.NormalSizeImageHeight,
                     NormalSizeImageStoredFileName = src.NormalSizeImageStoredFileName,
                     NormalSizeImageWidth = src.NormalSizeImageWidth,
                     Order = src.Order,
                     OriginalFileName = src.OriginalFileName,
                     ContentType = src.ContentType,
                     FileSizeInBytes = src.FileSizeInBytes,
                     ImageHeight = src.ImageHeight,
                     StoredFileName = src.StoredFileName,
                     ImageWidth = src.ImageWidth,
                     SrcUrl = src.SrcUrl,
                     Status = src.Status,
                     ThumbnailImageHeight = src.ThumbnailImageHeight,
                     ThumbnailImageStoredFileName = src.ThumbnailImageStoredFileName,
                     ThumbnailImageWidth = src.ThumbnailImageWidth
                 };
        }
        

    }
}
