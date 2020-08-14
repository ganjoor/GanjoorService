using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RMuseum.Models.Artifact.ViewModels
{
    /// <summary>
    /// Tag Bundle View Model
    /// </summary>
    public class RTagBundleViewModel
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Friendly Url
        /// </summary>
        public string FriendlyUrl { get; set; }

        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Plural Name
        /// </summary>
        public string PluralName { get; set; }

        /// <summary>
        /// Values
        /// </summary>
        public RTagBundleValueViewModel[] Values { get; set; }
    }
}
