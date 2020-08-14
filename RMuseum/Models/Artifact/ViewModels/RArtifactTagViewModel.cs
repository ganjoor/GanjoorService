using System.Collections.Generic;

namespace RMuseum.Models.Artifact.ViewModels
{
    /// <summary>
    /// Artifact Tag (RTag with grouped related RTagValues of Artifact)
    /// </summary>
    public class RArtifactTagViewModel : RTag
    {
        public RArtifactTagViewModel(RTag src)
        {
            Id = src.Id;
            Order = src.Order;
            TagType = src.TagType;
            FriendlyUrl = src.FriendlyUrl;
            Status = src.Status;
            Name = src.Name;
            NameInEnglish = src.NameInEnglish;
            GlobalValue = src.GlobalValue;
            PluralName = src.PluralName;
            PluralNameInEnglish = src.PluralNameInEnglish;
        }

        public ICollection<RTagValue> Values { get; set; }
    }
}
