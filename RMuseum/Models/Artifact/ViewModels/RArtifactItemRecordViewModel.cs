using System;
using System.Collections.Generic;
using System.Linq;

namespace RMuseum.Models.Artifact.ViewModels
{
    /// <summary>
    /// RArtifactItemRecord with additional info
    /// </summary>
    public class RArtifactItemRecordViewModel
    {

        private RArtifactItemRecord _Item;

        /// <summary>
        /// main item info
        /// </summary>
        public RArtifactItemRecord Item
        {
            get
            {
                return _Item;
            }
            set
            {
                _Item = value;

                List<RArtifactTagViewModel> rItemTags = new List<RArtifactTagViewModel>();
                if (_Item != null && _Item.Tags != null)
                {
                    foreach (RTagValue tag in _Item.Tags)
                    {
                        RArtifactTagViewModel related = rItemTags.Where(t => t.Id == tag.RTagId).SingleOrDefault();
                        List<RTagValue> values = (related == null) ? new List<RTagValue>() : new List<RTagValue>(related.Values);
                        if (related == null && tag.RTag != null)
                        {
                            related = new RArtifactTagViewModel()
                            {
                                Id = tag.RTag.Id,
                                Order = tag.RTag.Order,
                                TagType = tag.RTag.TagType,
                                FriendlyUrl = tag.RTag.FriendlyUrl,
                                Status = tag.RTag.Status,
                                Name = tag.RTag.Name,
                                NameInEnglish = tag.RTag.NameInEnglish,
                                GlobalValue = tag.RTag.GlobalValue,
                                PluralName = tag.RTag.PluralName,
                                PluralNameInEnglish = tag.RTag.PluralNameInEnglish
                            };
                            rItemTags.Add(related);

                        }
                        
                        if (related != null)
                        {
                            values.Add(tag);
                            values.Sort((a, b) => a.Order - b.Order);
                            related.Values = values;
                        }
                        
                    }

                    rItemTags.Sort((a, b) => a.Order - b.Order);
                }

                FormattedTags = rItemTags;
            }
        }

        /// <summary>
        /// parent
        /// </summary>
        public string ParentFriendlyUrl { get; set; }

        /// <summary>
        /// parent name
        /// </summary>
        public string ParentName { get; set; }

        /// <summary>
        /// parent image
        /// </summary>
        public Guid ParentImageId { get; set; }

        /// <summary>
        /// parent image external url
        /// </summary>
        public string ParentExternalNormalSizeImageUrl { get; set; }

        /// <summary>
        /// parent item count
        /// </summary>
        public int ParentItemCount { get; set; }

        /// <summary>
        /// empty or null means this is last item
        /// </summary>
        public string NextItemFriendlyUrl { get; set; }

        /// <summary>
        /// next image
        /// </summary>
        public Guid? NextItemImageId { get; set; }

        /// <summary>
        /// next image external url
        /// </summary>
        public string NextItemExternalNormalSizeImageUrl { get; set; }

        /// <summary>
        /// empty or null means this is first item
        /// </summary>
        public string PreviousItemFriendlyUrl { get; set; }

        /// <summary>
        /// prev image
        /// </summary>
        public Guid? PrevItemImageId { get; set; }

        /// <summary>
        /// prev image external url
        /// </summary>
        public string PrevItemExternalNormalSizeImageUrl { get; set; }

        /// <summary>
        /// Formatted Tags
        /// </summary>
        public ICollection<RArtifactTagViewModel> FormattedTags { get; set; }

    }
}
