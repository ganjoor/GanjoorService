using System;
using System.Security.Cryptography;
using System.IO;

namespace ganjoor
{
    /// <summary>
    /// اطلاعات فایل صوتی شعر
    /// </summary>
    public class PoemAudio
    {
        /// <summary>
        /// سازنده
        /// </summary>
        public PoemAudio()
        {
        }

        /// <summary>
        /// شناسۀ شعر
        /// </summary>
        public int PoemId
        {
            get;
            set;
        }

        /// <summary>
        /// شناسۀ فایل صوتی با شروع از 1 برای هر شعر
        /// </summary>
        public int Id
        {
            get;
            set;
        }

        /// <summary>
        /// مسیر فایل
        /// </summary>
        public string FilePath
        {
            get;
            set;
        }

        /// <summary>
        /// شرح
        /// </summary>
        public string Description
        {
            get;
            set;
        }

        /// <summary>
        /// آیا همگام شده
        /// </summary>
        public bool IsSynced
        {
            get
            {
                return SyncArray != null;
            }
        }


        /// <summary>
        /// ساختار اطلاعات همگام سازی
        /// </summary>
        public struct SyncInfo
        {
            /// <summary>
            /// ترتیب مصرع (-1 نشانگر آغاز فایل صوتی و -2 نشانگر پایان آن است)
            /// </summary>
            public int VerseOrder;
            /// <summary>
            /// زمان در فایل صوتی بر حسب میلی ثانیه
            /// </summary>
            public int AudioMiliseconds;
        }

        /// <summary>
        /// اطلاعات همگام سازی
        /// </summary>
        public SyncInfo[] SyncArray
        {
            get;
            set;
        }

        /// <summary>
        /// نشانی دریافت فایل صوتی
        /// </summary>
        public string DownloadUrl
        {
            get;
            set;
        }

        /// <summary>
        /// آیا لینک دریافت فایل صوتی مستقیم است
        /// </summary>
        public bool IsDirectlyDownloadable
        {
            get;
            set;
        }

        /// <summary>
        /// شناسۀ یکتای اطلاعات همگام سازی
        /// </summary>
        public Guid SyncGuid
        {
            get;
            set;
        }

        /// <summary>
        /// امضای یکتای فایل صوتی
        /// </summary>
        public string FileCheckSum
        {
            get;
            set;
        }

        /// <summary>
        /// محاسبه چک سام فایل
        /// </summary>
        /// <param name="filePath"></param>
        public static string ComputeCheckSum(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                try
                {
                    using (var stream = File.OpenRead(filePath))
                    {
                        return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
                    }
                }
                catch
                {
                    return ""; //problem with file
                }
            }
            
        }

        /// <summary>
        /// آیا اطلاعات همگام سازی آپلود شده؟
        /// </summary>
        public bool IsUploaded
        {
            get;
            set;
        }

        /// <summary>
        /// تبدیل به رشته
        /// </summary>
        /// <returns>شرح</returns>
        public override string ToString()
        {
            return this.Description;
        }


        ///<remarks>
        ///Warning: not always filled
        ///</remarks>
        public string PoetName
        {
            get;
            set;
        }

        /// <remarks>
        /// Warning: Rarely filled
        /// </remarks>
        public string PoemTitle
        {
            get;
            set;
        }




    }
}
