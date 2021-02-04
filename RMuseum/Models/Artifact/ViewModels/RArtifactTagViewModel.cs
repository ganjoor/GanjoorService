using System;
using System.Collections.Generic;

namespace RMuseum.Models.Artifact.ViewModels
{
    /// <summary>
    /// Artifact Tag (RTag with grouped related RTagValues of Artifact)
    /// </summary>
    public class RArtifactTagViewModel
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Order
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// tag type
        /// </summary>
        public RTagType TagType { get; set; }

        /// <summary>
        /// Friendly Url
        /// </summary>
        public string FriendlyUrl { get; set; }

        /// <summary>
        /// Publish Status
        /// </summary>
        public PublishStatus Status { get; set; }

        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Name In English
        /// </summary>
        public string NameInEnglish { get; set; }

        /// <summary>
        /// Plural Name
        /// </summary>
        public string PluralName { get; set; }

        /// <summary>
        /// Plural Name In English
        /// </summary>
        public string PluralNameInEnglish { get; set; }

        /// <summary>
        /// Value is normally changed globally 
        /// </summary>
        public bool GlobalValue { get; set; }

        ///Values
        public ICollection<RTagValue> Values { get; set; }
    }
}
