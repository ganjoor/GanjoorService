namespace RMuseum.Models.PDFLibrary
{
    /// <summary>
    /// OCR Queued Item
    /// </summary>
    public class OCRQueue
    {
        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// PDF Book Id
        /// </summary>
        public int PDFBookId { get; set; }
    }
}
