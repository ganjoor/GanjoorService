using System;
namespace RMuseum.Models.Ganjoor.ViewModels
{
    /// <summary>
    /// GanjoorPageSnapshot summary view model
    /// </summary>
    public class GanjoorPageSnapshotSummaryViewModel
    {
        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// record date
        /// </summary>
        public DateTime RecordDate { get; set; }

        /// <summary>
        /// a description of the modfication
        /// </summary>
        public string Note { get; set; }
    }
}
