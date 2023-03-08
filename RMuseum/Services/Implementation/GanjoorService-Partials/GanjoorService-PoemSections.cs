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
using Azure;
using System.Data.Common;

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
        public RServiceResult<bool> StartFillingPoemSectionsCoupletIndex()
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
                                   var job = (await jobProgressServiceEF.NewJob("StartFillingPoemSectionsCoupletIndex", "Query data")).Result;
                                   int progress = 0;
                                   try
                                   {
                                       var sections = await context.GanjoorPoemSections.OrderBy(s => s.SectionType).ToListAsync();
                                       int count = sections.Count;

                                       for (int i = 0; i < count; i++)
                                       {
                                           var section = sections[i];

                                           var firstSectionVerse = await context.GanjoorVerses.AsNoTracking()
                                                     .Where
                                                     (v =>
                                                         v.PoemId == section.PoemId
                                                         &&
                                                         (
                                                         (section.VerseType == VersePoemSectionType.Second && v.SectionIndex2 == section.Index)
                                                         ||
                                                         (section.VerseType == VersePoemSectionType.Third && v.SectionIndex3 == section.Index)
                                                          ||
                                                         (section.VerseType == VersePoemSectionType.Forth && v.SectionIndex4 == section.Index)
                                                         )
                                                     )
                                                     .OrderBy(v => v.VOrder)
                                                     .FirstOrDefaultAsync();
                                           if (firstSectionVerse != null && firstSectionVerse.CoupletIndex != null)
                                           {
                                               section.CachedFirstCoupletIndex = (int)firstSectionVerse.CoupletIndex;
                                               context.Update(section);
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

        private bool _IsMasnavi(List<GanjoorVerse> verses, bool bandCouplet = false)
        {
            if (verses.Count % 2 != 0)
                return false;
            if(bandCouplet)
            {
                if (verses.Any(v => v.VersePosition != VersePosition.CenteredVerse1 && v.VersePosition != VersePosition.CenteredVerse2))
                    return false;
            }
            else
            {
                if (verses.Any(v => v.VersePosition != VersePosition.Right && v.VersePosition != VersePosition.Left))
                    return false;
            }
           
            int rhymingCouplets = 0;
            for (int i = 0; i < verses.Count; i += 2)
            {
                var rightVerse = verses[i];
                if(bandCouplet)
                {
                    if (rightVerse.VersePosition != VersePosition.CenteredVerse1)
                        return false;
                }
                else
                {
                    if (rightVerse.VersePosition != VersePosition.Right)
                        return false;
                }
               
                
                var leftVerse = verses[i + 1];
                if(bandCouplet)
                {
                    if (leftVerse.VersePosition != VersePosition.CenteredVerse2)
                        return false;
                }
                else
                {
                    if (leftVerse.VersePosition != VersePosition.Left)
                        return false;
                }
                
                List<GanjoorVerse> coupletVerses = new List<GanjoorVerse>();
                coupletVerses.Add(rightVerse);
                coupletVerses.Add(leftVerse);
                var res = LanguageUtils.FindRhyme(coupletVerses, false, bandCouplet);
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
                        if (jobProgressServiceEF != null)
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
            if(_IsMasnavi(bandVerses, true))
            {
                if(mainSection.PoemFormat == GanjoorPoemFormat.MultiBand)
                {
                    mainSection.PoemFormat = GanjoorPoemFormat.TarkibBand;
                }
                for (int v = 0; v < bandVerses.Count; v += 2)
                {
                    if (bandVerses[v].VersePosition != VersePosition.CenteredVerse1 || bandVerses[v + 1].VersePosition != VersePosition.CenteredVerse2)
                        continue;
                    index++;
                    var rightVerse = bandVerses[v];
                    var leftVerse = bandVerses[v + 1];
                    List<GanjoorVerse> coupletVerses = new List<GanjoorVerse>();
                    coupletVerses.Add(rightVerse);
                    coupletVerses.Add(leftVerse);
                    var res = LanguageUtils.FindRhyme(coupletVerses, false, true, true);
                    if(string.IsNullOrEmpty(res.Rhyme))
                    {
                        res = LanguageUtils.FindRhyme(coupletVerses, false, true, false);
                    }


                    GanjoorPoemSection verseSection = new GanjoorPoemSection()
                    {
                        PoemId = poem.Id,
                        PoetId = poem.Cat.PoetId,
                        SectionType = PoemSectionType.Couplet,
                        VerseType = VersePoemSectionType.Third,
                        Index = index,
                        Number = index + 1,
                        GanjoorMetreId = poem.GanjoorMetreId,
                        RhymeLetters = res.Rhyme,
                        GanjoorMetreRefSectionIndex = mainSection.Index,
                    };

                    rightVerse.SectionIndex3 = verseSection.Index;
                    leftVerse.SectionIndex3 = verseSection.Index;

                    context.Update(rightVerse);
                                                   context.Update(leftVerse);

                    var rl = new List<GanjoorVerse>(); rl.Add(rightVerse); rl.Add(leftVerse);
                    verseSection.HtmlText = PrepareHtmlText(rl);
                    verseSection.PlainText = PreparePlainText(rl);

                    context.Add(verseSection);
                }
            }
            else
            {
                bandSection.RhymeLetters = LanguageUtils.FindRhyme(bandVerses).Rhyme;
            }
            bandSection.HtmlText = PrepareHtmlText(bandVerses);
            bandSection.PlainText = PreparePlainText(bandVerses);
            context.Add(mainSection);
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

                        var rl = new List<GanjoorVerse>(); rl.Add(rightVerse); rl.Add(leftVerse);
                        verseSection.HtmlText = PrepareHtmlText(rl);
                        verseSection.PlainText = PreparePlainText(rl);

                        context.Add(verseSection);
                    }
                }
            }

            context.UpdateRange(nonCommentVerses);
        }

        /// <summary>
        /// filter secion verses
        /// </summary>
        /// <param name="section"></param>
        /// <param name="verses"></param>
        /// <returns></returns>
        public List<GanjoorVerse> FilterSectionVerses(GanjoorPoemSection section, List<GanjoorVerse> verses)
        {
            List<GanjoorVerse> sectionVerses = new List<GanjoorVerse>();
            foreach (GanjoorVerse verse in verses)
            {
                switch (section.VerseType)
                {
                    case VersePoemSectionType.First:
                        if (verse.SectionIndex1 == section.Index)
                            sectionVerses.Add(verse);
                        break;
                    case VersePoemSectionType.Second:
                        if (verse.SectionIndex2 == section.Index)
                            sectionVerses.Add(verse);
                        break;
                    case VersePoemSectionType.Third:
                        if (verse.SectionIndex3 == section.Index)
                            sectionVerses.Add(verse);
                        break;
                    default:
                        if (verse.SectionIndex4 == section.Index)
                            sectionVerses.Add(verse);
                        break;
                }
            }
            return sectionVerses;
        }

        /// <summary>
        /// get couplet sections
        /// </summary>
        /// <param name="poemId"></param>
        /// <param name="coupletIndex"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPoemSection[]>> GetCoupletSectionsAsync(int poemId, int coupletIndex)
        {
            var firstVerse = await _context.GanjoorVerses.AsNoTracking().Where(v => v.PoemId == poemId && v.CoupletIndex == coupletIndex).OrderBy(v => v.VOrder).FirstOrDefaultAsync();
            if (firstVerse == null)
                return new RServiceResult<GanjoorPoemSection[]>(null);//not found
            try
            {
                return new RServiceResult<GanjoorPoemSection[]>
                (
                await _context.GanjoorPoemSections.AsNoTracking().Include(s => s.GanjoorMetre)
                .Where(s =>
                        s.PoemId == poemId
                        &&
                        (
                        s.Index == firstVerse.SectionIndex1
                        ||
                        s.Index == firstVerse.SectionIndex2
                        ||
                        s.Index == firstVerse.SectionIndex3
                        ||
                        s.Index == firstVerse.SectionIndex4
                        )
                        )
                .OrderBy(s => s.SectionType).ThenBy(s => s.VerseType)
                .ToArrayAsync()
                );
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorPoemSection[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get all poem sections
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPoemSection[]>> GetPoemSectionsAsync(int id)
        {
            var sections =
                await _context.GanjoorPoemSections.AsNoTracking().Include(s => s.GanjoorMetre).Where(s => s.PoemId == id).OrderBy(s => s.SectionType).ThenBy(s => s.VerseType).ToArrayAsync();

            return new RServiceResult<GanjoorPoemSection[]>(sections);
        }

        /// <summary>
        /// regenerate poem sections (dangerous: wipes out existing data)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> RegeneratePoemSections(int id)
        {
            LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(_context);
            var job = (await jobProgressServiceEF.NewJob($"RegeneratePoemSections  - {id}", "Query data")).Result;
            try
            {
                var sections = await _context.GanjoorPoemSections.Where(s => s.PoemId == id).ToListAsync();
                int? meterId = sections.Any(s => s.SectionType == PoemSectionType.WholePoem && s.VerseType == VersePoemSectionType.First) ? sections.First(s => s.SectionType == PoemSectionType.WholePoem && s.VerseType == VersePoemSectionType.First).GanjoorMetreId : null;
                _context.RemoveRange(sections);
                await _context.SaveChangesAsync();
                var poem = await _context.GanjoorPoems.Include(p => p.Cat).AsNoTracking().SingleAsync(p => p.Id == id);
               
                await _SectionizePoem(_context, poem, jobProgressServiceEF, job);

                if(meterId != null)
                {
                    List<string> rhymes = new List<string>();
                    var newSections = await _context.GanjoorPoemSections.Where(s => s.PoemId == id).ToListAsync();
                    foreach (var section in newSections)
                    {
                        section.GanjoorMetreId = meterId;
                        if(!string.IsNullOrEmpty(section.RhymeLetters))
                        {
                            if(!rhymes.Contains(section.RhymeLetters))
                            {
                                rhymes.Add(section.RhymeLetters);
                            }
                        }
                    }
                    _context.UpdateRange(newSections);
                    await _context.SaveChangesAsync();
                    if (rhymes.Count > 0)
                    {
                        _backgroundTaskQueue.QueueBackgroundWorkItem
                           (
                           async token =>
                           {
                               using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                               {
                                   LongRunningJobProgressServiceEF jobProgressServiceEF2 = new LongRunningJobProgressServiceEF(context);
                                   var job2 = (await jobProgressServiceEF2.NewJob($"RegeneratePoemSections  - Updating related sections", "Query data")).Result;
                                   foreach (var rhyme in rhymes)
                                   {
                                       await _UpdateRelatedSections(context, (int)meterId, rhyme, jobProgressServiceEF2, job2);
                                   }
                               }
                           }
                           );
                    }
                   
                }

                await jobProgressServiceEF.UpdateJob(job.Id, 100, "", true);
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, exp.ToString());
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// get a specific poem sections
        /// </summary>
        /// <param name="sectionId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPoemSection>> GetPoemSectionByIdAsync(int sectionId)
        {
            return new RServiceResult<GanjoorPoemSection>
                (
                await _context.GanjoorPoemSections.AsNoTracking().Include(s => s.GanjoorMetre).Where(s => s.Id == sectionId).SingleOrDefaultAsync()
                );
        }

        /// <summary>
        /// delete a poem section
        /// </summary>
        /// <param name="poemId"></param>
        /// <param name="sectionIndex"></param>
        /// <param name="convertVerses"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeletePoemSectionByPoemIdAndIndexAsync(int poemId, int sectionIndex, bool convertVerses)
        {
            try
            {
                var section = await _context.GanjoorPoemSections.Where(s => s.PoemId == poemId && s.Index == sectionIndex ).FirstOrDefaultAsync();
                if (section == null)
                {
                    return new RServiceResult<bool>(false);//Not found
                }
                var connextedVerses =
                    await _context.GanjoorVerses
                        .Where
                        (
                        v => v.PoemId == poemId
                        &&
                        (v.SectionIndex1 == sectionIndex || v.SectionIndex2 == sectionIndex || v.SectionIndex3 == sectionIndex || v.SectionIndex4 == sectionIndex)
                        ).ToListAsync();
                bool regenPageHtml = false;
                foreach (var verse in connextedVerses)
                {
                    if (section.SectionType == PoemSectionType.WholePoem)
                    {
                        if (verse.VersePosition != VersePosition.Paragraph && verse.VersePosition != VersePosition.Comment)
                        {
                            if (convertVerses)
                            {
                                verse.VersePosition = VersePosition.Paragraph;
                                regenPageHtml = true;
                            }
                            else
                            {
                                return new RServiceResult<bool>(false, "قطعه شامل مصرع‌های غیرپاراگرافی است.");
                            }
                        }
                    }
                    if (verse.SectionIndex1 == sectionIndex)
                    {
                        verse.SectionIndex1 = null;
                    }
                    if (verse.SectionIndex2 == sectionIndex)
                    {
                        verse.SectionIndex2 = null;
                    }
                    if (verse.SectionIndex3 == sectionIndex)
                    {
                        verse.SectionIndex3 = null;
                    }
                    if (verse.SectionIndex4 == sectionIndex)
                    {
                        verse.SectionIndex4 = null;
                    }

                }

                _context.UpdateRange(connextedVerses);
                _context.Remove(section);

                await _context.SaveChangesAsync();

                if(regenPageHtml)
                {
                    var poemVerses = await _context.GanjoorVerses.AsNoTracking().Where(p => p.PoemId == poemId).OrderBy(v => v.VOrder).ToListAsync();
                    var dbPoem = await _context.GanjoorPoems.Include(p => p.GanjoorMetre).Where(p => p.Id == poemId).SingleAsync();
                    var dbPage = await _context.GanjoorPages.Where(p => p.Id == poemId).SingleAsync();
                    dbPoem.HtmlText = PrepareHtmlText(poemVerses);
                    dbPoem.PlainText = PreparePlainText(poemVerses);
                    dbPage.HtmlText = dbPoem.HtmlText;
                    _context.Update(dbPoem);
                    _context.Update(dbPage);
                    await _context.SaveChangesAsync();
                }


                return new RServiceResult<bool>(true);
            }
            catch (Exception e)
            {
                return new RServiceResult<bool>(false, e.ToString());
            }
        }

        /// <summary>
        /// start band couplets fix
        /// </summary>
        /// <returns></returns>
        public RServiceResult<bool> StartOnTimeBandCoupletsFix()
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
                                   var job = (await jobProgressServiceEF.NewJob("StartBandCoupletsFix", "Query data")).Result;
                                   int progress = 0;
                                   try
                                   {
                                       var sections = await context.GanjoorPoemSections.Where(s => s.SectionType == PoemSectionType.BandCouplets).ToListAsync();
                                       int count = sections.Count;

                                       for (int i = 0; i < count; i++)
                                       {
                                           progress++;
                                           var section = sections[i];
                                           section.OldRhymeLetters = section.RhymeLetters;
                                           var verses = await context.GanjoorVerses.Where(v => v.PoemId == section.PoemId).OrderBy(v => v.VOrder).ToListAsync();
                                           var bandVerses = FilterSectionVerses(section, verses);
                                           var resRetry = LanguageUtils.FindRhyme(bandVerses, false, true, true);
                                           if(string.IsNullOrEmpty(resRetry.Rhyme))
                                               resRetry = LanguageUtils.FindRhyme(bandVerses, false, true, false);
                                           if (!string.IsNullOrEmpty(resRetry.Rhyme) && section.OldRhymeLetters != resRetry.Rhyme)
                                           {
                                               section.RhymeLetters = resRetry.Rhyme;
                                               context.Update(section);
                                               await jobProgressServiceEF.UpdateJob(job.Id, progress);
                                               if (section.GanjoorMetreId != null && section.OldRhymeLetters != resRetry.Rhyme)
                                               {
                                                   if(!string.IsNullOrEmpty(section.OldRhymeLetters))
                                                   {
                                                       await _UpdateRelatedSections(context, (int)section.GanjoorMetreId, section.OldRhymeLetters);
                                                   }
                                                   await _UpdateRelatedSections(context, (int)section.GanjoorMetreId, section.RhymeLetters);
                                               }
                                               continue;
                                           }
                                           if (_IsMasnavi(bandVerses, true))
                                           {
                                               var poemSections = await context.GanjoorPoemSections.Where(s => s.PoemId == section.PoemId).ToListAsync();
                                               if (poemSections.Any(s => s.VerseType == VersePoemSectionType.Forth))
                                                   continue;
                                               var mainSections = poemSections.Where(s => s.SectionType == PoemSectionType.WholePoem && s.VerseType == VersePoemSectionType.First).ToList();
                                               GanjoorPoemSection mainSection = null;
                                               if (mainSections.Count == 1)
                                               {
                                                   mainSection = mainSections.First();
                                                   if (mainSection.PoemFormat == GanjoorPoemFormat.MultiBand)
                                                   {
                                                       mainSection.PoemFormat = GanjoorPoemFormat.TarkibBand;
                                                       context.Update(mainSection);
                                                   }
                                               }
                                               else
                                                   continue;

                                               int index = poemSections.Max(s => s.Index);
                                               VersePoemSectionType verseType =
                                                    poemSections.Any(s => s.VerseType == VersePoemSectionType.Third) ? VersePoemSectionType.Forth : VersePoemSectionType.Third;

                                               string preRhymeLetters = "";
                                               for (int v = 0; v < bandVerses.Count; v += 2)
                                               {
                                                   if (bandVerses[v].VersePosition != VersePosition.CenteredVerse1 || bandVerses[v + 1].VersePosition != VersePosition.CenteredVerse2)
                                                       continue;

                                                   index++;
                                                   var rightVerse = bandVerses[v];
                                                   var leftVerse = bandVerses[v + 1];
                                                   List<GanjoorVerse> coupletVerses = new List<GanjoorVerse>();
                                                   coupletVerses.Add(rightVerse);
                                                   coupletVerses.Add(leftVerse);
                                                   var res = LanguageUtils.FindRhyme(coupletVerses);

                                                   GanjoorPoemSection verseSection = new GanjoorPoemSection()
                                                   {
                                                       PoemId = section.PoemId,
                                                       PoetId = section.PoetId,
                                                       SectionType = PoemSectionType.Couplet,
                                                       VerseType = verseType,
                                                       Index = index,
                                                       Number = index + 1,
                                                       GanjoorMetreId = mainSection.GanjoorMetreId,
                                                       RhymeLetters = res.Rhyme,
                                                       GanjoorMetreRefSectionIndex = mainSection.Index,
                                                       CachedFirstCoupletIndex = (int)rightVerse.CoupletIndex,
                                                   };

                                                   if(verseType == VersePoemSectionType.Third)
                                                   {
                                                       rightVerse.SectionIndex3 = verseSection.Index;
                                                       leftVerse.SectionIndex3 = verseSection.Index;
                                                   }
                                                   else
                                                   {
                                                       rightVerse.SectionIndex4 = verseSection.Index;
                                                       leftVerse.SectionIndex4 = verseSection.Index;
                                                   }

                                                   context.Update(rightVerse);
                                                   context.Update(leftVerse);
                                                   

                                                   var rl = new List<GanjoorVerse>(); rl.Add(rightVerse); rl.Add(leftVerse);
                                                   verseSection.HtmlText = PrepareHtmlText(rl);
                                                   verseSection.PlainText = PreparePlainText(rl);

                                                   context.Add(verseSection);

                                                   await jobProgressServiceEF.UpdateJob(job.Id, progress);

                                                   if(verseSection.GanjoorMetreId != null && !string.IsNullOrEmpty(verseSection.RhymeLetters) && preRhymeLetters != verseSection.RhymeLetters)
                                                   {
                                                       preRhymeLetters = verseSection.RhymeLetters;
                                                       await _UpdateRelatedSections(context, (int)verseSection.GanjoorMetreId, verseSection.RhymeLetters);
                                                   }
                                               }
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
    }
}