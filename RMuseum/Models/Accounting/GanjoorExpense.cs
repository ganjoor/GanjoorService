using System;
using System.Collections.Generic;

namespace RMuseum.Models.Accounting
{
    /// <summary>
    /// Ganjoor Expenses
    /// </summary>
    public class GanjoorExpense
    {
        /// <summary>
        /// Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// date
        /// </summary>
        public DateTime ExpenseDate { get; set; }

        /// <summary>
        /// amount
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// unit
        /// </summary>
        public string Unit { get; set; }

        /// <summary>
        /// description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// donation expenitures
        /// </summary>
        public ICollection<DonationExpenditure> DonationExpenditures { get; set; }
    }
}
