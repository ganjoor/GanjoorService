using System;
using System.Collections.Generic;

namespace RSecurityBackend.Models.ClientBilling.Db
{
    /// <summary>
    /// cached billing info
    /// </summary>
    public class RBillingData
    {
        /// <summary>
        /// code name
        /// </summary>
        public string CodeName { get; set; }

        /// <summary>
        /// db name
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// personal tenants have limitted unmodifyable features and are not listed in tenant management panel
        /// </summary>
        public bool Personal { get; set; }

        /// <summary>
        /// active
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// last update (use for caching purposes)
        /// </summary>
        public DateTime LastUpdate { get; set; }

        /// <summary>
        /// data is valid until this date and should be refreshed after that because of changes of user billing status
        /// </summary>
        public DateTime ValidUntil { get; set; }

        /// <summary>
        /// auto charge user for tenant validitiy expiration
        /// </summary>
        public bool AutoCharge { get; set; }

        /// <summary>
        /// charged features
        /// </summary>
        public ICollection<RChargedFeature> Features { get; set; }
    }
}
