using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Artifact;
using System.Linq;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    internal class TagHandler
    {
        public async static Task<RTagValue> PrepareAttribute(RMuseumDbContext db, string aName, string aValue, int order)
        {
            RTag type = await db.Tags.Where(a => a.Name == aName || a.NameInEnglish == aName).SingleOrDefaultAsync();
            if(type == null)
            {
                int maxOrder = await db.Tags.CountAsync() == 0 ? 0 : await db.Tags.MaxAsync(a => a.Order);
                type = new RTag()
                {
                    Name = aName,
                    NameInEnglish = aName,
                    PluralName = $"{aName}s",
                    PluralNameInEnglish = $"{aName}s",
                    Order = maxOrder + 1,
                    Status = PublishStatus.Published,
                    GlobalValue = true
                };
                await db.Tags.AddAsync(type);
                await db.SaveChangesAsync();
            }

            RTagValue tag =
                new RTagValue()
            {
                Order = order,
                Value = aValue,
                ValueInEnglish = aValue,
                ValueSupplement = "",
                RTag = type,
                Status = PublishStatus.Published
            };

            if (type.TagType == RTagType.Search || type.TagType == RTagType.LinkSearch)
            {
                RTagValue similar = await db.TagValues.Where(v => v.RTagId == type.Id && v.ValueInEnglish == aValue && !string.IsNullOrEmpty(v.FriendlyUrl)).FirstOrDefaultAsync();
                if(similar != null)
                {
                    tag.Value = similar.Value;
                    tag.FriendlyUrl = similar.FriendlyUrl;
                }
            }

            return tag;

        }
    }
}
