using System;

namespace RMuseum.Models.Accounting.ViewModels
{
    /// <summary>
    /// Ganjoor Donation View Model
    /// </summary>
    public class GanjoorDonationViewModel
    {
        /// <summary>
        /// id - null for POST api -
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// record date
        /// </summary>
        public DateTime RecordDate { get; set; }

        /// <summary>
        /// donation amount (0 for old imported records)
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// "تومان" is good, Amount Unit, if you send another Unit it would be consumed with EXPENSES of the same Unit, if it send EMPTY it wotld not be consumed!
        /// </summary>
        public string Unit { get; set; }

        /// <summary>
        /// donor name
        /// </summary>
        public string DonorName { get; set; }

        /// <summary>
        /// null for POST api -  date: (donation dates have been collected in different formats, so instead of keeping their dates in a DateTime field I ought to use a string field)
        /// </summary>
        public string DateString { get; set; }

        /// <summary>
        /// null for POST api -amount string (avoiding to parse different formats of old html text)
        /// </summary>
        public string AmountString { get; set; }

        /// <summary>
        /// null for POST api -remaining (it could be ignored later when our data has been normalized enough and be calculated using related data)
        /// </summary>
        public decimal Remaining { get; set; }

        /// <summary>
        /// null for POST api -expenditure desc
        /// </summary>
        public string ExpenditureDesc { get; set; }

        /// <summary>
        /// null for POST api - record is imported from old HTML text of donation page
        /// </summary>
        public bool ImportedRecord { get; set; }
    }
}
