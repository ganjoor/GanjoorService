namespace RMuseum.Models.Accounting
{
    /// <summary>
    /// donation expenditures
    /// </summary>
    public class DonationExpenditure
    {
        /// <summary>
        /// Id
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// GanjoorDonation Id
        /// </summary>
        public int GanjoorDonationId { get; set; }

        /// <summary>
        /// GanjoorDonation
        /// </summary>
        public GanjoorDonation GanjoorDonation { get; set; }

        /// <summary>
        /// amount
        /// </summary>
        public decimal Amount { get; set; }
    }
}
