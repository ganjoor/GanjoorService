using System;


namespace RSecurityBackend.Models.ClientBilling.Db
{
    /// <summary>
    /// charged feature
    /// </summary>
    public class RChargedFeature
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Code Name
        /// </summary>
        public string CodeName { get; set; }

        /// <summary>
        /// Feature Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Amount
        /// </summary>
        public decimal Amount { get; set; }
    }
}
