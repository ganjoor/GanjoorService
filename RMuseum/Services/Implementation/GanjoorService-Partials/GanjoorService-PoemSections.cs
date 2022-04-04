using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services.Implementation;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {
        public RServiceResult<bool> StartSectionizingPoems(bool clearOldSections = false)
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
                                   try
                                   {
                                       var poems = await context.GanjoorPoems.Include(p  => p.Cat).AsNoTracking().ToListAsync();
                                       int count = poems.Count;
                                       int progress = 0;
                                       for (int i = 0; i < count; i++)
                                       {
                                           var poem = poems[i];
                                           var oldSections = await context.GanjoorPoemSections.Where(s => s.PoemId == poem.Id).ToListAsync();
                                           if (oldSections.Count > 0)
                                           {
                                               if (!clearOldSections) continue;
                                               context.RemoveRange(oldSections);
                                               await context.SaveChangesAsync();
                                           }
                                           var nonCommentVerses = await context.GanjoorVerses.Where(v => v.PoemId == poem.Id && v.VersePosition != VersePosition.Comment).OrderBy(v => v.VOrder).ToListAsync();
                                           if (!nonCommentVerses.Where(v => v.VersePosition != VersePosition.Right && v.VersePosition != VersePosition.Left).Any())
                                           {
                                               //normal poem
                                               GanjoorPoemSection mainSection = new GanjoorPoemSection()
                                               {
                                                   PoemId = poem.Id,
                                                   PoetId = poem.Cat.PoetId,
                                                   SectionType = PoemSectionType.WholePoem,
                                                   VerseType = VersePoemSectionType.First,
                                                   Index = 0,
                                                   Number = 1,
                                                   GanjoorMetreId = poem.GanjoorMetreId,
                                                   RhymeLetters = poem.RhymeLetters
                                               };
                                               //checking for مثنوی phase 1
                                               if (string.IsNullOrEmpty(poem.RhymeLetters))
                                               {
                                                   var analysisRes = await _FindPoemRhyme(poem.Id, context);
                                                   if (!string.IsNullOrEmpty(analysisRes.Result.Rhyme))
                                                   {
                                                       mainSection.RhymeLetters = analysisRes.Result.Rhyme;
                                                   }
                                               }
                                               context.Add(mainSection);//having a main section for مثنوی inside normal text helps keep track of related versess
                                               foreach (var verse in nonCommentVerses)
                                               {
                                                   verse.SectionIndex = mainSection.Index;
                                                   verse.SecondSectionIndex = null;//clear previous indices
                                                   verse.ThirdSectionIndex = null;//clear previous indices
                                               }

                                               int index = 0;
                                               //checking for مثنوی phase 2
                                               if (nonCommentVerses.Count > 2 && string.IsNullOrEmpty(mainSection.RhymeLetters))
                                               {
                                                   if(_IsMasnavi(nonCommentVerses))
                                                   {
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
                                                               RhymeLetters = res.Rhyme
                                                           };

                                                           rightVerse.SecondSectionIndex = verseSection.Index;
                                                           leftVerse.SecondSectionIndex = verseSection.Index;

                                                           context.Add(verseSection);
                                                       }
                                                   }
                                               }

                                               context.UpdateRange(nonCommentVerses);
                                           }
                                           else
                                           if(!nonCommentVerses.Where(v => 
                                                    v.VersePosition != VersePosition.CenteredVerse1
                                                    &&
                                                    v.VersePosition != VersePosition.CenteredVerse2
                                                    &&
                                                    v.VersePosition != VersePosition.Right
                                                    &&
                                                    v.VersePosition != VersePosition.Left
                                                    &&
                                                    v.VersePosition != VersePosition.Comment
                                                    ).Any())
                                           {
                                               //multi-band poem
                                               GanjoorPoemSection mainSection = new GanjoorPoemSection()
                                               {
                                                   PoemId = poem.Id,
                                                   PoetId = poem.Cat.PoetId,
                                                   SectionType = PoemSectionType.WholePoem,
                                                   VerseType = VersePoemSectionType.First,
                                                   Index = 0,
                                                   Number = 1,
                                                   GanjoorMetreId = poem.GanjoorMetreId,
                                                   RhymeLetters = poem.RhymeLetters
                                               };
                                               context.Add(mainSection);
                                               int index = 1;
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
                                               };
                                               List<GanjoorVerse> bandVerses = new List<GanjoorVerse>();
                                               foreach (var verse in nonCommentVerses)
                                               {
                                                   verse.SectionIndex = mainSection.Index;
                                                   verse.SecondSectionIndex = null;//clear previous indices
                                                   verse.ThirdSectionIndex = null;//clear previous indices
                                                   if (verse.VersePosition == VersePosition.Right || verse.VersePosition == VersePosition.Left)
                                                   {
                                                       currentBandVerses.Add(verse);
                                                   }
                                                   if(verse.VersePosition == VersePosition.CenteredVerse1 || verse.VersePosition == VersePosition.CenteredVerse2)
                                                   {
                                                       bandVerses.Add(verse);

                                                       if(currentBandVerses.Count > 0)
                                                       {
                                                           foreach (var currentBandVerse in currentBandVerses)
                                                           {
                                                               currentBandVerse.SecondSectionIndex = currentBandSection.Index;
                                                           }
                                                           currentBandSection.RhymeLetters = LanguageUtils.FindRhyme(currentBandVerses).Rhyme;
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
                                                           };
                                                       }
                                                   }
                                               }

                                               if (currentBandVerses.Count > 0)
                                               {
                                                   foreach (var currentBandVerse in currentBandVerses)
                                                   {
                                                       currentBandVerse.SecondSectionIndex = currentBandSection.Index;
                                                   }
                                                   currentBandSection.RhymeLetters = LanguageUtils.FindRhyme(currentBandVerses).Rhyme;
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
                                               };
                                               foreach (var bandVerse in bandVerses)
                                               {
                                                   bandVerse.SecondSectionIndex = bandSection.Index;
                                               }
                                               bandSection.RhymeLetters = LanguageUtils.FindRhyme(bandVerses).Rhyme;
                                               context.Add(bandSection);
                                               context.UpdateRange(nonCommentVerses);
                                           }
                                           else
                                           {
                                               //poems with different types of verses
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
                                       await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, exp.ToString());
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
    }
}