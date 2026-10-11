using Microsoft.EntityFrameworkCore;
using RMuseum.Models.Ganjoor;
using RSecurityBackend.Models.Generic;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using DNTPersianUtils.Core;
using RMuseum.Models.Ganjoor.ViewModels;
using RMuseum.DbContext;
using System.Collections.Generic;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {
        public async Task<RServiceResult<int>> _BreakPoemAsync(RMuseumDbContext context, int poemId, int vOrder, Guid userId, GanjoorPoemCompleteViewModel poem, GanjoorPage parentPage, string poemTitleStaticPart)
        {
            if (poem.Next == null)
            {
                return await _BreakLastPoemInItsCategoryAsync(context, poemId, vOrder, userId, poem, parentPage, poemTitleStaticPart);
            }
            var dbMainPoem = await context.GanjoorPoems.Include(p => p.GanjoorMetre).Where(p => p.Id == poemId).SingleOrDefaultAsync();

            if(dbMainPoem.Title.IndexOf(poemTitleStaticPart) != 0)
                return new RServiceResult<int>(-1, $"dbMainPoem.Title.IndexOf(poemTitleStaticPart) != 0");

            if (!int.TryParse(dbMainPoem.UrlSlug.Substring("sh".Length), out int mainPoemSlugNumber))
                return new RServiceResult<int>(-1, $"slug error for the last poem in the category: {dbMainPoem.UrlSlug}");


            var dbPage = await context.GanjoorPages.Where(p => p.Id == poemId).SingleOrDefaultAsync();

            var catPage = await context.GanjoorPages.AsNoTracking().Where(p => p.GanjoorPageType == GanjoorPageType.CatPage && p.CatId == dbMainPoem.CatId).SingleOrDefaultAsync();

            var poemList = await context.GanjoorPoems.AsNoTracking().Where(p => p.CatId == dbMainPoem.CatId && p.Id > dbMainPoem.Id).OrderBy(p => p.Id).ToListAsync();

            var lastPoemInCategory = poemList.Last();

            if (!int.TryParse(lastPoemInCategory.UrlSlug.Substring("sh".Length), out int newPoemSlugNumber))
                return new RServiceResult<int>(-1, $"slug error for the last poem in the category: {lastPoemInCategory.UrlSlug}");

            string nextPoemUrlSluf = $"sh{newPoemSlugNumber + 1}";

            var maxPoemId = await context.GanjoorPoems.MaxAsync(p => p.Id);
            if (await context.GanjoorPages.MaxAsync(p => p.Id) > maxPoemId)
                maxPoemId = await context.GanjoorPages.MaxAsync(p => p.Id);
            var nextPoemId = 1 + maxPoemId;

            string nextPoemTitle = $"{poemTitleStaticPart} {(newPoemSlugNumber + 1).ToPersianNumbers()}";

            GanjoorPoem dbNewPoem = new GanjoorPoem()
            {
                Id = nextPoemId,
                CatId = poem.Category.Cat.Id,
                Title = nextPoemTitle,
                UrlSlug = nextPoemUrlSluf,
                FullTitle = $"{parentPage.FullTitle} » {nextPoemTitle}",
                FullUrl = $"{parentPage.FullUrl}/{nextPoemUrlSluf}",
                SourceName = poem.SourceName,
                SourceUrlSlug = poem.SourceUrlSlug,
                Language = dbMainPoem.Language,
                MixedModeOrder = dbMainPoem.MixedModeOrder,
                Published = dbMainPoem.Published,
            };

            GanjoorPage dbPoemNewPage = new GanjoorPage()
            {
                Id = nextPoemId,
                GanjoorPageType = GanjoorPageType.PoemPage,
                Published = true,
                PageOrder = -1,
                Title = dbNewPoem.Title,
                FullTitle = dbNewPoem.FullTitle,
                UrlSlug = dbNewPoem.UrlSlug,
                FullUrl = dbNewPoem.FullUrl,
                HtmlText = dbNewPoem.HtmlText,
                PoetId = parentPage.PoetId,
                CatId = poem.Category.Cat.Id,
                PoemId = nextPoemId,
                PostDate = DateTime.Now,
                ParentId = parentPage.Id,
            };
            context.GanjoorPoems.Add(dbNewPoem);
            context.GanjoorPages.Add(dbPoemNewPage);
            await context.SaveChangesAsync();

            int targetPoemId = nextPoemId;

            //now copy each poem to its next sibling in their category
            for (int nPoemIndex = poemList.Count - 1; nPoemIndex >= 0; nPoemIndex--)
            {
                var sourcePoem = poemList[nPoemIndex];
                var targetPoem = await context.GanjoorPoems.Where(p => p.Id == targetPoemId).SingleAsync();

                var poemVerses = await context.GanjoorVerses.Where(v => v.PoemId == sourcePoem.Id).ToListAsync();
                for (int i = 0; i < poemVerses.Count; i++)
                {
                    poemVerses[i].PoemId = targetPoemId;
                }
                context.GanjoorVerses.UpdateRange(poemVerses);

                var poemSections = await context.GanjoorPoemSections.Where(s => s.PoemId == sourcePoem.Id).ToListAsync();
                for (int i = 0; i < poemSections.Count; i++)
                {
                    poemSections[i].PoemId = targetPoemId;
                }

                var recitaions = await context.Recitations.Where(r => r.GanjoorPostId == sourcePoem.Id).ToListAsync();
                for (int i = 0; i < recitaions.Count; i++)
                {
                    recitaions[i].GanjoorPostId = targetPoemId;
                }
                context.UpdateRange(recitaions);


                var tracks = await context.GanjoorPoemMusicTracks.Where(t => t.PoemId == sourcePoem.Id).ToListAsync();
                for (int i = 0; i < tracks.Count; i++)
                {
                    tracks[i].PoemId = targetPoemId;
                }
                context.UpdateRange(tracks);


                var comments = await context.GanjoorComments.Where(c => c.PoemId == sourcePoem.Id).ToListAsync();
                for (int i = 0; i < comments.Count; i++)
                {
                    comments[i].PoemId = targetPoemId;
                }
                context.UpdateRange(comments);


                var pageSnapshots = await context.GanjoorPageSnapshots.Where(c => c.GanjoorPageId == sourcePoem.Id).ToListAsync();
                for (int i = 0; i < pageSnapshots.Count; i++)
                {
                    pageSnapshots[i].GanjoorPageId = targetPoemId;
                }
                context.UpdateRange(pageSnapshots);


                var poemCorrections = await context.GanjoorPoemCorrections.Where(c => c.PoemId == sourcePoem.Id).ToListAsync();
                for (int i = 0; i < poemCorrections.Count; i++)
                {
                    poemCorrections[i].PoemId = targetPoemId;
                }
                context.UpdateRange(poemCorrections);


                var userBookmarks = await context.GanjoorUserBookmarks.Where(c => c.PoemId == sourcePoem.Id).ToListAsync();
                for (int i = 0; i < userBookmarks.Count; i++)
                {
                    userBookmarks[i].PoemId = targetPoemId;
                }
                context.UpdateRange(userBookmarks);


                var verseNumberings = await context.GanjoorVerseNumbers.Where(c => c.PoemId == sourcePoem.Id).ToListAsync();
                for (int i = 0; i < verseNumberings.Count; i++)
                {
                    verseNumberings[i].PoemId = targetPoemId;
                }
                context.UpdateRange(verseNumberings);

                var relatedSections = await context.GanjoorCachedRelatedSections.Where(c => c.PoemId == sourcePoem.Id).ToListAsync();
                for (int i = 0; i < relatedSections.Count; i++)
                {
                    relatedSections[i].PoemId = targetPoemId;
                }
                context.UpdateRange(relatedSections);

                var probables = await context.GanjoorPoemProbableMetres.Where(c => c.PoemId == sourcePoem.Id).ToListAsync();
                for (int i = 0; i < probables.Count; i++)
                {
                    probables[i].PoemId = targetPoemId;
                }
                context.UpdateRange(probables);


                var visits = await context.GanjoorUserPoemVisits.Where(c => c.PoemId == sourcePoem.Id).ToListAsync();
                for (int i = 0; i < visits.Count; i++)
                {
                    visits[i].PoemId = targetPoemId;
                }
                context.UpdateRange(visits);


                //the new title must be known before anything that caches it (links ...) is moved
                string title = sourcePoem.Title;
                char[] numbers = "0123456789۰۱۲۳۴۵۶۷۸۹".ToArray();
                int index = title.IndexOfAny(numbers);
                if (index != 0)
                {
                    while ((index + 1) < title.Length)
                    {
                        if (numbers.Contains(title[index + 1]))
                            index++;
                        else
                            break;
                    }
                    title = title[(index + 1)..].Trim();
                    if (title.IndexOf('-') == 0)
                    {
                        title = title[1..].Trim();
                    }
                }

                if (title.Length > 0)
                {
                    title = $"{poemTitleStaticPart} {(mainPoemSlugNumber + nPoemIndex + 2).ToPersianNumbers()} - {title}";
                }
                else
                {
                    title = $"{poemTitleStaticPart} {(mainPoemSlugNumber + nPoemIndex + 2).ToPersianNumbers()}";
                }

                targetPoem.Title = title;
                targetPoem.FullTitle = $"{catPage.FullTitle} » {title}";

                var links = await context.GanjoorLinks.Where(l => l.GanjoorPostId == sourcePoem.Id).ToListAsync();
                for (int i = 0; i < links.Count; i++)
                {
                    links[i].GanjoorPostId = targetPoemId;
                    links[i].GanjoorUrl = $"https://ganjoor.net{targetPoem.FullUrl}";
                    links[i].GanjoorTitle = targetPoem.FullTitle;
                }
                context.UpdateRange(links);


                var pinterests = await context.PinterestLinks.Where(l => l.GanjoorPostId == sourcePoem.Id).ToListAsync();
                for (int i = 0; i < pinterests.Count; i++)
                {
                    pinterests[i].GanjoorPostId = targetPoemId;
                    pinterests[i].GanjoorUrl = $"https://ganjoor.net{targetPoem.FullUrl}";
                    pinterests[i].GanjoorTitle = targetPoem.FullTitle;
                }
                context.UpdateRange(pinterests);

                await _MoveWholePoemAttachmentsAsync(context, sourcePoem.Id, targetPoemId, targetPoem);


                //copy everything but url
                targetPoem.HtmlText = sourcePoem.HtmlText;
                targetPoem.PlainText = sourcePoem.PlainText;
                targetPoem.SourceName = sourcePoem.SourceName;
                targetPoem.SourceUrlSlug = sourcePoem.SourceUrlSlug;
                targetPoem.OldTag = sourcePoem.OldTag;
                targetPoem.OldTagPageUrl = sourcePoem.OldTagPageUrl;
                targetPoem.MixedModeOrder = sourcePoem.MixedModeOrder;
                targetPoem.Published = sourcePoem.Published;
                targetPoem.Language = sourcePoem.Language;
                targetPoem.GanjoorMetreId = sourcePoem.GanjoorMetreId;
                targetPoem.RhymeLetters = sourcePoem.RhymeLetters;
                targetPoem.PoemSummary = sourcePoem.PoemSummary;
                targetPoem.ClaimedByMultiplePoets = sourcePoem.ClaimedByMultiplePoets;
                context.Update(targetPoem);

                var sourcePage = await context.GanjoorPages.AsNoTracking().Where(p => p.Id == sourcePoem.Id).SingleAsync();
                var targetPage = await context.GanjoorPages.Where(p => p.Id == targetPoemId).SingleAsync();
                //copy everything but url:
                targetPage.Title = targetPoem.Title;
                targetPage.FullTitle = targetPoem.FullTitle;
                targetPage.Published = sourcePage.Published;
                targetPage.PageOrder = sourcePage.PageOrder;
                targetPage.ParentId = sourcePage.ParentId;
                targetPage.PoetId = sourcePage.PoetId;
                targetPage.CatId = sourcePage.CatId;
                targetPage.PostDate = sourcePage.PostDate;
                targetPage.NoIndex = sourcePage.NoIndex;
                targetPage.HtmlText = sourcePage.HtmlText;
                context.Update(targetPage);

                targetPoemId = sourcePoem.Id;
            }

            await context.SaveChangesAsync();

            var dbLastTargetPoem = await context.GanjoorPoems.Where(p => p.Id == targetPoemId).SingleAsync();

            var targetPoemVerses = await context.GanjoorVerses.Where(v => v.PoemId == poemId && v.VOrder >= vOrder).OrderBy(v => v.VOrder).ToListAsync();
            var firstCoupletIndex = targetPoemVerses.Any() ? targetPoemVerses.First().CoupletIndex : 0;
            //everything bound to a couplet (PoemId + CoupletIndex) must follow the moved verses
            await _MoveCoupletAttachmentsAsync(context, poemId, targetPoemId, vOrder, firstCoupletIndex ?? 0, dbLastTargetPoem);
            for (int i = 0; i < targetPoemVerses.Count; i++)
            {
                targetPoemVerses[i].VOrder = i + 1;
                targetPoemVerses[i].PoemId = targetPoemId;
                targetPoemVerses[i].CoupletIndex -= firstCoupletIndex;
            }
            var mainPoemSections = await context.GanjoorPoemSections.Include(s => s.GanjoorMetre).Where(s => s.PoemId == poemId).OrderBy(s => s.SectionType).ThenBy(s => s.Index).ToListAsync();
            List<GanjoorPoemSection> targetPoemSections = new List<GanjoorPoemSection>();
            foreach (var section in mainPoemSections)
            {
                bool needsToBeDuplicated = false;
                switch (section.VerseType)
                {
                    case VersePoemSectionType.First:
                        if (targetPoemVerses.Any(v => v.SectionIndex1 == section.Index))
                        {
                            needsToBeDuplicated = true;
                        }
                        break;
                    case VersePoemSectionType.Second:
                        if (targetPoemVerses.Any(v => v.SectionIndex2 == section.Index))
                        {
                            needsToBeDuplicated = true;
                        }
                        break;
                    case VersePoemSectionType.Third:
                        if (targetPoemVerses.Any(v => v.SectionIndex3 == section.Index))
                        {
                            needsToBeDuplicated = true;
                        }
                        break;
                    default:
                        if (targetPoemVerses.Any(v => v.SectionIndex4 == section.Index))
                        {
                            needsToBeDuplicated = true;
                        }
                        break;
                }
                if (needsToBeDuplicated)
                {
                    var sectionCopy = new GanjoorPoemSection()
                    {
                        PoemId = dbLastTargetPoem.Id,
                        PoetId = section.PoetId,
                        SectionType = section.SectionType,
                        VerseType = section.VerseType,
                        Index = section.Index,
                        Number = section.Number,
                        GanjoorMetreId = section.GanjoorMetreId,
                        RhymeLetters = section.RhymeLetters,
                        PoemFormat = section.PoemFormat,
                    };
                    var sectionVerses = FilterSectionVerses(sectionCopy, targetPoemVerses);
                    sectionCopy.CoupletsCount = sectionVerses.Where(v => v.VersePosition == VersePosition.Left || v.VersePosition == VersePosition.CenteredVerse1).Count();
                    sectionCopy.HtmlText = PrepareHtmlText(sectionVerses);
                    sectionCopy.PlainText = PreparePlainText(sectionVerses);
                    try
                    {
                        var sectionRhymeLettersRes = LanguageUtils.FindRhyme(sectionVerses);
                        if (!string.IsNullOrEmpty(sectionRhymeLettersRes.Rhyme))
                        {
                            sectionCopy.RhymeLetters = sectionRhymeLettersRes.Rhyme;
                        }
                    }
                    catch
                    {

                    }
                    targetPoemSections.Add(sectionCopy);
                }
            }

            if (targetPoemSections.Count > 0)
            {
                context.AddRange(targetPoemSections);
            }

            dbLastTargetPoem.Title = $"{poemTitleStaticPart} {(mainPoemSlugNumber + 1).ToPersianNumbers()}";
            dbLastTargetPoem.FullTitle = $"{catPage.FullTitle} » {dbLastTargetPoem.Title}";
            dbLastTargetPoem.PlainText = PreparePlainText(targetPoemVerses);
            dbLastTargetPoem.HtmlText = PrepareHtmlText(targetPoemVerses);
            dbLastTargetPoem.SourceName = dbMainPoem.SourceName;
            dbLastTargetPoem.SourceUrlSlug = dbMainPoem.SourceUrlSlug;
            dbLastTargetPoem.OldTag = dbMainPoem.OldTag;
            dbLastTargetPoem.OldTagPageUrl = dbMainPoem.OldTagPageUrl;
            dbLastTargetPoem.MixedModeOrder = dbMainPoem.MixedModeOrder;
            dbLastTargetPoem.Published = dbMainPoem.Published;
            dbLastTargetPoem.Language = dbMainPoem.Language;
            dbLastTargetPoem.GanjoorMetreId = dbMainPoem.GanjoorMetreId;
            dbLastTargetPoem.RhymeLetters = dbMainPoem.RhymeLetters;
            dbLastTargetPoem.ClaimedByMultiplePoets = dbMainPoem.ClaimedByMultiplePoets;
            context.Update(dbLastTargetPoem);


            var dbLastTargetPage = await context.GanjoorPages.Where(p => p.Id == targetPoemId).SingleAsync();
            dbLastTargetPage.Title = dbLastTargetPoem.Title;
            dbLastTargetPage.FullTitle = dbLastTargetPoem.FullTitle;
            dbLastTargetPage.Published = dbPage.Published;
            dbLastTargetPage.PageOrder = dbPage.PageOrder;
            dbLastTargetPage.ParentId = dbPage.ParentId;
            dbLastTargetPage.PoetId = dbPage.PoetId;
            dbLastTargetPage.CatId = dbPage.CatId;
            dbLastTargetPage.PostDate = dbPage.PostDate;
            dbLastTargetPage.NoIndex = dbPage.NoIndex;
            dbLastTargetPage.HtmlText = dbLastTargetPoem.HtmlText;
            context.Update(dbLastTargetPage);


            context.GanjoorVerses.UpdateRange(targetPoemVerses);



            await context.SaveChangesAsync();

            var mainPoemVerses = await context.GanjoorVerses.Where(v => v.PoemId == poemId && v.VOrder < vOrder).OrderBy(v => v.VOrder).ToListAsync();

            dbMainPoem.HtmlText = PrepareHtmlText(mainPoemVerses);
            dbMainPoem.PlainText = PreparePlainText(mainPoemVerses);
            dbPage.HtmlText = dbMainPoem.HtmlText;
            context.Update(dbMainPoem);
            context.Update(dbPage);


            foreach (var section in mainPoemSections)
            {
                var sectionVerses = FilterSectionVerses(section, mainPoemVerses);
                if (sectionVerses.Count == 0)
                {
                    context.Remove(section);
                }
                else
                {
                    section.HtmlText = PrepareHtmlText(sectionVerses);
                    section.PlainText = PreparePlainText(sectionVerses);
                    try
                    {
                        var sectionRhymeLettersRes = LanguageUtils.FindRhyme(sectionVerses);
                        if (!string.IsNullOrEmpty(sectionRhymeLettersRes.Rhyme))
                        {
                            section.RhymeLetters = sectionRhymeLettersRes.Rhyme;
                        }
                    }
                    catch
                    {

                    }
                    context.Update(section);
                }
            }


            await context.SaveChangesAsync();

           


            return new RServiceResult<int>(targetPoemId);
        }


        private async Task<RServiceResult<int>> _BreakLastPoemInItsCategoryAsync(RMuseumDbContext context, int poemId, int vOrder, Guid userId, GanjoorPoemCompleteViewModel poem, GanjoorPage parentPage, string poemTitleStaticPart)
        {
            if (poem.UrlSlug.IndexOf("sh") != 0)
            {
                return new RServiceResult<int>(-1, "poem.UrlSlug.IndexOf(\"sh\") != 0");
            }

            var dbMainPoem = await context.GanjoorPoems.Include(p => p.GanjoorMetre).Where(p => p.Id == poemId).SingleOrDefaultAsync();
            var dbPage = await context.GanjoorPages.Where(p => p.Id == poemId).SingleOrDefaultAsync();



            if (!int.TryParse(poem.UrlSlug.Substring("sh".Length), out int slugNumber))
                return new RServiceResult<int>(-1, $"slug error: {poem.UrlSlug}");

            string nextPoemUrlSluf = $"sh{slugNumber + 1}";

            var maxPoemId = await context.GanjoorPoems.MaxAsync(p => p.Id);
            if (await context.GanjoorPages.MaxAsync(p => p.Id) > maxPoemId)
                maxPoemId = await context.GanjoorPages.MaxAsync(p => p.Id);
            var nextPoemId = 1 + maxPoemId;

            string nextPoemTitle = $"{poemTitleStaticPart} {(slugNumber + 1).ToPersianNumbers()}";

            GanjoorPoem dbLastTargetPoem = new GanjoorPoem()
            {
                Id = nextPoemId,
                CatId = poem.Category.Cat.Id,
                Title = nextPoemTitle,
                UrlSlug = nextPoemUrlSluf,
                FullTitle = $"{parentPage.FullTitle} » {nextPoemTitle}",
                FullUrl = $"{parentPage.FullUrl}/{nextPoemUrlSluf}",
                SourceName = poem.SourceName,
                SourceUrlSlug = poem.SourceUrlSlug,
                Language = dbMainPoem.Language,
                MixedModeOrder = dbMainPoem.MixedModeOrder,
                Published = dbMainPoem.Published,
                GanjoorMetreId = dbMainPoem.GanjoorMetreId,
                RhymeLetters = dbMainPoem.RhymeLetters,
                ClaimedByMultiplePoets = dbMainPoem.ClaimedByMultiplePoets,
            };

            var targetPoemVerses = await context.GanjoorVerses.Where(v => v.PoemId == poemId && v.VOrder >= vOrder).OrderBy(v => v.VOrder).ToListAsync();
            var firstCoupletIndex = targetPoemVerses.Any() ? targetPoemVerses.First().CoupletIndex : 0;
            //everything bound to a couplet (PoemId + CoupletIndex) must follow the moved verses
            await _MoveCoupletAttachmentsAsync(context, poemId, nextPoemId, vOrder, firstCoupletIndex ?? 0, dbLastTargetPoem);

            for (int i = 0; i < targetPoemVerses.Count; i++)
            {
                targetPoemVerses[i].VOrder = i + 1;
                targetPoemVerses[i].PoemId = nextPoemId;
                targetPoemVerses[i].CoupletIndex -= firstCoupletIndex;
            }
            var mainPoemSections = await context.GanjoorPoemSections.Where(s => s.PoemId == poemId).OrderBy(s => s.SectionType).ThenBy(s => s.Index).ToListAsync();
            List<GanjoorPoemSection> targetPoemSections = new List<GanjoorPoemSection>();
            foreach (var section in mainPoemSections)
            {
                bool needsToBeDuplicated = false;
                switch (section.VerseType)
                {
                    case VersePoemSectionType.First:
                        if (targetPoemVerses.Any(v => v.SectionIndex1 == section.Index))
                        {
                            needsToBeDuplicated = true;
                        }
                        break;
                    case VersePoemSectionType.Second:
                        if (targetPoemVerses.Any(v => v.SectionIndex2 == section.Index))
                        {
                            needsToBeDuplicated = true;
                        }
                        break;
                    case VersePoemSectionType.Third:
                        if (targetPoemVerses.Any(v => v.SectionIndex3 == section.Index))
                        {
                            needsToBeDuplicated = true;
                        }
                        break;
                    default:
                        if (targetPoemVerses.Any(v => v.SectionIndex4 == section.Index))
                        {
                            needsToBeDuplicated = true;
                        }
                        break;
                }
                if (needsToBeDuplicated)
                {
                    var sectionCopy = new GanjoorPoemSection()
                    {
                        PoemId = dbLastTargetPoem.Id,
                        PoetId = section.PoetId,
                        SectionType = section.SectionType,
                        VerseType = section.VerseType,
                        Index = section.Index,
                        Number = section.Number,
                        GanjoorMetreId = section.GanjoorMetreId,
                        RhymeLetters = section.RhymeLetters,
                        PoemFormat = section.PoemFormat,
                    };
                    var sectionVerses = FilterSectionVerses(sectionCopy, targetPoemVerses);
                    sectionCopy.CoupletsCount = sectionVerses.Where(v => v.VersePosition == VersePosition.Left || v.VersePosition == VersePosition.CenteredVerse1).Count();
                    sectionCopy.HtmlText = PrepareHtmlText(sectionVerses);
                    sectionCopy.PlainText = PreparePlainText(sectionVerses);
                    try
                    {
                        var sectionRhymeLettersRes = LanguageUtils.FindRhyme(sectionVerses);
                        if (!string.IsNullOrEmpty(sectionRhymeLettersRes.Rhyme))
                        {
                            sectionCopy.RhymeLetters = sectionRhymeLettersRes.Rhyme;
                        }
                    }
                    catch
                    {

                    }
                    targetPoemSections.Add(sectionCopy);
                }
            }

            if (targetPoemSections.Count > 0)
            {
                context.AddRange(targetPoemSections);
            }

            dbLastTargetPoem.PlainText = PreparePlainText(targetPoemVerses);
            dbLastTargetPoem.HtmlText = PrepareHtmlText(targetPoemVerses);


            context.GanjoorPoems.Add(dbLastTargetPoem);
            context.GanjoorVerses.UpdateRange(targetPoemVerses);

            GanjoorPage dbPoemNewPage = new GanjoorPage()
            {
                Id = nextPoemId,
                GanjoorPageType = GanjoorPageType.PoemPage,
                Published = true,
                PageOrder = -1,
                Title = dbLastTargetPoem.Title,
                FullTitle = dbLastTargetPoem.FullTitle,
                UrlSlug = dbLastTargetPoem.UrlSlug,
                FullUrl = dbLastTargetPoem.FullUrl,
                HtmlText = dbLastTargetPoem.HtmlText,
                PoetId = parentPage.PoetId,
                CatId = poem.Category.Cat.Id,
                PoemId = nextPoemId,
                PostDate = DateTime.Now,
                ParentId = parentPage.Id,
            };

            context.GanjoorPages.Add(dbPoemNewPage);
            await context.SaveChangesAsync();


            var mainPoemVerses = await context.GanjoorVerses.Where(v => v.PoemId == poemId && v.VOrder < vOrder).OrderBy(v => v.VOrder).ToListAsync();

            dbMainPoem.HtmlText = PrepareHtmlText(mainPoemVerses);
            dbMainPoem.PlainText = PreparePlainText(mainPoemVerses);
            dbPage.HtmlText = dbMainPoem.HtmlText;
            context.Update(dbMainPoem);
            context.Update(dbPage);

            foreach (var section in mainPoemSections)
            {
                var sectionVerses = FilterSectionVerses(section, mainPoemVerses);
                if (sectionVerses.Count == 0)
                {
                    context.Remove(section);
                }
                else
                {
                    section.HtmlText = PrepareHtmlText(sectionVerses);
                    section.PlainText = PreparePlainText(sectionVerses);
                    try
                    {
                        var sectionRhymeLettersRes = LanguageUtils.FindRhyme(sectionVerses);
                        if (!string.IsNullOrEmpty(sectionRhymeLettersRes.Rhyme))
                        {
                            section.RhymeLetters = sectionRhymeLettersRes.Rhyme;
                        }
                    }
                    catch
                    {

                    }
                    context.Update(section);
                }
            }

            //the changes above (main poem texts, trimmed/removed sections) were never saved
            await context.SaveChangesAsync();

            return new RServiceResult<int>(nextPoemId);

        }

        /// <summary>
        /// moves everything that is bound to a couplet of <paramref name="fromPoemId"/> and lies in the part
        /// that is being broken off (verses with VOrder >= <paramref name="vOrder"/>) to the new poem, fixing
        /// PoemId and CoupletIndex (the first moved couplet becomes couplet 0 of the new poem). Changes are
        /// only tracked here, the caller saves.
        /// </summary>
        private async Task _MoveCoupletAttachmentsAsync(RMuseumDbContext context, int fromPoemId, int toPoemId, int vOrder, int firstCoupletIndex, GanjoorPoem toPoem)
        {
            //if the break happens in the middle of a couplet, items of that couplet stay with the first part
            bool midCouplet = await context.GanjoorVerses.AsNoTracking().AnyAsync(v => v.PoemId == fromPoemId && v.VOrder < vOrder && v.CoupletIndex == firstCoupletIndex);
            int minIndex = midCouplet ? firstCoupletIndex + 1 : firstCoupletIndex;

            var geoTags = await context.PoemGeoDateTags.Where(t => t.PoemId == fromPoemId && t.CoupletIndex >= minIndex).ToListAsync();
            foreach (var tag in geoTags)
            {
                tag.PoemId = toPoemId;
                tag.CoupletIndex -= firstCoupletIndex;
            }

            var comments = await context.GanjoorComments.Where(c => c.PoemId == fromPoemId && c.CoupletIndex != null && c.CoupletIndex >= minIndex).ToListAsync();
            var movedCommentIds = new HashSet<int>(comments.Select(c => c.Id));
            foreach (var comment in comments)
            {
                comment.PoemId = toPoemId;
                comment.CoupletIndex -= firstCoupletIndex;
            }
            //replies stay in the same poem as the comment they reply to
            var frontier = movedCommentIds.ToList();
            while (frontier.Count > 0)
            {
                var replies = await context.GanjoorComments.Where(c => c.PoemId == fromPoemId && c.InReplyToId != null && frontier.Contains(c.InReplyToId.Value)).ToListAsync();
                frontier = new List<int>();
                foreach (var reply in replies)
                {
                    if (!movedCommentIds.Add(reply.Id))
                        continue;
                    reply.PoemId = toPoemId;
                    if (reply.CoupletIndex != null && reply.CoupletIndex >= minIndex)
                        reply.CoupletIndex -= firstCoupletIndex;
                    frontier.Add(reply.Id);
                }
            }
            if (movedCommentIds.Count > 0)
            {
                var movedIds = movedCommentIds.ToList();
                var reactions = await context.GanjoorCommentReactions.Where(r => movedIds.Contains(r.GanjoorCommentId)).ToListAsync();
                foreach (var reaction in reactions)
                {
                    reaction.PoemId = toPoemId;
                }
            }

            var bookmarks = await context.GanjoorUserBookmarks.Where(b => b.PoemId == fromPoemId && b.CoupletIndex >= minIndex).ToListAsync();
            foreach (var bookmark in bookmarks)
            {
                bookmark.PoemId = toPoemId;
                bookmark.CoupletIndex -= firstCoupletIndex;
            }

            var verseNumbers = await context.GanjoorVerseNumbers.Where(n => n.PoemId == fromPoemId && n.CoupletIndex >= minIndex).ToListAsync();
            foreach (var verseNumber in verseNumbers)
            {
                verseNumber.PoemId = toPoemId;
                verseNumber.CoupletIndex -= firstCoupletIndex;
            }

            //quotations: this poem can be either side of them
            var quoted = await context.GanjoorQuotedPoems.Where(q => q.PoemId == fromPoemId && q.CoupletIndex != null && q.CoupletIndex >= minIndex).ToListAsync();
            foreach (var q in quoted)
            {
                q.PoemId = toPoemId;
                q.CoupletIndex -= firstCoupletIndex;
            }
            var quotedRelated = await context.GanjoorQuotedPoems.Where(q => q.RelatedPoemId == fromPoemId && q.RelatedCoupletIndex != null && q.RelatedCoupletIndex >= minIndex).ToListAsync();
            foreach (var q in quotedRelated)
            {
                q.RelatedPoemId = toPoemId;
                q.RelatedCoupletIndex -= firstCoupletIndex;
                q.CachedRelatedPoemFullTitle = toPoem.FullTitle;
                q.CachedRelatedPoemFullUrl = toPoem.FullUrl;
            }

            //person relation / affiliation evidences quote a couplet
            var relationEvidences = await context.GanjoorPersonRelationEvidences.Where(e => e.PoemId == fromPoemId && e.CoupletIndex >= minIndex).ToListAsync();
            foreach (var evidence in relationEvidences)
            {
                evidence.PoemId = toPoemId;
                evidence.CoupletIndex -= firstCoupletIndex;
            }
            var affiliationEvidences = await context.GanjoorPersonAffiliationEvidences.Where(e => e.PoemId == fromPoemId && e.CoupletIndex >= minIndex).ToListAsync();
            foreach (var evidence in affiliationEvidences)
            {
                evidence.PoemId = toPoemId;
                evidence.CoupletIndex -= firstCoupletIndex;
            }
        }

        /// <summary>
        /// when a whole poem is shifted to another poem id: moves the rows referencing it which the caller
        /// does not handle (couplet indices do not change). Changes are only tracked here, the caller saves.
        /// </summary>
        private async Task _MoveWholePoemAttachmentsAsync(RMuseumDbContext context, int fromPoemId, int toPoemId, GanjoorPoem toPoem)
        {
            var geoTags = await context.PoemGeoDateTags.Where(t => t.PoemId == fromPoemId).ToListAsync();
            foreach (var tag in geoTags)
                tag.PoemId = toPoemId;

            var quoted = await context.GanjoorQuotedPoems.Where(q => q.PoemId == fromPoemId).ToListAsync();
            foreach (var q in quoted)
                q.PoemId = toPoemId;
            var quotedRelated = await context.GanjoorQuotedPoems.Where(q => q.RelatedPoemId == fromPoemId).ToListAsync();
            foreach (var q in quotedRelated)
            {
                q.RelatedPoemId = toPoemId;
                q.CachedRelatedPoemFullTitle = toPoem.FullTitle;
                q.CachedRelatedPoemFullUrl = toPoem.FullUrl;
            }

            var reactions = await context.GanjoorCommentReactions.Where(r => r.PoemId == fromPoemId).ToListAsync();
            foreach (var reaction in reactions)
                reaction.PoemId = toPoemId;

            var relationEvidences = await context.GanjoorPersonRelationEvidences.Where(e => e.PoemId == fromPoemId).ToListAsync();
            foreach (var evidence in relationEvidences)
                evidence.PoemId = toPoemId;
            var affiliationEvidences = await context.GanjoorPersonAffiliationEvidences.Where(e => e.PoemId == fromPoemId).ToListAsync();
            foreach (var evidence in affiliationEvidences)
                evidence.PoemId = toPoemId;

            var translations = await context.Set<GanjoorPoemTranslation>().Where(t => t.PoemId == fromPoemId).ToListAsync();
            foreach (var translation in translations)
                translation.PoemId = toPoemId;

            var cachedRelatedPoems = await context.GanjoorCachedRelatedPoems.Where(c => c.PoemId == fromPoemId).ToListAsync();
            foreach (var cached in cachedRelatedPoems)
                cached.PoemId = toPoemId;

            var pdfLinks = await context.PDFGanjoorLinks.Where(l => l.GanjoorPostId == fromPoemId).ToListAsync();
            foreach (var link in pdfLinks)
            {
                link.GanjoorPostId = toPoemId;
                link.GanjoorUrl = $"https://ganjoor.net{toPoem.FullUrl}";
                link.GanjoorTitle = toPoem.FullTitle;
            }

            var queueItems = await context.DiscoverQuotedQueueItems.Where(i => i.PoemId == fromPoemId).ToListAsync();
            foreach (var item in queueItems)
                item.PoemId = toPoemId;
            var queueRelated = await context.DiscoverQuotedQueueItems.Where(i => i.RelatedPoemId == fromPoemId).ToListAsync();
            foreach (var item in queueRelated)
                item.RelatedPoemId = toPoemId;
        }
    }
}