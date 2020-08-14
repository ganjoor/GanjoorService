using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RMuseum.Models.Artifact.ViewModels
{
    /// <summary>
    /// Museum Master Item View Model
    /// </summary>
    public class RArtifactMasterRecordViewModel : RArtifactMasterRecord
    {
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="src"></param>
        public RArtifactMasterRecordViewModel(RArtifactMasterRecord src)
        {
            Id = src.Id;
            FriendlyUrl = src.FriendlyUrl;
            Status = src.Status;
            Name = src.Name;
            NameInEnglish = src.NameInEnglish;
            Description = src.Description;
            DescriptionInEnglish = src.DescriptionInEnglish;
            DateTime = src.DateTime;
            LastModified = src.LastModified;
            CoverItemIndex = src.CoverItemIndex;
            CoverImage = src.CoverImage;
            CoverImageId = src.CoverImageId;
            ItemCount = src.ItemCount;           
            Tags = null;

            List<RArtifactItemRecord> items = new List<RArtifactItemRecord>();
            List<RTagSum> sums = new List<RTagSum>();
            List<RTitleInContents> contents = new List<RTitleInContents>();
            int orderOfContents = 0;
            if(src.Items != null)
            foreach(RArtifactItemRecord item in src.Items)
            {
                if(item.Tags != null)
                {
                    var OrderedTags = item.Tags.OrderBy(t => t.RTag.Order).ThenBy(t => t.Order);
                    foreach (RTagValue value in OrderedTags)
                    {
                        if (value.RTag != null)
                        {
                            switch(value.RTag.TagType)
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
                                        if(!int.TryParse(value.ValueSupplement, out int level))
                                        {
                                            level = 1;
                                        }
                                        if(contents.Where(c => c.Title == value.Value).SingleOrDefault() == null)
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
            Items = items;
            RTagSums = sums;
            Contents = contents;

            List<RArtifactTagViewModel> rArtifactTags = new List<RArtifactTagViewModel>();
            if(src.Tags != null)
            {
                foreach (RTagValue tag in src.Tags)
                {
                    RArtifactTagViewModel related = rArtifactTags.Where(t => t.Id == tag.RTagId).SingleOrDefault();
                    List<RTagValue> values = (related == null) ? new List<RTagValue>() : new List<RTagValue>(related.Values);
                    if (related == null)
                    {
                        related = new RArtifactTagViewModel(tag.RTag);
                        rArtifactTags.Add(related);

                    }
                    values.Add(tag);
                    values.Sort((a, b) => a.Order - b.Order);
                    related.Values = values;
                }

                rArtifactTags.Sort((a, b) => a.Order - b.Order);
            }

            ArtifactTags = rArtifactTags;
        }

        /// <summary>
        /// Tags
        /// </summary>
        public ICollection<RArtifactTagViewModel> ArtifactTags { get; set; }

        /// <summary>
        /// Binary Tagged Items
        /// </summary>
        public ICollection<RTagSum> RTagSums { get; set; }

        /// <summary>
        /// Titles of Items in Contents
        /// </summary>
        public ICollection<RTitleInContents> Contents { get; set; }
    }
}
