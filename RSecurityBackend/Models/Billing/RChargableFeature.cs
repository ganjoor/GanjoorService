using System;
using System.Collections.Generic;

namespace RSecurityBackend.Models.Billing
{
    /// <summary>
    /// Chargable Feature For Tenant
    /// </summary>
    public class RChargableFeature
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
        /// Description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// if it is not optional it cannot be unselected (boolean) or it might be selected at least at minimum value
        /// </summary>
        public bool Optional { get; set; } = true;

        /// <summary>
        /// Feature Type
        /// </summary>
        public RChargableFeatureType FeatureType { get; set; } = RChargableFeatureType.Boolean;

        /// <summary>
        /// miniumum amount
        /// </summary>
        public decimal MinimumAmount { get; set; } = 0;

        /// <summary>
        /// suggested amount
        /// </summary>
        public decimal SuggestedAmount { get; set; } = 0;

        /// <summary>
        /// has maximum
        /// </summary>
        public bool HasMaximumAmount { get; set; } = true;

        /// <summary>
        /// maximum value
        /// </summary>
        public decimal MaximumnAmount { get; set; } = 1;

        /// <summary>
        /// if a module contains features for other modules added their codeNames here (comma separated)
        /// </summary>
        public string Covering { get; set; } = "";

        /// <summary>
        /// it must be priced according to period or it is a one time cost
        /// </summary>
        public bool ConstantOverPeriod { get; set; } = false;

        /// <summary>
        /// is it refundable
        /// </summary>
        public bool Refundable { get; set; } = true;

        /// <summary>
        /// is it setup cost
        /// </summary>
        public bool InitialSetupCost { get; set; } = false;

        /// <summary>
        /// is active
        /// </summary>
        public bool Active { get; set; } = true;

        /// <summary>
        /// prices
        /// </summary>
        public ICollection<RChargableFeaturePrice> Prices { get; set; }
    }
}
