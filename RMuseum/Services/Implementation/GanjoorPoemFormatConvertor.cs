using RMuseum.Models.Ganjoor;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// poem format convertor
    /// </summary>
    public static class GanjoorPoemFormatConvertor
    {
        /// <summary>
        /// تبدیل به رشته
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string GetString(GanjoorPoemFormat? format)
        {
            if (format == null) return "";
            switch (format)
            {
                case GanjoorPoemFormat.Ghazal:
                    return "غزل";
                case GanjoorPoemFormat.Ghaside:
                    return "قصیده";
                case GanjoorPoemFormat.Masnavi:
                    return "مثنوی";
                case GanjoorPoemFormat.Ghete:
                    return "قطعه";
                case GanjoorPoemFormat.Robaee:
                    return "رباعی";
                case GanjoorPoemFormat.Dobeyti:
                    return "دوبیتی";
                case GanjoorPoemFormat.Generic:
                    return "غزل/قصیده/قطعه";
                case GanjoorPoemFormat.TarkibBand:
                    return "ترکیب بند";
                case GanjoorPoemFormat.Takbeyt:
                    return "تک بیت";
                case GanjoorPoemFormat.Nimaee:
                    return "نیمایی";
                case GanjoorPoemFormat.TarjeeBand:
                    return "ترجیع بند";
                case GanjoorPoemFormat.Mosammat3:
                    return "مسمط مثلث";
                case GanjoorPoemFormat.Mostazad:
                    return "مستزاد";
                case GanjoorPoemFormat.RobaeeMostazad:
                    return "رباعی مستزاد";
                case GanjoorPoemFormat.Mosammat4:
                    return "مسمط مربع";
                case GanjoorPoemFormat.Mosammat5:
                    return "مسمط مخمس";
                case GanjoorPoemFormat.Sepeed:
                    return "سپید";
                case GanjoorPoemFormat.New:
                    return "نیمایی یا سپید";
                case GanjoorPoemFormat.Mosammat6:
                    return "مسمط مسدس";
                case GanjoorPoemFormat.Mosammat8:
                    return "مسمط مثمن";
                case GanjoorPoemFormat.Mosammat:
                    return "مسمط";
                case GanjoorPoemFormat.ChaharPare:
                    return "چهارپاره";
                case GanjoorPoemFormat.MultiBand:
                    return "چند بندی";
                case GanjoorPoemFormat.BahreTavil:
                    return "بحر طویل";
            }
            return "";
        }
    }
}
