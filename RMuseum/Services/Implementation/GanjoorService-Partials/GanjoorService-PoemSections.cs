using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services.Implementation;
using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using RSecurityBackend.Models.Generic.Db;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {
        /// <summary>
        /// sectionizing poems
        /// </summary>
        /// <returns></returns>
        public RServiceResult<bool> StartSectionizingPoems()
        {
            try
            {

                _backgroundTaskQueue.QueueBackgroundWorkItem
                           (
                           async token =>
                           {
                               using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                               {
                                   LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                                   var job = (await jobProgressServiceEF.NewJob("SectionizingPoems", "Query data")).Result;
                                   int progress = 0;
                                   try
                                   {
                                       var poems = await context.GanjoorPoems.Include(p => p.Cat).AsNoTracking().ToListAsync();
                                       int count = poems.Count;
                                       
                                       for (int i = 0; i < count; i++)
                                       {
                                           var poem = poems[i];

                                           if(false == await _SectionizePoem(context, poem, jobProgressServiceEF, job))
                                           {
                                               return;
                                           }

                                           if ((i * 100 / count) > progress)
                                           {
                                               progress = i * 100 / count;
                                               await jobProgressServiceEF.UpdateJob(job.Id, progress);
                                           }
                                       }
                                       await jobProgressServiceEF.UpdateJob(job.Id, 100, "", true);
                                   }
                                   catch (Exception exp)
                                   {
                                       await jobProgressServiceEF.UpdateJob(job.Id, progress, "", false, exp.ToString());
                                   }

                               }
                           });
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        private bool _IsMasnavi(List<GanjoorVerse> verses)
        {
            if (verses.Count % 2 != 0)
                return false;
            if (verses.Where(v => v.VersePosition != VersePosition.Right && v.VersePosition != VersePosition.Left).Any())
                return false;
            int rhymingCouplets = 0;
            for (int i = 0; i < verses.Count; i += 2)
            {
                var rightVerse = verses[i];
                if (rightVerse.VersePosition != VersePosition.Right)
                    return false;
                var leftVerse = verses[i + 1];
                if (leftVerse.VersePosition != VersePosition.Left)
                    return false;
                List<GanjoorVerse> coupletVerses = new List<GanjoorVerse>();
                coupletVerses.Add(rightVerse);
                coupletVerses.Add(leftVerse);
                var res = LanguageUtils.FindRhyme(coupletVerses);
                if (!string.IsNullOrEmpty(res.Rhyme))
                    rhymingCouplets++;
                if (rhymingCouplets * 200 / verses.Count > 50)
                    return true;
            }
            return false;
        }


        private async Task<bool> _SectionizePoem(RMuseumDbContext context, GanjoorPoem poem, LongRunningJobProgressServiceEF jobProgressServiceEF, RLongRunningJobStatus job)
        {
            if (true == await context.GanjoorPoemSections.AsNoTracking().Where(s => s.PoemId == poem.Id).AnyAsync())
                return true;

            var nonCommentVerses = await context.GanjoorVerses.Where(v => v.PoemId == poem.Id && v.VersePosition != VersePosition.Comment).OrderBy(v => v.VOrder).ToListAsync();
            if (!nonCommentVerses.Where(v => v.VersePosition == VersePosition.Paragraph || v.VersePosition == VersePosition.Single).Any())
            {
                if (nonCommentVerses.Where(v =>
                   v.VersePosition == VersePosition.CenteredVerse1
                   ||
                   v.VersePosition == VersePosition.CenteredVerse2
                  ).Any())
                {
                    //multi-band poem
                    _SectionizeMultibandVerses(context, nonCommentVerses, poem, 0);
                }
                else
                if (!nonCommentVerses.Where(v => v.VersePosition != VersePosition.Right && v.VersePosition != VersePosition.Left).Any())
                {
                    _SectionizeNormalVerses(context, nonCommentVerses, poem, 0);
                }
            }
            else
            {
                int sectionIndex = 0;
                int vIndex = 0;
                List<GanjoorVerse> singleVerses = new List<GanjoorVerse>();
                List<GanjoorVerse> normalVerses = new List<GanjoorVerse>();
                while (vIndex < nonCommentVerses.Count)
                {
                    if (nonCommentVerses[vIndex].VersePosition == VersePosition.Single)
                    {
                        singleVerses.Add(nonCommentVerses[vIndex]);
                        vIndex++;
                        continue;
                    }
                    if (singleVerses.Count > 0)
                    {
                        GanjoorPoemSection mainSection = new GanjoorPoemSection()
                        {
                            PoemId = poem.Id,
                            PoetId = poem.Cat.PoetId,
                            SectionType = PoemSectionType.WholePoem,
                            VerseType = VersePoemSectionType.First,
                            Index = sectionIndex,
                            Number = sectionIndex + 1,
                            GanjoorMetreId = poem.GanjoorMetreId,
                            RhymeLetters = poem.RhymeLetters,
                            HtmlText = PrepareHtmlText(singleVerses),
                            PlainText = PreparePlainText(singleVerses),
                            PoemFormat = GanjoorPoemFormat.New,
                        };
                        context.Add(mainSection);//having a main section for مثنوی inside normal text helps keep track of related versess
                        foreach (var verse in singleVerses)
                        {
                            verse.SectionIndex1 = mainSection.Index;
                            verse.SectionIndex2 = null;//clear previous indices
                            verse.SectionIndex3 = null;//clear previous indices
                            verse.SectionIndex4 = null;//clear previous indices
                        }
                        context.UpdateRange(singleVerses);

                        sectionIndex++;

                        singleVerses = new List<GanjoorVerse>();
                    }

                    if (nonCommentVerses[vIndex].VersePosition == VersePosition.Left || nonCommentVerses[vIndex].VersePosition == VersePosition.Right
                    ||
                    nonCommentVerses[vIndex].VersePosition == VersePosition.CenteredVerse1 || nonCommentVerses[vIndex].VersePosition == VersePosition.CenteredVerse2
                         )
                    {
                        normalVerses.Add(nonCommentVerses[vIndex]);
                        vIndex++;
                        continue;
                    }

                    if (normalVerses.Count > 0)
                    {
                        if (normalVerses.Where(v =>
                           v.VersePosition == VersePosition.CenteredVerse1
                           ||
                           v.VersePosition == VersePosition.CenteredVerse2
                          ).Any())
                        {
                            //multi-band poem
                            _SectionizeMultibandVerses(context, normalVerses, poem, sectionIndex);
                        }
                        else
                        {
                            _SectionizeNormalVerses(context, normalVerses, poem, sectionIndex);
                        }
                        sectionIndex++;
                        normalVerses = new List<GanjoorVerse>();
                    }

                    if (nonCommentVerses[vIndex].VersePosition != VersePosition.Paragraph)
                    {
                        if(jobProgressServiceEF != null)
                        {
                            await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, $"Poem: {poem.Id}, nonCommentVerses[{vIndex}].VersePosition != VersePosition.Paragraph");
                        }
                        return false;
                    }
                    vIndex++;
                }

                if (singleVerses.Count > 0)
                {
                    GanjoorPoemSection mainSection = new GanjoorPoemSection()
                    {
                        PoemId = poem.Id,
                        PoetId = poem.Cat.PoetId,
                        SectionType = PoemSectionType.WholePoem,
                        VerseType = VersePoemSectionType.First,
                        Index = sectionIndex,
                        Number = sectionIndex + 1,
                        GanjoorMetreId = poem.GanjoorMetreId,
                        RhymeLetters = poem.RhymeLetters,
                        HtmlText = PrepareHtmlText(singleVerses),
                        PlainText = PreparePlainText(singleVerses),
                        PoemFormat = GanjoorPoemFormat.New,
                    };
                    context.Add(mainSection);//having a main section for مثنوی inside normal text helps keep track of related versess
                    foreach (var verse in singleVerses)
                    {
                        verse.SectionIndex1 = mainSection.Index;
                        verse.SectionIndex2 = null;//clear previous indices
                        verse.SectionIndex3 = null;//clear previous indices
                        verse.SectionIndex4 = null;//clear previous indices
                    }
                    context.UpdateRange(singleVerses);
                }

                if (normalVerses.Count > 0)
                {
                    if (normalVerses.Where(v =>
                       v.VersePosition == VersePosition.CenteredVerse1
                       ||
                       v.VersePosition == VersePosition.CenteredVerse2
                      ).Any())
                    {
                        //multi-band poem
                        _SectionizeMultibandVerses(context, normalVerses, poem, sectionIndex);
                    }
                    else
                    {
                        _SectionizeNormalVerses(context, normalVerses, poem, sectionIndex);
                    }
                }
            }
            return true;
        }
        private void _SectionizeMultibandVerses(RMuseumDbContext context, List<GanjoorVerse> nonCommentVerses, GanjoorPoem poem, int index)
        {
            GanjoorPoemSection mainSection = new GanjoorPoemSection()
            {
                PoemId = poem.Id,
                PoetId = poem.Cat.PoetId,
                SectionType = PoemSectionType.WholePoem,
                VerseType = VersePoemSectionType.First,
                Index = index,
                Number = index + 1,
                GanjoorMetreId = poem.GanjoorMetreId,
                RhymeLetters = poem.RhymeLetters,
                HtmlText = PrepareHtmlText(nonCommentVerses),
                PlainText = PreparePlainText(nonCommentVerses),
                PoemFormat = GanjoorPoemFormat.MultiBand,
            };
            if (poem.FullTitle.Contains("ترجیع"))
                mainSection.PoemFormat = GanjoorPoemFormat.TarjeeBand;
            if (poem.FullTitle.Contains("ترکیب"))
                mainSection.PoemFormat = GanjoorPoemFormat.TarkibBand;
            if (poem.FullTitle.Contains("مسمط"))
                mainSection.PoemFormat = GanjoorPoemFormat.Mosammat;
            if (poem.FullTitle.Contains("مخمس"))
                mainSection.PoemFormat = GanjoorPoemFormat.Mosammat5;
            if (poem.FullTitle.Contains("مسدس"))
                mainSection.PoemFormat = GanjoorPoemFormat.Mosammat6;
            if (poem.FullTitle.Contains("چهارپاره"))
                mainSection.PoemFormat = GanjoorPoemFormat.ChaharPare;

            context.Add(mainSection);
            index++;
            List<GanjoorVerse> currentBandVerses = new List<GanjoorVerse>();
            GanjoorPoemSection currentBandSection = new GanjoorPoemSection()
            {
                PoemId = poem.Id,
                PoetId = poem.Cat.PoetId,
                SectionType = PoemSectionType.Band,
                VerseType = VersePoemSectionType.Second,
                Index = index,
                Number = index + 1,
                GanjoorMetreId = poem.GanjoorMetreId,
                GanjoorMetreRefSectionIndex = mainSection.Index,
            };
            List<GanjoorVerse> bandVerses = new List<GanjoorVerse>();
            foreach (var verse in nonCommentVerses)
            {
                verse.SectionIndex1 = mainSection.Index;
                verse.SectionIndex2 = null;//clear previous indices
                verse.SectionIndex3 = null;//clear previous indices
                verse.SectionIndex4 = null;//clear previous indices
                if (verse.VersePosition == VersePosition.Right || verse.VersePosition == VersePosition.Left)
                {
                    currentBandVerses.Add(verse);
                }
                if (verse.VersePosition == VersePosition.CenteredVerse1 || verse.VersePosition == VersePosition.CenteredVerse2)
                {
                    bandVerses.Add(verse);

                    if (currentBandVerses.Count > 0)
                    {
                        foreach (var currentBandVerse in currentBandVerses)
                        {
                            currentBandVerse.SectionIndex2 = currentBandSection.Index;
                        }
                        currentBandSection.RhymeLetters = LanguageUtils.FindRhyme(currentBandVerses).Rhyme;
                        currentBandSection.HtmlText = PrepareHtmlText(currentBandVerses);
                        currentBandSection.PlainText = PreparePlainText(currentBandVerses);
                        context.Add(currentBandSection);

                        index++;
                        currentBandVerses = new List<GanjoorVerse>();
                        currentBandSection = new GanjoorPoemSection()
                        {
                            PoemId = poem.Id,
                            PoetId = poem.Cat.PoetId,
                            SectionType = PoemSectionType.Band,
                            VerseType = VersePoemSectionType.Second,
                            Index = index,
                            Number = index + 1,
                            GanjoorMetreId = poem.GanjoorMetreId,
                            GanjoorMetreRefSectionIndex = mainSection.Index,
                        };
                    }
                }
            }

            if (currentBandVerses.Count > 0)
            {
                foreach (var currentBandVerse in currentBandVerses)
                {
                    currentBandVerse.SectionIndex2 = currentBandSection.Index;
                }
                currentBandSection.RhymeLetters = LanguageUtils.FindRhyme(currentBandVerses).Rhyme;
                currentBandSection.HtmlText = PrepareHtmlText(currentBandVerses);
                currentBandSection.PlainText = PreparePlainText(currentBandVerses);
                context.Add(currentBandSection);
            }
            index++;
            GanjoorPoemSection bandSection = new GanjoorPoemSection()
            {
                PoemId = poem.Id,
                PoetId = poem.Cat.PoetId,
                SectionType = PoemSectionType.BandCouplets,
                VerseType = VersePoemSectionType.Second,
                Index = index,
                Number = index + 1,
                GanjoorMetreId = poem.GanjoorMetreId,
                GanjoorMetreRefSectionIndex = mainSection.Index,
            };
            foreach (var bandVerse in bandVerses)
            {
                bandVerse.SectionIndex2 = bandSection.Index;
            }
            bandSection.RhymeLetters = LanguageUtils.FindRhyme(bandVerses).Rhyme;
            bandSection.HtmlText = PrepareHtmlText(bandVerses);
            bandSection.PlainText = PreparePlainText(bandVerses);
            context.Add(bandSection);
            context.UpdateRange(nonCommentVerses);
        }

        private void _SectionizeNormalVerses(RMuseumDbContext context, List<GanjoorVerse> nonCommentVerses, GanjoorPoem poem, int index)
        {
            //normal poem
            GanjoorPoemSection mainSection = new GanjoorPoemSection()
            {
                PoemId = poem.Id,
                PoetId = poem.Cat.PoetId,
                SectionType = PoemSectionType.WholePoem,
                VerseType = VersePoemSectionType.First,
                Index = index,
                Number = index + 1,
                GanjoorMetreId = poem.GanjoorMetreId,
                RhymeLetters = poem.RhymeLetters,
                HtmlText = PrepareHtmlText(nonCommentVerses),
                PlainText = PreparePlainText(nonCommentVerses),
                PoemFormat = GanjoorPoemFormat.Unknown,
            };
            //checking for مثنوی phase 1
            if (string.IsNullOrEmpty(poem.RhymeLetters))
            {
                mainSection.RhymeLetters = LanguageUtils.FindRhyme(nonCommentVerses).Rhyme;
                mainSection.PoemFormat = GanjoorPoemFormat.Generic;
                if (poem.FullTitle.Contains("رباعی"))
                    mainSection.PoemFormat = GanjoorPoemFormat.Robaee;
                if (poem.FullTitle.Contains("غزل"))
                    mainSection.PoemFormat = GanjoorPoemFormat.Ghazal;
                if (poem.FullTitle.Contains("قص"))
                    mainSection.PoemFormat = GanjoorPoemFormat.Ghaside;
                if (poem.FullTitle.Contains("قطع"))
                    mainSection.PoemFormat = GanjoorPoemFormat.Ghete;
            }
            context.Add(mainSection);//having a main section for مثنوی inside normal text helps keep track of related versess
            foreach (var verse in nonCommentVerses)
            {
                verse.SectionIndex1 = mainSection.Index;
                verse.SectionIndex2 = null;//clear previous indices
                verse.SectionIndex3 = null;//clear previous indices
                verse.SectionIndex4 = null;//clear previous indices
            }

            index++;
            //checking for مثنوی phase 2
            if (nonCommentVerses.Count > 2 && string.IsNullOrEmpty(mainSection.RhymeLetters))
            {
                if (_IsMasnavi(nonCommentVerses))
                {
                    mainSection.PoemFormat = GanjoorPoemFormat.Masnavi;
                    for (int v = 0; v < nonCommentVerses.Count; v += 2)
                    {
                        index++;
                        var rightVerse = nonCommentVerses[v];
                        var leftVerse = nonCommentVerses[v + 1];
                        List<GanjoorVerse> coupletVerses = new List<GanjoorVerse>();
                        coupletVerses.Add(rightVerse);
                        coupletVerses.Add(leftVerse);
                        var res = LanguageUtils.FindRhyme(coupletVerses);

                        GanjoorPoemSection verseSection = new GanjoorPoemSection()
                        {
                            PoemId = poem.Id,
                            PoetId = poem.Cat.PoetId,
                            SectionType = PoemSectionType.Couplet,
                            VerseType = VersePoemSectionType.Second,
                            Index = index,
                            Number = index,//couplet number
                            GanjoorMetreId = poem.GanjoorMetreId,
                            RhymeLetters = res.Rhyme,
                            GanjoorMetreRefSectionIndex = mainSection.Index,
                        };

                        rightVerse.SectionIndex2 = verseSection.Index;
                        leftVerse.SectionIndex2 = verseSection.Index;

                        var rl = new List<GanjoorVerse>();rl.Add(rightVerse);rl.Add(leftVerse);
                        verseSection.HtmlText = PrepareHtmlText(rl);
                        verseSection.PlainText = PreparePlainText(rl);

                        context.Add(verseSection);
                    }
                }
            }

            context.UpdateRange(nonCommentVerses);
        }
    }
}