using System;

namespace RSecurityBackend.Models.Image
{
    /// <summary>
    /// Image Files
    /// </summary>
    public class RImage
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Original File Name
        /// </summary>
        public string OriginalFileName { get; set; }

        /// <summary>
        /// content type
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Original Image File Size In Byte
        /// </summary>
        public long FileSizeInBytes { get; set; }

        /// <summary>
        /// Original Image Width
        /// </summary>
        public int ImageWidth { get; set; }

        /// <summary>
        /// Original Image Height
        /// </summary>
        public int ImageHeight { get; set; }

        /// <summary>
        /// پوشه محل ذخیره تصویر
        /// </summary>
        /// <example>
        /// 2017-08
        /// </example>
        public string FolderName { get; set; }

        /// <summary>
        /// نام فایل ذخیره شده با بالاترین کیفیت
        /// </summary>
        /// <example>
        /// 85007253-09f2-4434-8b7e-1a23db2cd9c9.png
        /// </example>
        public string StoredFileName { get; set; }

        /// <summary>
        /// datetime
        /// </summary>
        public DateTime DataTime { get; set; }

        /// <summary>
        /// Last Modified for caching purposes
        /// </summary>
        public DateTime LastModified { get; set; }




    }
}
