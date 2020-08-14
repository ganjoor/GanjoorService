using System;

namespace RMuseum.Models.Artifact
{
    /// <summary>
    /// Attribute Value
    /// </summary>
    public class RTagValue
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid Id { get; set; }

        public Guid RTagId { get; set; }

        public RTag RTag { get; set; }

        public int Order { get; set; }

        /// <summary>
        /// Friendly Url
        /// </summary>
        public string FriendlyUrl { get; set; }

        /// <summary>
        /// Publish Status
        /// </summary>
        public PublishStatus Status { get; set; }

        /// <summary>
        /// Value
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Values In English
        /// </summary>
        public string ValueInEnglish { get; set; }

        /// <summary>
        /// link or ....
        /// </summary>
        public string ValueSupplement { get; set; }
    }
}
