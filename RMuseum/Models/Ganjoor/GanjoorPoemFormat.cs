namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// poem format
    /// </summary>
    public enum GanjoorPoemFormat
    {
        /// <summary>
        /// unknown
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// غزل
        /// </summary>
        Ghazal = 1,

        /// <summary>
        /// قصیده
        /// </summary>
        Ghaside = 2,

        /// <summary>
        /// مثنوی
        /// </summary>
        Masnavi = 3,

        /// <summary>
        /// قطعه
        /// </summary>
        Ghete = 4,

        /// <summary>
        /// رباعی
        /// </summary>
        Robaee =  5,

        /// <summary>
        /// دوبیتی
        /// </summary>
        Dobeyti = 6,

        /// <summary>
        /// غزل/قصیده/قطعه
        /// </summary>
        Generic = 7,

        /// <summary>
        /// ترکیب بند
        /// </summary>
        TarkibBand = 8,

        /// <summary>
        /// تک بیت
        /// </summary>
        Takbeyt = 10,

        /// <summary>
        /// نیمایی
        /// </summary>
        Nimaee = 11,

        /// <summary>
        /// ترکیب بند
        /// </summary>
        TarjeeBand = 16,

        /// <summary>
        /// مسمط مثلث
        /// </summary>
        Mosammat3 = 32,

        /// <summary>
        /// مستزاد
        /// </summary>
        Mostazad = 40,

        /// <summary>
        /// رباعی مستزاد
        /// </summary>
        RobaeeMostazad = 45,

        /// <summary>
        /// مسمط مربع
        /// </summary>
        Mosammat4 = 64,

        /// <summary>
        /// مسمط مخمس
        /// </summary>
        Mosammat5 = 128,

        /// <summary>
        /// سپید
        /// </summary>
        Sepeed = 176,

        /// <summary>
        /// نیمایی یا سپید
        /// </summary>
        New = 187,

        /// <summary>
        /// مسمط مسدس
        /// </summary>
        Mosammat6 = 256,

        /// <summary>
        /// مسمط مثمن
        /// </summary>
        Mosammat8 = 512,

        /// <summary>
        /// مسمط
        /// </summary>
        Mosammat = 992,

        /// <summary>
        /// چهارپاره
        /// </summary>
        ChaharPare = 1024,

        /// <summary>
        /// چند بندی
        /// </summary>
        MultiBand = 2032,

        /// <summary>
        /// بحر طویل
        /// </summary>
        BahreTavil = 2048,

    }
}
