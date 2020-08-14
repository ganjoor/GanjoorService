using System;

namespace RSecurityBackend.Models.Billing
{
    /// <summary>
    /// Chargable Feature Price Row
    /// </summary>
    public class RChargableFeaturePrice
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Minimum Amount For This Price To be applicable
        /// </summary>
        public decimal MinimumAmount { get; set; } = 0;

        /// <summary>
        /// base price: (Minimum Amount whole price)
        /// </summary>
        public decimal BasePrice { get; set; } = 0;

        /// <summary>
        /// unit price for additional items from (Amount-MinimumAmount)
        /// </summary>
        public decimal UnitPrice { get; set; } = 0;

        /// <summary>
        /// active
        /// </summary>
        public bool Active { get; set; } = true;

        /// <summary>
        /// is effective from which date/time (useful for maintaining histotical data)
        /// </summary>
        public DateTime EffectiveFrom { get; set; } = DateTime.Now.Date;
    }
}
