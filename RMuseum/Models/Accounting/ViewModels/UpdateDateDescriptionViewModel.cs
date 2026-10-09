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

        /// <summary>
        /// expenditure description (donations only; null = leave unchanged, empty = not spent yet).
        /// Used to mark credit donations (amount 0) as consumed. Ignored for expenses.
        /// </summary>
        public string ExpenditureDesc { get; set; }
    }
}
