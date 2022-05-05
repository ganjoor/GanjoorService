using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RMuseum.Utils;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services.Implementation;
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
        /// update related poems info (after metreId or rhyme for one of these poems changes)
        /// </summary>
        /// <param name="context"></param>
        /// <param name="metreId"></param>
        /// <param name="rhyme"></param>
        /// <returns></returns>
        private async Task<RServiceResult<bool>> _UpdateRelatedPoems(RMuseumDbContext context, int metreId, string rhyme)
        {
            try
            {
                var poemIds = await context.GanjoorPoemSections.AsNoTracking().Where(section => section.GanjoorMetreId == metreId && section.RhymeLetters == rhyme).Select(p => p.PoemId).ToListAsync();
                foreach (var id in poemIds)
                {
                    await _UpdatePoemRelatedPoemsInfoNoSaveChanges(context, id);
                }
                await context.SaveChangesAsync();
                return new RServiceResult<bool>(true);
            }
            catch(Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
            

        }
        private async Task _UpdatePoemRelatedPoemsInfoNoSaveChanges(RMuseumDbContext context, int poemId)
        {
            var poem = await context.GanjoorPoems.AsNoTracking().Where(p => p.Id == poemId).SingleAsync();
            var oldRelations = await context.GanjoorCachedRelatedPoems.Where(r => r.PoemId == poemId).ToListAsync();
            context.GanjoorCachedRelatedPoems.RemoveRange(oldRelations);

            var section = await context.GanjoorPoemSections.AsNoTracking().Where(
                section => section.PoemId == poemId 
                && section.SectionType != PoemSectionType.Couplet && section.SectionType != PoemSectionType.Band
                && section.GanjoorMetreId != null && !string.IsNullOrEmpty(section.RhymeLetters)
                ).OrderBy(section => section.VerseType).FirstOrDefaultAsync();

            if (section == null)
                return;

            int metreId = (int)section.GanjoorMetreId;
            string rhyme = section.RhymeLetters;


            var relatedSections = await context.GanjoorPoemSections.AsNoTracking().Include(section => section.Poem).Include(section => section.Poet)
                .Where(section =>
                        section.GanjoorMetreId == metreId
                        &&
                        section.RhymeLetters == rhyme
                        &&
                        section.PoemId != poemId
                        )
                .OrderBy(p => p.Poet.BirthYearInLHijri).ThenBy(p => p.PoetId).ToListAsync();

            List<GanjoorCachedRelatedPoem> ganjoorCachedRelatedPoems = new List<GanjoorCachedRelatedPoem>();
            int r = 0;
            int prePoetId = -1;
            foreach (var relatedSection in relatedSections)
            {
                if(prePoetId != relatedSection.PoetId)
                {
                    r++;

                    GanjoorCachedRelatedPoem newRelatedPoem = new GanjoorCachedRelatedPoem()
                    {
                        PoemId = poemId,
                        PoetId = (int)relatedSection.PoetId,
                        RelationOrder = r,
                        PoetName = relatedSection.Poet.Nickname,
                        PoetImageUrl = $"/api/ganjoor/poet/image{(await context.GanjoorCategories.Where(c => c.ParentId == null && c.PoetId == relatedSection.PoetId).AsNoTracking().SingleAsync()).FullUrl}.gif",
                        FullTitle = relatedSection.Poem.FullTitle,
                        FullUrl = relatedSection.Poem.FullUrl,
                        PoetMorePoemsLikeThisCount = 0,
                        HtmlExcerpt = GanjoorPoemTools.GetPoemHtmlExcerpt(relatedSection.HtmlText)
                    };

                    ganjoorCachedRelatedPoems.Add(newRelatedPoem);

                    prePoetId = (int)relatedSection.PoetId;
                }
                else
                {
                    ganjoorCachedRelatedPoems[ganjoorCachedRelatedPoems.Count - 1].PoetMorePoemsLikeThisCount++;
                }
            }
            if(ganjoorCachedRelatedPoems.Count > 0)
            {
                context.GanjoorCachedRelatedPoems.AddRange(ganjoorCachedRelatedPoems);
            }
        }

    }
}