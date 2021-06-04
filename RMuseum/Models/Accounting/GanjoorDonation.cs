using System;

namespace RMuseum.Models.Accounting
{
    /// <summary>
    /// Ganjoor Donation (based on donation records saved in old html format of https://ganjoor.net/donate/)
    /// </summary>
    public class GanjoorDonation
    {
        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// date: (donation dates have been collected in different formats, so instead of keeping their dates in a DateTime field I ought to use a string field)
        /// </summary>
        public string DateString { get; set; }

        /// <summary>
        /// record date
        /// </summary>
        public DateTime RecordDate { get; set; }

        /// <summary>
        /// donation amount (0 for old imported records)
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Amount Unit
        /// </summary>
        public string Unit { get; set; }

        /// <summary>
        /// amount string (avoiding to parse different formats of old html text)
        /// </summary>
        public string AmountString { get; set; }

        /// <summary>
        /// donor name
        /// </summary>
        public string DonorName { get; set; }

        /// <summary>
        /// donor link (unused)
        /// </summary>
        public string DonorLink { get; set; }

        /// <summary>
        /// remaining (it could be ignored later when our data has been normalized enough and be calculated using related data)
        /// </summary>
        public decimal Remaining { get; set; }

        /// <summary>
        /// expenditure desc
        /// </summary>
        public string ExpenditureDesc { get; set; }

        /// <summary>
        /// record is imported from old HTML text of donation page
        /// </summary>
        public bool ImportedRecord { get; set; }

    }
}
