using System;

namespace RMuseum.Models.Accounting.ViewModels
{
    /// <summary>
    /// update date and description view model (donation + expense limited update api)
    /// </summary>
    public class UpdateDateDescriptionViewModel
    {
        /// <summary>
        /// date
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// description
        /// </summary>
        public string Description { get; set; }
    }
}
