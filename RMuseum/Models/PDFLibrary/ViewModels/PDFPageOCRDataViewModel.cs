using System;

namespace RMuseum.Models.PDFLibrary.ViewModels
{
    /// <summary>
    /// PDF Page OCR Data View Model
    /// </summary>
    public class PDFPageOCRDataViewModel
    {
        /// <summary>
        ///  Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// full resolution image width
        /// </summary>
        public int FullResolutionImageWidth { get; set; }

        /// <summary>
        /// full resolution image height
        /// </summary>
        public int FullResolutionImageHeight { get; set; }

        /// <summary>
        /// ocred
        /// </summary>
        public bool OCRed { get; set; }

        /// <summary>
        /// page text
        /// </summary>
        public string PageText { get; set; }
    }
}
