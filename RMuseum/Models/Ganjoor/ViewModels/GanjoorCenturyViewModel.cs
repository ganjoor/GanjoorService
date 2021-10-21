using System.Collections.Generic;

namespace RMuseum.Models.Ganjoor.ViewModels
{
    /// <summary>
    /// GanjoorCentury View Model
    /// </summary>
    public class GanjoorCenturyViewModel
    {
        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// order
        /// </summary>
        public int HalfCenturyOrder { get; set; }

        /// <summary>
        /// start
        /// </summary>
        public int StartYear { get; set; }

        /// <summary>
        /// end
        /// </summary>
        public int EndYear { get; set; }

        /// <summary>
        /// show in time line
        /// </summary>
        public bool ShowInTimeLine { get; set; }

        /// <summary>
        /// poets
        /// </summary>
        public List<GanjoorPoetViewModel> Poets { get; set; }
    }
}
