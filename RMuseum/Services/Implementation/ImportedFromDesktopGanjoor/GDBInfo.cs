using System;
using System.ComponentModel;

namespace ganjoor
{
    public class GDBInfo
    {
        [DisplayName("سخنور/بخش")]
        public string CatName { get; set; }
        [DisplayName("شناسهٔ سخنور")]
        public int PoetID { get; set; }
        [DisplayName("شناسهٔ بخش")]
        public int CatID { get; set; }
        [DisplayName("نشانی دریافت")]
        public string DownloadUrl { get; set; }
        [DisplayName("نشانی توضیحات")]
        public string BlogUrl { get; set; }
        [DisplayName("پسوند")]
        public string FileExt { get; set; }
        [DisplayName("نشانی تصویر")]
        public string ImageUrl { get; set; }
        [DisplayName("اندازه")]
        public int FileSizeInByte { get; set; }
        [DisplayName("اولین شعر")]
        public int LowestPoemID { get; set; }
        [DisplayName("تاریخ انتشار")]
        public DateTime PubDate { get; set; }
    }
}
