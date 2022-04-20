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
            dbCorrection.ReviewNote = moderation.ReviewNote;

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
                
                if (dbCorrection.Result == CorrectionReviewResult.Approved)
                {
                    dbCorrection.AffectedThePoem = true;
                    dbPoem.Title = moderation.Title.Replace("ۀ", "هٔ").Replace("ك", "ک");
                    updatePoem = true;
                }
            }

            var sections = await _context.GanjoorPoemSections.Where(s => s.PoemId == moderation.PoemId).OrderBy(s => s.SectionType).ThenBy(s => s.Index).ToListAsync();
            int maxSections = sections.Count == 0 ? 0 : sections.Max(s => s.Index);
            //beware: items consisting only of paragraphs have no main setion (mainSection in the following line can legitimately become null)
            var mainSection = sections.FirstOrDefault(s => s.SectionType == PoemSectionType.WholePoem && s.VerseType == VersePoemSectionType.First);

            var poemVerses = await _context.GanjoorVerses.Where(p => p.PoemId == dbCorrection.PoemId).OrderBy(v => v.VOrder).ToListAsync();

            if (dbCorrection.Rhythm != null)
            {
                if (moderation.RhythmResult == CorrectionReviewResult.NotReviewed)
                    return new RServiceResult<GanjoorPoemCorrectionViewModel>(null, "تغییرات وزن بررسی نشده است.");
                dbCorrection.RhythmResult = moderation.RhythmResult;
                if (dbCorrection.RhythmResult == CorrectionReviewResult.Approved)
                {
                    if (mainSection == null)
                    {
                        return new RServiceResult<GanjoorPoemCorrectionViewModel>(null, "شعر فاقد بخش اصلی برای ذخیرهٔ وزن است.");
                    }
                    if (poemVerses.Where(v => v.VersePosition == VersePosition.Paragraph).Any())
                    {
                        return new RServiceResult<GanjoorPoemCorrectionViewModel>(null, "امکان انتساب وزن به متون مخلوط از طریق ویرایشگر کاربر وجود ندارد.");
                    }
                    if (sections.Where(s => s.SectionType == PoemSectionType.WholePoem && s.VerseType == VersePoemSectionType.First).Count() > 1)
                    {
                        return new RServiceResult<GanjoorPoemCorrectionViewModel>(null, "امکان انتساب وزن به متون حاوی بیش از یک شعر از طریق ویرایشگر کاربر وجود ندارد.");
                    }

                    dbCorrection.AffectedThePoem = true;
                    mainSection.OldGanjoorMetreId = mainSection.GanjoorMetreId;
                    if (moderation.Rhythm == "")
                    {
                        mainSection.GanjoorMetreId = null;
                    }
                    else
                    {
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
                        mainSection.GanjoorMetreId = metre.Id;
                    }

                    mainSection.Modified = mainSection.OldGanjoorMetreId != mainSection.GanjoorMetreId;

                    foreach (var section in sections.Where(s => s.GanjoorMetreRefSectionIndex == mainSection.Index))
                    {
                        section.GanjoorMetreId = mainSection.GanjoorMetreId;
                        section.Modified = mainSection.Modified;
                    }
                }
            }

            if (dbCorrection.Rhythm2 != null)
            {
                if (moderation.Rhythm2Result == CorrectionReviewResult.NotReviewed)
                    return new RServiceResult<GanjoorPoemCorrectionViewModel>(null, "تغییرات وزن دوم بررسی نشده است.");
                dbCorrection.Rhythm2Result = moderation.RhythmResult;
                if (dbCorrection.Rhythm2Result == CorrectionReviewResult.Approved)
                {
                    if (mainSection == null)
                    {
                        return new RServiceResult<GanjoorPoemCorrectionViewModel>(null, "شعر فاقد بخش اصلی برای ذخیرهٔ وزن است.");
                    }
                    if (poemVerses.Where(v => v.VersePosition == VersePosition.Paragraph).Any())
                    {
                        return new RServiceResult<GanjoorPoemCorrectionViewModel>(null, "امکان انتساب وزن به متون مخلوط از طریق ویرایشگر کاربر وجود ندارد.");
                    }
                    if (sections.Where(s => s.SectionType == PoemSectionType.WholePoem && s.VerseType == VersePoemSectionType.First).Count() > 1)
                    {
                        return new RServiceResult<GanjoorPoemCorrectionViewModel>(null, "امکان انتساب وزن به متون حاوی بیش از یک شعر از طریق ویرایشگر کاربر وجود ندارد.");
                    }
                    if (sections.Where(s => s.SectionType == PoemSectionType.WholePoem && s.VerseType == VersePoemSectionType.Second).Count() > 1)
                    {
                        return new RServiceResult<GanjoorPoemCorrectionViewModel>(null, "امکان انتساب وزن به متون حاوی بیش از یک شعر از طریق ویرایشگر کاربر وجود ندارد.");
                    }

                    dbCorrection.AffectedThePoem = true;
                    var secondMetreSection = sections.FirstOrDefault(s => s.SectionType == PoemSectionType.WholePoem && s.VerseType == VersePoemSectionType.Second);
                    if (secondMetreSection == null)
                    {
                        maxSections++;
                        secondMetreSection = new GanjoorPoemSection()
                        {
                            PoemId = dbPoem.Id,
                            PoetId = mainSection.PoetId,
                            SectionType = PoemSectionType.WholePoem,
                            VerseType = VersePoemSectionType.Second,
                            Index = maxSections,
                            Number = maxSections + 1,
                            GanjoorMetreId = null,
                            RhymeLetters = mainSection.RhymeLetters,
                            HtmlText = mainSection.HtmlText,
                            PlainText = mainSection.PlainText,
                            PoemFormat = mainSection.PoemFormat,
                        };
                        _context.Add(secondMetreSection);

                        foreach (var secondLevelSections in sections.Where(s => s.SectionType != PoemSectionType.WholePoem && s.VerseType == VersePoemSectionType.Second).OrderBy(s => s.Index))
                        {
                            maxSections++;

                        }
                    }
                    secondMetreSection.OldGanjoorMetreId = secondMetreSection.GanjoorMetreId;
                    if (moderation.Rhythm == "")
                    {
                        secondMetreSection.GanjoorMetreId = null;
                    }
                    else
                    {
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
                        secondMetreSection.GanjoorMetreId = metre.Id;
                    }
                }
            }

            if (moderation.VerseOrderText.Length != dbCorrection.VerseOrderText.Count)
                return new RServiceResult<GanjoorPoemCorrectionViewModel>(null, "moderation.VerseOrderText.Length != dbCorrection.VerseOrderText.Count");

            
            
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

            if (modifiedVerses.Count > 0)
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
