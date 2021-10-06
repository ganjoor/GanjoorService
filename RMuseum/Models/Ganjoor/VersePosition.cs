namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// معنی فیلد position در جدول verse با توجه به مقادیر این ساختار داده مشخص می شود.
    /// </summary>
    /// <remarks>
    /// imported from Desktop Ganjoor source
    /// </remarks>
    public enum VersePosition
    {
        /// <summary>
        /// First Verse in a normal Beyt
        /// </summary>
        Right = 0,//مصرع اول
        /// <summary>
        /// Second Verse in a normal Beyt
        /// </summary>
        Left = 1,// مصرع دوم
        /// <summary>
        /// First Verse or the only one in a Band Beyt
        /// </summary>
        CenteredVerse1 = 2,// مصرع اول یا تنهای ابیات ترجیع یا ترکیب
        /// <summary>
        /// Second Verse in a Band Beyt
        /// </summary>
        CenteredVerse2 = 3,// مصرع دوم ابیات ترجیع یا ترکیب
        /// <summary>
        /// Free form verse
        /// </summary>
        Single = 4, //مصرعهای شعرهای نیمایی یا آزاد
        /// <summary>
        /// Comment 
        /// </summary>
        Comment = 5, //پاراگرافهایی که حالت توضیحی دارند
        /// <summary>
        /// Non-poem paragraph
        /// </summary>
        Paragraph = -1, //نثر
    }
}
