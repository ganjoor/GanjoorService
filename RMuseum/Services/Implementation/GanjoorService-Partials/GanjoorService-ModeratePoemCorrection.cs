using Microsoft.EntityFrameworkCore;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
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
        /// moderate poem correction
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="moderation"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPoemCorrectionViewModel>> ModeratePoemCorrection(Guid userId,
            GanjoorPoemCorrectionViewModel moderation)
        {
            var dbCorrection = await _context.GanjoorPoemCorrections.Include(c => c.VerseOrderText).Include(c => c.User)
                .Where(c => c.Id == moderation.Id)
                .FirstOrDefaultAsync();

            dbCorrection.ReviewerUserId = userId;
            dbCorrection.ReviewDate = DateTime.Now;
            dbCorrection.ApplicationOrder = await _context.GanjoorPoemCorrections.Where(c => c.Reviewed).AnyAsync() ? 1 + await _context.GanjoorPoemCorrections.Where(c => c.Reviewed).MaxAsync(c => c.ApplicationOrder) : 1;
            dbCorrection.Reviewed = true;
            dbCorrection.AffectedThePoem = false;

            if (dbCorrection == null)
                return new RServiceResult<GanjoorPoemCorrectionViewModel>(null);

            var dbPoem = await _context.GanjoorPoems.Include(p => p.GanjoorMetre).Where(p => p.Id == moderation.PoemId).SingleOrDefaultAsync();
            var dbPage = await _context.GanjoorPages.AsNoTracking().Where(p => p.Id == moderation.PoemId).SingleOrDefaultAsync();

            bool updatePoem = false;
            if (dbCorrection.Title != null)
            {
                if (moderation.Result == CorrectionReviewResult.NotReviewed)
                    return new RServiceResult<GanjoorPoemCorrectionViewModel>(null, "تغییرات عنوان بررسی نشده است.");
                dbCorrection.Result = moderation.Result;
                dbCorrection.ReviewNote = moderation.ReviewNote;
                if (dbCorrection.Result == CorrectionReviewResult.Approved)
                {
                    dbCorrection.AffectedThePoem = true;
                    dbPoem.Title = moderation.Title.Replace("ۀ", "هٔ").Replace("ك", "ک");
                    updatePoem = true;
                }
            }

            var sections = await _context.GanjoorPoemSections.Where(s => s.PoemId == moderation.PoemId).OrderBy(s => s.SectionType).ThenBy(s => s.Index).ToListAsync();
            

            bool metreChanged = false;
            if (dbCorrection.Rhythm != null)
            {
                if (moderation.RhythmResult == CorrectionReviewResult.NotReviewed)
                    return new RServiceResult<GanjoorPoemCorrectionViewModel>(null, "تغییرات وزن بررسی نشده است.");
                dbCorrection.RhythmResult = moderation.RhythmResult;
                dbCorrection.ReviewNote = moderation.ReviewNote;
                if (dbCorrection.RhythmResult == CorrectionReviewResult.Approved)
                {
                    dbCorrection.AffectedThePoem = true;
                    var mainSection = sections.First(s => s.SectionType == PoemSectionType.WholePoem && s.VerseType == VersePoemSectionType.First);
                    if (mainSection == null)
                    {
                        return new RServiceResult<GanjoorPoemCorrectionViewModel>(null, "شعر فاقد بخش اصلی برای ذخیرهٔ وزن است.");
                    }
                    var metre = await _context.GanjoorMetres.AsNoTracking().Where(m => m.Rhythm == moderation.Rhythm).SingleOrDefaultAsync();
                    if (metre == null)
                    {
                        metre = new GanjoorMetre()
                        {
                            Rhythm = moderation.Rhythm,
                            VerseCount = 0
                        };
                        _context.GanjoorMetres.Add(metre);
                        await _context.SaveChangesAsync();
                    }
                    metreChanged = mainSection.GanjoorMetreId != metre.Id;
                    mainSection.GanjoorMetreId = metre.Id;
                }
            }

            if (moderation.VerseOrderText.Length != dbCorrection.VerseOrderText.Count)
                return new RServiceResult<GanjoorPoemCorrectionViewModel>(null, "moderation.VerseOrderText.Length != dbCorrection.VerseOrderText.Count");

            var poemVerses = await _context.GanjoorVerses.Where(p => p.PoemId == dbCorrection.PoemId).OrderBy(v => v.VOrder).ToListAsync();
            
            var modifiedVerses = new List<GanjoorVerse>();

            foreach (var moderatedVerse in moderation.VerseOrderText)
            {
                if (moderatedVerse.Result == CorrectionReviewResult.NotReviewed)
                    return new RServiceResult<GanjoorPoemCorrectionViewModel>(null, $"تغییرات مصرع {moderatedVerse.VORder} بررسی نشده است.");
                var dbVerse = dbCorrection.VerseOrderText.Where(c => c.VORder == moderatedVerse.VORder).Single();
                dbVerse.Result = moderatedVerse.Result;
                dbVerse.ReviewNote = moderatedVerse.ReviewNote;
                if (dbVerse.Result == CorrectionReviewResult.Approved)
                {
                    dbCorrection.AffectedThePoem = true;
                    var poemVerse = poemVerses.Where(v => v.VOrder == moderatedVerse.VORder).Single();
                    poemVerse.Text = moderatedVerse.Text.Replace("ۀ", "هٔ").Replace("ك", "ک");
                    modifiedVerses.Add(poemVerse);
                }
            }

            if(modifiedVerses.Count > 0)
            {
                _context.UpdateRange(modifiedVerses);
                dbPoem.HtmlText = PrepareHtmlText(poemVerses);
                dbPoem.PlainText = PreparePlainText(poemVerses);
                updatePoem = true;
            }

            if(updatePoem)
            {
                _context.Update(dbPoem);
            }

            await _context.SaveChangesAsync();

            if (metreChanged || modifiedVerses.Count > 0)
            {
                foreach (var section in sections)
                {
                    var sectionVerses = poemVerses.Where(v =>
                            (section.VerseType == VersePoemSectionType.First && v.SectionIndex == section.Index)
                            ||
                            (section.VerseType == VersePoemSectionType.Second && v.SecondSectionIndex == section.Index)
                            ||
                            (section.VerseType == VersePoemSectionType.Third && v.ThirdSectionIndex == section.Index)
                            ||
                            (section.VerseType == VersePoemSectionType.Forth && v.ForthSectionIndex == section.Index)
                            ).OrderBy(v => v.VOrder).ToList();
                    if(sectionVerses.Any(v => modifiedVerses.Contains(v)))
                    {
                        section.HtmlText = PrepareHtmlText(sectionVerses);
                        section.PlainText = PreparePlainText(sectionVerses);
                    }
                }
            }


            /*
            string originalRhyme = "";
            if(mainSection != null)
            {
                try
                {
                    var newRhyme = LanguageUtils.FindRhyme(poemVerses);
                    if (!string.IsNullOrEmpty(newRhyme.Rhyme) &&
                        (string.IsNullOrEmpty(mainSection.RhymeLetters) || (!string.IsNullOrEmpty(mainSection.RhymeLetters) && newRhyme.Rhyme.Length > mainSection.RhymeLetters.Length))
                        )
                    {
                        originalRhyme = mainSection.RhymeLetters ?? "";
                        if(originalRhyme != newRhyme.Rhyme)
                        {
                            mainSection.RhymeLetters = newRhyme.Rhyme;
                            _context.Update(mainSection);
                        }
                        
                    }
                }
                catch
                {

                }
            }
            */
            


            _context.GanjoorPoemCorrections.Update(dbCorrection);
            await _context.SaveChangesAsync();

            await _notificationService.PushNotification(dbCorrection.UserId,
                               "بررسی ویرایش پیشنهادی شما",
                               $"با سپاس از زحمت و همت شما ویرایش پیشنهادیتان برای <a href=\"{dbPoem.FullUrl}\" target=\"_blank\">{dbPoem.FullTitle}</a> بررسی شد.{Environment.NewLine}" +
                               $"جهت مشاهدهٔ نتیجهٔ بررسی در میز کاربری خود بخش «ویرایش‌های من» را مشاهده بفرمایید.{Environment.NewLine}"
                               );

            return new RServiceResult<GanjoorPoemCorrectionViewModel>(moderation);
        }
    }
}
