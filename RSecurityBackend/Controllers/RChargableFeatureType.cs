using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RBilling.Models.Tenant.Db
{
    /// <summary>
    /// Chargable Feature For Tenant Type
    /// </summary>
    public enum RChargableFeatureType
    {
        /// <summary>
        /// yes/no
        /// </summary>
        Boolean = 0,
        /// <summary>
        /// 0, ...
        /// </summary>
        RegularNumber = 1,
        /// <summary>
        /// 1.222 or large numbers
        /// </summary>
        FloatNumber
    }
}
