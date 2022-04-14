using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RSecurityBackend.Models.Generic;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {
        /// <summary>
        /// get a section related sections
        /// </summary>
        /// <param name="poemId">poem id</param>
        /// <param name="sectionIndex">section index</param>
        /// <param name="skip"></param>
        /// <param name="itemsCount">if sent 0 or less returns all items</param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorCachedRelatedSection[]>> GetRelatedSections(int poemId, int sectionIndex, int skip, int itemsCount)
        {
            var source =
                 _context.GanjoorCachedRelatedSections
                         .Where(r => r.PoemId == poemId && r.SectionIndex == sectionIndex)
                         .OrderBy(r => r.RelationOrder);

            if (itemsCount <= 0)
                return new RServiceResult<GanjoorCachedRelatedSection[]>(await source.ToArrayAsync());
            return new RServiceResult<GanjoorCachedRelatedSection[]>
                (
                await source.Skip(skip).Take(itemsCount).ToArrayAsync()
                );
        }

        /// <summary>
        /// update related sections info (after metreId or rhyme for one of these sections changes)
        /// </summary>
        /// <param name="context"></param>
        /// <param name="metreId"></param>
        /// <param name="rhyme"></param>
        /// <returns></returns>
        private async Task<RServiceResult<bool>> _UpdateRelatedSections(RMuseumDbContext context, int metreId, string rhyme)
        {
            try
            {
                var sectionIds = await context.GanjoorPoemSections.AsNoTracking().Where(section => section.GanjoorMetreId == metreId && section.RhymeLetters == rhyme).Select(p => p.Id).ToListAsync();
                foreach (var id in sectionIds)
                {
                    await _UpdateSectionRelatedSectionsInfoNoSaveChanges(context, id);
                }
                await context.SaveChangesAsync();
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        private async Task _UpdateSectionRelatedSectionsInfoNoSaveChanges(RMuseumDbContext context, int sectionId)
        {
            var section = await context.GanjoorPoemSections.AsNoTracking().Where(p => p.Id == sectionId).SingleAsync();
            var oldRelations = await context.GanjoorCachedRelatedSections.Where(r => r.PoemId == section.PoemId && r.SectionIndex == section.Index).ToListAsync();
            context.GanjoorCachedRelatedSections.RemoveRange(oldRelations);

            int metreId = (int)section.GanjoorMetreId;
            string rhyme = section.RhymeLetters;


            var relatedSections = await context.GanjoorPoemSections.AsNoTracking().Include(section => section.Poem).Include(section => section.Poet)
                .Where(s =>
                        s.GanjoorMetreId == metreId
                        &&
                        s.RhymeLetters == rhyme
                        &&
                        ((s.PoemId == section.PoemId && s.Index !=  section.Index ) || (s.PoemId != section.PoemId))
                        )
                .OrderBy(p => p.Poet.BirthYearInLHijri).ThenBy(p => p.PoetId).ToListAsync();

            List<GanjoorCachedRelatedSection> GanjoorCachedRelatedSections = new List<GanjoorCachedRelatedSection>();
            int r = 0;
            int prePoetId = -1;
            foreach (var relatedSection in relatedSections)
            {
                if (prePoetId != relatedSection.PoetId)
                {
                    r++;

                    GanjoorCachedRelatedSection newRelatedPoem = new GanjoorCachedRelatedSection()
                    {
                        PoemId = relatedSection.PoemId,
                        SectionIndex = relatedSection.Index,
                        PoetId = (int)relatedSection.PoetId,
                        RelationOrder = r,
                        PoetName = relatedSection.Poet.Nickname,
                        PoetImageUrl = $"/api/ganjoor/poet/image{(await context.GanjoorCategories.Where(c => c.ParentId == null && c.PoetId == relatedSection.PoetId).AsNoTracking().SingleAsync()).FullUrl}.gif",
                        FullTitle = relatedSection.Poem.FullTitle,
                        FullUrl = relatedSection.Poem.FullUrl,
                        PoetMorePoemsLikeThisCount = 0,
                        HtmlExcerpt = GetPoemHtmlExcerpt(relatedSection.HtmlText)
                    };

                    GanjoorCachedRelatedSections.Add(newRelatedPoem);

                    prePoetId = (int)relatedSection.PoetId;
                }
                else
                {
                    GanjoorCachedRelatedSections[GanjoorCachedRelatedSections.Count - 1].PoetMorePoemsLikeThisCount++;
                }
            }
            if (GanjoorCachedRelatedSections.Count > 0)
            {
                context.GanjoorCachedRelatedSections.AddRange(GanjoorCachedRelatedSections);
            }
        }
    }
}