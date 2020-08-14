namespace RSecurityBackend.Models.Billing.Ganjoor
{
    /// <summary>
    /// Ganjoor Chargable Features
    /// </summary>
    public static class ChargableFeatures
    {
        /// <summary>
        /// Setup
        /// </summary>
        public const string Setup = "setup";

        /// <summary>
        /// Active Users
        /// </summary>
        public const string ActiveUsers = "activeusers";

        /// <summary>
        /// GanjoorDoc View
        /// </summary>
        public const string GanjoorDocView = "ganjoordoc:view";

        /// <summary>
        /// GanjoorDoc Full
        /// </summary>
        public const string GanjoorDocFull = "ganjoordoc:full";

        /// <summary>
        /// GanjoorRun View
        /// </summary>
        public const string GanjoorRunView = "ganjoorrun:view";

        /// <summary>
        /// GanjoorRun Full
        /// </summary>
        public const string GanjoorRunFull = "ganjoorrun:full";

        /// <summary>
        /// Features
        /// </summary>
        public static RChargableFeature[] Features
        {
            get
            {
                return new RChargableFeature[]
                {
                    new RChargableFeature()
                    {
                        CodeName = Setup,
                        Name = "راه‌اندازی اولیه",
                        Description = "هزینهٔ اختصاص فضای ذخیره سازی (غیر قابل برگشت)",
                        Optional = false,
                        FeatureType = RChargableFeatureType.Boolean,
                        MinimumAmount = 1,
                        SuggestedAmount = 1,
                        HasMaximumAmount = true,
                        MaximumnAmount = 1,
                        Covering = "",
                        Refundable = false,
                        InitialSetupCost = true,
                        ConstantOverPeriod = true,
                        Prices = new RChargableFeaturePrice[]
                        {
                            new RChargableFeaturePrice()
                            {
                                MinimumAmount = 1,
                                BasePrice = 10000000,
                                UnitPrice = 0
                            }
                        }
                    },
                    new RChargableFeature()
                    {
                        CodeName = ActiveUsers,
                        Name = "حداکثر کاربران فعال",
                        Description = "حداکثر تعداد کاربران فعال در سازمان",
                        Optional = false,
                        FeatureType = RChargableFeatureType.RegularNumber,
                        MinimumAmount = 5,
                        SuggestedAmount = 5,
                        HasMaximumAmount = false,
                        Covering = "",
                        Refundable = true,
                        InitialSetupCost = false,
                        ConstantOverPeriod = false,
                        Prices = new RChargableFeaturePrice[]
                        {
                            new RChargableFeaturePrice()
                            {
                                MinimumAmount = 6,
                                BasePrice = 50,
                                UnitPrice = 50
                            },
                            new RChargableFeaturePrice()
                            {
                                MinimumAmount = 11,
                                BasePrice = (10 - 5) * 50 + 25,
                                UnitPrice = 25
                            },
                            new RChargableFeaturePrice()
                            {
                                MinimumAmount = 21,
                                BasePrice = ((10 - 5) * 50) + (20 - 10) * 25 + 10,
                                UnitPrice = 10
                            },
                        }
                    },
                    new RChargableFeature()
                    {
                        CodeName = GanjoorDocView,
                        Name = "ماژول مشاهده مستندات روالها",
                        Description = "در صورتی که تمایل دارید عناوین مربوطه در ناوبری ظاهر نشود این ماژول را از حالت انتخاب خارج کنید",
                        Optional = true,
                        FeatureType = RChargableFeatureType.Boolean,
                        MinimumAmount = 0,
                        SuggestedAmount = 0,
                        HasMaximumAmount = true,
                        MaximumnAmount = 1,
                        Covering = "",
                        Refundable = true,
                        InitialSetupCost = false,
                        ConstantOverPeriod = false,
                        Prices = new RChargableFeaturePrice[]
                        {                           
                        }
                    },                    
                    new RChargableFeature()
                    {
                        CodeName = GanjoorDocFull,
                        Name = "ماژول مستندسازی روالها",
                        Description = "دسترسی کامل به ماژول مستندسازی روالها",
                        Optional = true,
                        FeatureType = RChargableFeatureType.Boolean,
                        MinimumAmount = 0,
                        SuggestedAmount = 1,
                        HasMaximumAmount = true,
                        MaximumnAmount = 1,
                        Covering = GanjoorDocView,
                        Refundable = true,
                        InitialSetupCost = false,
                        ConstantOverPeriod = false,
                        Prices = new RChargableFeaturePrice[]
                        {
                            new RChargableFeaturePrice()
                            {
                                MinimumAmount = 1,
                                BasePrice = 10000,
                                UnitPrice = 0
                            },
                        }
                    },
                    new RChargableFeature()
                    {
                        CodeName = GanjoorRunView,
                        Name = "ماژول مشاهده اطلاعات اجرایی روالها (کارها)",
                        Description = "در صورتی که تمایل دارید عناوین مربوطه در ناوبری ظاهر نشود این ماژول را از حالت انتخاب خارج کنید",
                        Optional = true,
                        FeatureType = RChargableFeatureType.Boolean,
                        MinimumAmount = 0,
                        SuggestedAmount = 0,
                        HasMaximumAmount = true,
                        MaximumnAmount = 1,
                        Covering = "",
                        Refundable = true,
                        InitialSetupCost = false,
                        ConstantOverPeriod = false,
                        Prices = new RChargableFeaturePrice[]
                        {
                        }
                    },
                    new RChargableFeature()
                    {
                        CodeName = GanjoorRunFull,
                        Name = "ماژول اجرای روالها (کارها)",
                        Description = "دسترسی کامل به ماژوال اجرای روالها",
                        Optional = true,
                        FeatureType = RChargableFeatureType.Boolean,
                        MinimumAmount = 0,
                        SuggestedAmount = 1,
                        HasMaximumAmount = true,
                        MaximumnAmount = 1,
                        Refundable = true,
                        InitialSetupCost = false,
                        ConstantOverPeriod = false,
                        Covering = GanjoorRunView,
                        Prices = new RChargableFeaturePrice[]
                        {
                            new RChargableFeaturePrice()
                            {
                                MinimumAmount = 1,
                                BasePrice = 20000,
                                UnitPrice = 0
                            },
                        }
                    },

                };
            }
        }
    }
}
