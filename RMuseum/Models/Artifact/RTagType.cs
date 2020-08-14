namespace RMuseum.Models.Artifact
{
    /// <summary>
    /// نوع ویژگی
    /// </summary>
    public enum RTagType
    {
        /// <summary>
        /// عادی
        /// </summary>
        Ordinary = 0,
        /// <summary>
        /// پیوند
        /// </summary>
        Link = 1,
        /// <summary>
        /// جستجو
        /// </summary>
        Search = 2,
        /// <summary>
        /// پیوند و جستجو
        /// </summary>
        LinkSearch = 3,
        /// <summary>
        /// متن چپ به راست
        /// </summary>
        LeftToRightText = 4,
        /// <summary>
        /// بدون مقدار
        /// </summary>
        Binary = 5,
        /// <summary>
        /// عنوان در فهرست
        /// </summary>
        TitleInContents = 6,
    }
}
