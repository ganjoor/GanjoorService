using System;
using System.Collections.Generic;

namespace RMuseum.Models.Artifact
{
    public class RArtifactItemRecord
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// parent id
        /// </summary>
        public Guid RArtifactMasterRecordId { get; set; }

        /// <summary>
        /// Order in Collections
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Friendly Url
        /// </summary>
        public string FriendlyUrl { get; set; }

        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Name In English
        /// </summary>
        public string NameInEnglish { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Description In English
        /// </summary>
        public string DescriptionInEnglish { get; set; }

        /// <summary>
        /// Main Image
        /// </summary>
        public int CoverImageIndex { get; set; }

        /// <summary>
        /// Last Modified for caching purposes
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// All Images
        /// </summary>
        public ICollection<RPictureFile> Images { get; set; }

        /// <summary>
        /// Attributes
        /// </summary>
        public ICollection<RTagValue> Tags { get; set; }


    }
}
