using RMuseum.Models.Artifact.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RMuseum.Models.Artifact
{
    /// <summary>
    /// Museum Master Item
    /// </summary>
    public class RArtifactMasterRecord
    {

        public RArtifactMasterRecord() { }
        public RArtifactMasterRecord(string name, string description)
        {
            Name = NameInEnglish = name;
            Description = DescriptionInEnglish = description;
        }

        /// <summary>
        /// Id
        /// </summary>
        public Guid Id { get; set; }

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
        /// Description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Description In English
        /// </summary>
        public string DescriptionInEnglish { get; set; }      
        

        /// <summary>
        /// Date/Time
        /// </summary>
        public DateTime DateTime { get; set; }

        /// <summary>
        /// Last Modified for caching purposes
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// Cover Item Index
        /// </summary>
        public int CoverItemIndex { get; set; }

        /// <summary>
        /// Cover Image
        /// </summary>
        public RPictureFile CoverImage { get; set; }

        /// <summary>
        /// Cover Image Id
        /// </summary>
        public Guid CoverImageId { get; set; }

        /// <summary>
        /// Parts of this item
        /// </summary>
        public ICollection<RArtifactItemRecord> Items { get; set; }

        /// <summary>
        /// Item Count (for lists and queries)
        /// </summary>
        public int ItemCount { get; set; }

        /// <summary>
        /// Attributes
        /// </summary>
        public ICollection<RTagValue> Tags { get; set; }

        /// <summary>
        /// to view model
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static RArtifactMasterRecordViewModel ToViewModel(RArtifactMasterRecord src)
        {
            RArtifactMasterRecordViewModel res = new RArtifactMasterRecordViewModel()
            {
                Id = src.Id,
                FriendlyUrl = src.FriendlyUrl,
                Status = src.Status,
                Name = src.Name,
                NameInEnglish = src.NameInEnglish,
                Description = src.Description,
                DescriptionInEnglish = src.DescriptionInEnglish,
                DateTime = src.DateTime,
                LastModified = src.LastModified,
                CoverItemIndex = src.CoverItemIndex,
                CoverImage = src.CoverImage,
                CoverImageId = src.CoverImageId,
                ItemCount = src.ItemCount,
                Tags = null
            };

            List<RArtifactItemRecord> items = new List<RArtifactItemRecord>();
            List<RTagSum> sums = new List<RTagSum>();
            List<RTitleInContents> contents = new List<RTitleInContents>();
            int orderOfContents = 0;
            if (src.Items != null)
                foreach (RArtifactItemRecord item in src.Items)
                {
                    if (item.Tags != null)
                    {
                        var OrderedTags = item.Tags.OrderBy(t => t.RTag.Order).ThenBy(t => t.Order);
                        foreach (RTagValue value in OrderedTags)
                        {
                            if (value.RTag != null)
                            {
                                switch (value.RTag.TagType)
                                {
                                    case RTagType.Binary:
                                        {
                                            RTagSum sum = sums.Where(s => s.TagFriendlyUrl == value.RTag.FriendlyUrl).SingleOrDefault();
                                            if (sum != null)
                                            {
                                                sum.ItemCount++;
                                            }
                                            else
                                            {
                                                sums.Add(new RTagSum()
                                                {
                                                    TagName = value.RTag.Name,
                                                    TagFriendlyUrl = value.RTag.FriendlyUrl,
                                                    ItemCount = 1
                                                });
                                            }
                                        }
                                        break;
                                    case RTagType.TitleInContents:
                                        {
                                            if (!int.TryParse(value.ValueSupplement, out int level))
                                            {
                                                level = 1;
                                            }
                                            if (contents.Where(c => c.Title == value.Value).SingleOrDefault() == null)
                                            {
                                                contents.Add
                                                    (
                                                    new RTitleInContents()
                                                    {
                                                        Title = value.Value,
                                                        Order = ++orderOfContents,
                                                        Level = level,
                                                        ItemFriendlyUrl = item.FriendlyUrl
                                                    }
                                                    );
                                            }
                                        }
                                        break;
                                }

                            }
                        }
                    }

                    item.Tags = null;
                    items.Add(item);
                }
            res.Items = items;
            res.RTagSums = sums;
            res.Contents = contents;

            List<RArtifactTagViewModel> rArtifactTags = new List<RArtifactTagViewModel>();
            if (src.Tags != null)
            {
                foreach (RTagValue tag in src.Tags)
                {
                    RArtifactTagViewModel related = rArtifactTags.Where(t => t.Id == tag.RTagId).SingleOrDefault();
                    List<RTagValue> values = (related == null) ? new List<RTagValue>() : new List<RTagValue>(related.Values);
                    if (related == null)
                    {
                        related =
                            new RArtifactTagViewModel()
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
                        rArtifactTags.Add(related);

                    }
                    values.Add(tag);
                    values.Sort((a, b) => a.Order - b.Order);
                    related.Values = values;
                }

                rArtifactTags.Sort((a, b) => a.Order - b.Order);
            }

            res.ArtifactTags = rArtifactTags;

            return res;

        }
    }
}
