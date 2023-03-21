using System.Linq;

namespace RMuseum.Models.Ganjoor.ViewModels
{
    public class GTaggedLanguage1
    {
        /// <summary>
        /// code
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// description
        /// </summary>
        public string Description { get; set; }

        public static string LanguageNameFromCode(string code)
        {
            var lang = Languages.Where(l => l.Code == code).FirstOrDefault();
            if (lang != null)
            {
                return lang.Name;
            }
            return "فارسی";
        }
        public static GTaggedLanguage1[] Languages
        {
            get
            {
                return new GTaggedLanguage1[]
                {
                    new GTaggedLanguage1()
                    {
                        Code = "fa-IR",
                        Name = "فارسی",
                        Description = "فارسی"
                    },
                    new GTaggedLanguage1()
                    {
                        Code = "ar",
                        Name = "عربی",
                        Description = "عربی (اشعار عربی سعدی، خاقانی و ...)"
                    },
                    new GTaggedLanguage1()
                    {
                        Code = "azb",
                        Name = "ترکی",
                        Description = "ترکی (گزیده‌ای از اشعار استاد شهریار و ...)"
                    },
                    new GTaggedLanguage1()
                    {
                        Code = "ckb",
                        Name = "کردی",
                        Description = "کردی (مولانا خالد نقشبندی)"
                    },
                    new GTaggedLanguage1()
                    {
                        Code = "glk",
                        Name = "گیلکی",
                        Description = "گیلکی (قاسم انوار)"
                    },
                    new GTaggedLanguage1()
                    {
                        Code = "mzn",
                        Name = "مازندرانی",
                        Description = "مازندرانی (امیر پازواری)"
                    }
                };
            }
        }
    }
}
