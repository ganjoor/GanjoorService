namespace RMuseum.Models.Ganjoor.ViewModels
{
    public class GTaggedLanguage
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


        public static GTaggedLanguage[] Languages
        {
            get
            {
                return new GTaggedLanguage[]
                {
                    new GTaggedLanguage()
                    {
                        Code = "fa-IR",
                        Name = "فارسی",
                        Description = "فارسی"
                    },
                    new GTaggedLanguage()
                    {
                        Code = "ar",
                        Name = "عربی",
                        Description = "عربی (اشعار عربی سعدی، خاقانی و ...)"
                    },
                    new GTaggedLanguage()
                    {
                        Code = "azb",
                        Name = "ترکی",
                        Description = "ترکی (گزیده‌ای از اشعار استاد شهریار و ...)"
                    },
                    new GTaggedLanguage()
                    {
                        Code = "ckb",
                        Name = "کردی",
                        Description = "کردی (مولانا خالد نقشبندی)"
                    },
                    new GTaggedLanguage()
                    {
                        Code = "glk",
                        Name = "گیلکی",
                        Description = "گیلکی (قاسم انوار)"
                    }
                };
            }
        }
    }
}
