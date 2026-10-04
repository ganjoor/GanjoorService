using DNTPersianUtils.Core;
using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
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
        /// moderate poem correction
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="moderation"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPoemCorrectionViewModel>> ModeratePoemCorrection(Guid userId,
            GanjoorPoemCorrectionViewModel moderation)
        {
            try
            {
                var dbCorrection = await _context.GanjoorPoemCorrections.Include(c => c.VerseOrderText).Include(c => c.GeoDateTags).Include(c => c.User)
                .Where(c => c.Id == moderation.Id)
                .FirstOrDefaultAsync();

                if (dbCorrection == null)
                    return new RServiceResult<GanjoorPoemCorrectionViewModel>(null);

                if (!string.IsNullOrEmpty(dbCorrection.Rhythm3) || !string.IsNullOrEmpty(dbCorrection.Rhythm4))
                    return new RServiceResult<GanjoorPoemCorrectionViewModel>(null, "انتساب وزن سوم و چهارم هنوز پیاده‌سازی نشده است.");

                dbCorrection.ReviewerUserId = userId;
                dbCorrection.ReviewDate = DateTime.Now;
                dbCorrection.ApplicationOrder = await _context.GanjoorPoemCorrections.Where(c => c.Reviewed).AnyAsync() ? 1 + await _context.GanjoorPoemCorrections.Where(c => c.Reviewed).MaxAsync(c => c.ApplicationOrder) : 1;

                dbCorrection.AffectedThePoem = false;
                dbCorrection.ReviewNote = moderation.ReviewNote;

                var dbPoem = await _context.GanjoorPoems.Include(p => p.GanjoorMetre).Where(p => p.Id == moderation.PoemId).SingleOrDefaultAsync();
                var dbPage = await _context.GanjoorPages.Where(p => p.Id == moderation.PoemId).SingleOrDefaultAsync();
                var sections = await _context.GanjoorPoemSections.Include(s => s.GanjoorMetre).Where(s => s.PoemId == moderation.PoemId).OrderBy(s => s.SectionType).ThenBy(s => s.Index).ToListAsync();
                foreach (var section in sections)
                {
                    section.OldGanjoorMetreId = section.GanjoorMetreId;
                    section.OldRhymeLetters = section.RhymeLetters;
                }
                //beware: items consisting only of paragraphs have no main setion (mainSection in the following line can legitimately become null)
                var mainSection = sections.FirstOrDefault(s => s.SectionType == PoemSectionType.WholePoem && s.VerseType == VersePoemSectionType.First);

                GanjoorPageSnapshot snapshot = new GanjoorPageSnapshot()
                {
                    GanjoorPageId = dbPage.Id,
                    MadeObsoleteByUserId = userId,
                    RecordDate = DateTime.Now,
                    Note = $"بررسی پیشنهاد ویرایش با شناسهٔ {dbCorrection.Id}",
                    Title = dbPoem.Title,
                    UrlSlug = dbPoem.UrlSlug,
                    HtmlText = dbPoem.HtmlText,
                    SourceName = dbPoem.SourceName,
                    SourceUrlSlug = dbPoem.SourceUrlSlug,
                    Rhythm = mainSection == null || mainSection.GanjoorMetre == null ? null : mainSection.GanjoorMetre.Rhythm,
                    RhymeLetters = mainSection == null ? null : mainSection.RhymeLetters,
                    OldTag = dbPoem.OldTag,
                    OldTagPageUrl = dbPoem.OldTagPageUrl
                };
                _context.GanjoorPageSnapshots.Add(snapshot);
                await _context.SaveChangesAsync();

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
                        dbPage.Title = dbPoem.Title;
                        if (dbPage.ParentId != null)
                        {
                            GanjoorPage parent = await _context.GanjoorPages.AsNoTracking().Where(p => p.Id == dbPage.ParentId).SingleAsync();
                            dbPage.FullTitle = parent.FullTitle + " » " + dbPage.Title;
                            dbPoem.FullTitle = dbPage.FullTitle;
                        }
                        updatePoem = true;
                    }
                }

                if(dbCorrection.PoemSummary != null)
                {
                    if(moderation.SummaryReviewResult == CorrectionReviewResult.NotSuggestedByUser)
                        return new RServiceResult<GanjoorPoemCorrectionViewModel>(null, "تغییرات خلاصه به زبان ساده بررسی نشده است.");
                    dbCorrection.SummaryReviewResult = moderation.SummaryReviewResult;
                    if(dbCorrection.SummaryReviewResult == CorrectionReviewResult.Approved)
                    {
                        dbCorrection.AffectedThePoem = true;
                        dbPoem.PoemSummary = moderation.PoemSummary.Replace("ۀ", "هٔ").Replace("ك", "ک").Replace("ي", "ی");
                        updatePoem = true;
                    }
                }

                int maxSections = sections.Count == 0 ? 0 : sections.Max(s => s.Index);

                var poemVerses = await _context.GanjoorVerses.Where(p => p.PoemId == dbCorrection.PoemId).OrderBy(v => v.VOrder).ToListAsync();

                if (moderation.VerseOrderText.Length != dbCorrection.VerseOrderText.Count)
                    return new RServiceResult<GanjoorPoemCorrectionViewModel>(null, "moderation.VerseOrderText.Length != dbCorrection.VerseOrderText.Count");

                bool versePosistionsChanged = false;
                var modifiedVerses = new List<GanjoorVerse>();

                foreach (var moderatedVerse in moderation.VerseOrderText)
                {
                    if (moderatedVerse.MarkForDelete || moderatedVerse.NewVerse)
                        continue;
                    if (moderatedVerse.Result == CorrectionReviewResult.NotReviewed)
                        return new RServiceResult<GanjoorPoemCorrectionViewModel>(null, $"تغییرات مصرع {moderatedVerse.VORder} بررسی نشده است.");
                    var dbVerse = dbCorrection.VerseOrderText.Where(c => c.VORder == moderatedVerse.VORder).Single();
                    dbVerse.Result = moderatedVerse.Result;
                    dbVerse.ReviewNote = moderatedVerse.ReviewNote;
                    if (dbVerse.VersePosition != null)
                    {
                        if (moderatedVerse.VersePositionResult == CorrectionReviewResult.NotReviewed)
                            return new RServiceResult<GanjoorPoemCorrectionViewModel>(null, $"تغییرات نوع مصرع {moderatedVerse.VORder} بررسی نشده است.");
                        versePosistionsChanged = true;
                        dbVerse.VersePositionResult = moderatedVerse.VersePositionResult;
                    }
                    if(dbVerse.CoupletSummary != null)
                    {
                        if(moderatedVerse.SummaryReviewResult == CorrectionReviewResult.NotReviewed)
                            return new RServiceResult<GanjoorPoemCorrectionViewModel>(null, $"زبان سادهٔ مصرع {moderatedVerse.VORder} بررسی نشده است.");
                        dbVerse.SummaryReviewResult = moderatedVerse.SummaryReviewResult;
                    }
                    if(dbVerse.LanguageId != null)
                    {
                        if(moderatedVerse.LanguageReviewResult == CorrectionReviewResult.NotReviewed)
                            return new RServiceResult<GanjoorPoemCorrectionViewModel>(null, $"زبان مصرع {moderatedVerse.VORder} بررسی نشده است.");
                        dbVerse.LanguageReviewResult = moderatedVerse.LanguageReviewResult;
                    }
                    if (dbVerse.Result == CorrectionReviewResult.Approved
                        ||
                        dbVerse.VersePositionResult == CorrectionReviewResult.Approved
                        ||
                        dbVerse.SummaryReviewResult == CorrectionReviewResult.Approved
                        ||
                        dbVerse.LanguageReviewResult == CorrectionReviewResult.Approved
                        )
                    {
                        dbCorrection.AffectedThePoem = true;
                        var poemVerse = poemVerses.Where(v => v.VOrder == moderatedVerse.VORder).Single();
                        if (dbVerse.Result == CorrectionReviewResult.Approved)
                            poemVerse.Text = moderatedVerse.Text.Replace("ۀ", "هٔ").Replace("ك", "ک");
                        if (dbVerse.VersePositionResult == CorrectionReviewResult.Approved)
                            poemVerse.VersePosition = (VersePosition)dbVerse.VersePosition;
                        if (dbVerse.SummaryReviewResult == CorrectionReviewResult.Approved)
                            // dbVerse.CoupletSummary was already run through this same substitution when the
                            // suggestion was first submitted (see Editor.cshtml.cs's OnPostSaveCorrectionsAsync),
                            // but re-applying it here too - same as Title/PoemSummary/verse Text above - keeps
                            // this field consistent even if a suggestion ever arrives through another path.
                            poemVerse.CoupletSummary = dbVerse.CoupletSummary?.Replace("ۀ", "هٔ").Replace("ك", "ک").Replace("ي", "ی");
                        if (dbVerse.LanguageReviewResult == CorrectionReviewResult.Approved)
                        {
                            if(dbVerse.LanguageId == 1)//fa-IR
                            {
                                dbVerse.LanguageId = null;
                            }
                            poemVerse.LanguageId = dbVerse.LanguageId;
                        }
                        modifiedVerses.Add(poemVerse);
                    }
                }


                if (modifiedVerses.Count > 0)
                {
                    _context.UpdateRange(modifiedVerses);
                    dbPoem.HtmlText = PrepareHtmlText(poemVerses);
                    dbPoem.PlainText = PreparePlainText(poemVerses, dbPoem.Title);
                    dbPage.HtmlText = dbPoem.HtmlText;
                    updatePoem = true;
                }

                bool versesDeleted = false;
                foreach (var moderatedVerse in moderation.VerseOrderText.Where(v => v.MarkForDelete == true).ToList())
                {
                    if (moderatedVerse.MarkForDeleteResult == CorrectionReviewResult.NotReviewed)
                        return new RServiceResult<GanjoorPoemCorrectionViewModel>(null, $"نتیجهٔ پیشنهاد حذف مصرع {moderatedVerse.VORder} بررسی نشده است.");
                    var dbVerse = dbCorrection.VerseOrderText.Where(c => c.VORder == moderatedVerse.VORder).Single();
                    dbVerse.MarkForDeleteResult = moderatedVerse.MarkForDeleteResult;
                    dbVerse.ReviewNote = moderatedVerse.ReviewNote;
                    if (dbVerse.MarkForDeleteResult == CorrectionReviewResult.Approved)
                    {
                        if(!string.IsNullOrEmpty(dbVerse.Text))
                        {
                            dbVerse.OriginalText = dbVerse.Text;
                        }
                       
                        dbCorrection.AffectedThePoem = true;
                        versesDeleted = true;
                        updatePoem = true;

                        var poemVerse = poemVerses.Where(v => v.VOrder == moderatedVerse.VORder).Single();
                        _context.Remove(poemVerse);
                    }
                }

                bool verseAdded = false;
                if(moderation.VerseOrderText.Any(v => v.NewVerse == true))
                {
                    var addedVerses = moderation.VerseOrderText.Where(v => v.NewVerse == true).OrderBy(v => v.VORder).ToList();
                    int nextVORder = addedVerses.First().VORder;
                    bool anyVerseAdded = false;
                    foreach (var addedVerse in addedVerses)
                    {
                        if (addedVerse.NewVerseResult == CorrectionReviewResult.NotReviewed)
                        {
                            return new RServiceResult<GanjoorPoemCorrectionViewModel>(null, $"نتیجهٔ پیشنهاد اضافه شدن مصرع {addedVerse.VORder} بررسی نشده است.");
                        }
                        else
                        if (addedVerse.NewVerseResult == CorrectionReviewResult.Approved)
                        {
                            anyVerseAdded = true;
                            if (addedVerse.VORder != nextVORder)
                            {
                                return new RServiceResult<GanjoorPoemCorrectionViewModel>(null, "مصرع‌های اضافه شده تأیید شده پشت سر هم نیستند.");
                            }
                            nextVORder++;
                        }
                        var dbVerse = dbCorrection.VerseOrderText.Where(c => c.VORder == addedVerse.VORder).Single();
                        dbVerse.NewVerseResult = addedVerse.NewVerseResult;
                        dbVerse.ReviewNote = addedVerse.ReviewNote;
                    }

                    if(anyVerseAdded)
                    {
                        dbCorrection.AffectedThePoem = true;
                        verseAdded = true;
                        updatePoem = true;

                        var lastInsertedVerse = addedVerses.Where(v => v.NewVerseResult == CorrectionReviewResult.Approved).OrderBy(v => v.VORder).Last();
                        var firstInsertedVerse = addedVerses.Where(v => v.NewVerseResult == CorrectionReviewResult.Approved).OrderBy(v => v.VORder).First();
                        int newVersesCount = addedVerses.Count(v => v.NewVerseResult == CorrectionReviewResult.Approved);
                        var beforeVerse = poemVerses.Where(v => v.VOrder == firstInsertedVerse.VORder).SingleOrDefault();
                        int nextVerseOrder = beforeVerse == null ? poemVerses.Any() ? poemVerses.OrderBy(v => v.VOrder).Last().VOrder + 1 : 1 : beforeVerse.VOrder; //VOrder starts from 1
                        int insertionIndex = beforeVerse == null ? poemVerses.Count : poemVerses.IndexOf(beforeVerse);

                        foreach(var nextVerse in poemVerses.Where(v => v.VOrder >= nextVerseOrder))
                        {
                            nextVerse.VOrder += newVersesCount;
                        }

                        VersePosition versePosition = VersePosition.Right;
                        foreach (var newVerse in addedVerses.Where(v => v.NewVerseResult == CorrectionReviewResult.Approved).OrderBy(v => v.VORder).ToList())
                        {
                            newVerse.VORder = nextVerseOrder;
                            poemVerses.Insert(
                                insertionIndex,
                                new GanjoorVerse()
                                {
                                    PoemId = dbCorrection.PoemId,
                                    VOrder = newVerse.VORder,
                                    VersePosition = versePosition,
                                    Text = newVerse.Text.Replace("  ", " ").ApplyCorrectYeKe().Trim(),
                                    SectionIndex1 = beforeVerse == null ? 0 : beforeVerse.SectionIndex1,
                                    SectionIndex2 = beforeVerse == null ? null : beforeVerse.SectionIndex2,
                                    SectionIndex3 = beforeVerse == null ? null : beforeVerse.SectionIndex3,
                                    SectionIndex4 = beforeVerse == null ? null : beforeVerse.SectionIndex4,
                                });
                            nextVerseOrder++;
                            insertionIndex++;
                            versePosition = versePosition == VersePosition.Right ? VersePosition.Left : VersePosition.Right;
                        }

                    }
                    _context.UpdateRange(poemVerses);
                    await _context.SaveChangesAsync();//temporary ids should be saved
                }

                if (versesDeleted || verseAdded || versePosistionsChanged)
                {
                    var undeletedPoemVerss = poemVerses.OrderBy(v => v.VOrder).Where(v => !moderation.VerseOrderText.Any(mv => mv.VORder == v.VOrder && mv.MarkForDelete == true)).ToList();
                    for (int vOrder = 1; vOrder <= undeletedPoemVerss.Count; vOrder++)
                    {
                        if (undeletedPoemVerss[vOrder - 1].VOrder != vOrder)
                        {
                            undeletedPoemVerss[vOrder - 1].VOrder = vOrder;
                        }
                    }

                    int cIndex = -1;
                    foreach (var verse in undeletedPoemVerss)
                    {
                        if (verse.VersePosition != VersePosition.Left && verse.VersePosition != VersePosition.CenteredVerse2 && verse.VersePosition != VersePosition.Comment)
                            cIndex++;
                        if (verse.VersePosition != VersePosition.Comment)
                        {
                            verse.CoupletIndex = cIndex;
                        }
                        else
                        {
                            verse.CoupletIndex = null;
                        }
                    }
                    _context.UpdateRange(undeletedPoemVerss);
                    dbPoem.HtmlText = PrepareHtmlText(undeletedPoemVerss);
                    dbPoem.PlainText = PreparePlainText(undeletedPoemVerss, dbPoem.Title);
                    dbPage.HtmlText = dbPoem.HtmlText;
                    updatePoem = true;
                    poemVerses = undeletedPoemVerss;
                }

                if (modifiedVerses.Count > 0 || versesDeleted || verseAdded)
                {
                    foreach (var section in sections)
                    {
                        var sectionVerses = poemVerses.Where(v =>
                                (section.VerseType == VersePoemSectionType.First && v.SectionIndex1 == section.Index)
                                ||
                                (section.VerseType == VersePoemSectionType.Second && v.SectionIndex2 == section.Index)
                                ||
                                (section.VerseType == VersePoemSectionType.Third && v.SectionIndex3 == section.Index)
                                ||
                                (section.VerseType == VersePoemSectionType.Forth && v.SectionIndex4 == section.Index)
                                ).OrderBy(v => v.VOrder).ToList();
                        if (sectionVerses.Any(v => modifiedVerses.Contains(v)))
                        {
                            section.HtmlText = PrepareHtmlText(sectionVerses);
                            section.PlainText = PreparePlainText(sectionVerses);
                            section.Modified = true;
                            section.OldRhymeLetters = section.RhymeLetters ?? "";
                            try
                            {
                                var newRhyme = LanguageUtils.FindRhyme(sectionVerses);
                                if (!string.IsNullOrEmpty(newRhyme.Rhyme) &&
                                    (string.IsNullOrEmpty(section.RhymeLetters) || (!string.IsNullOrEmpty(section.RhymeLetters) && newRhyme.Rhyme.Length > section.RhymeLetters.Length))
                                    )
                                {

                                    if (section.OldRhymeLetters != newRhyme.Rhyme)
                                    {
                                        section.RhymeLetters = newRhyme.Rhyme;
                                    }
                                }
                            }
                            catch
                            {

                            }
                            _context.Update(section);
                        }
                    }
                }

                if (dbCorrection.RhymeLetters != null)
                {
                    if (moderation.RhymeLettersReviewResult == CorrectionReviewResult.NotReviewed)
                        return new RServiceResult<GanjoorPoemCorrectionViewModel>(null, "تغییرات قافیه بررسی نشده است.");
                    dbCorrection.RhymeLettersReviewResult = moderation.RhymeLettersReviewResult;
                    if (dbCorrection.RhymeLettersReviewResult == CorrectionReviewResult.Approved)
                    {
                        if (mainSection == null)
                        {
                            return new RServiceResult<GanjoorPoemCorrectionViewModel>(null, "شعر فاقد بخش اصلی برای ذخیرهٔ قافیه است.");
                        }
                        if (poemVerses.Where(v => v.VersePosition == VersePosition.Paragraph).Any())
                        {
                            return new RServiceResult<GanjoorPoemCorrectionViewModel>(null, "امکان انتساب قافیه به متون مخلوط از طریق ویرایشگر کاربر وجود ندارد.");
                        }

                        dbCorrection.AffectedThePoem = true;
                        dbCorrection.OriginalRhymeLetters = mainSection.RhymeLetters;
                        if (moderation.RhymeLetters == "")
                        {
                            mainSection.RhymeLetters = null;
                        }
                        else
                        {
                            mainSection.RhymeLetters = dbCorrection.RhymeLetters;
                        }

                        mainSection.Modified = true;
                    }
                }

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
                        dbCorrection.OriginalRhythm = mainSection.GanjoorMetre == null ? null : mainSection.GanjoorMetre.Rhythm;
                        mainSection.OldGanjoorMetreId = mainSection.GanjoorMetreId;
                        if (moderation.Rhythm == "")
                        {
                            mainSection.GanjoorMetreId = null;

                            if (sections.Any(s => s.SectionType == PoemSectionType.WholePoem && s.VerseType != VersePoemSectionType.First && s.GanjoorMetreId != null))
                            {
                                return new RServiceResult<GanjoorPoemCorrectionViewModel>(null, "حذف وزن اصلی در حالی که وزنهای دیگری منتسب شده امکان ندارد.");
                            }
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
                    dbCorrection.Rhythm2Result = moderation.Rhythm2Result;
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
                        dbCorrection.OriginalRhythm2 = secondMetreSection == null ? null : secondMetreSection.GanjoorMetre == null ? null : secondMetreSection.GanjoorMetre.Rhythm;

                        if (moderation.Rhythm2 == "")
                        {
                            if (secondMetreSection != null)
                            {
                                if (sections.Any(s => s.SectionType == PoemSectionType.WholePoem && s.VerseType != VersePoemSectionType.First && s.VerseType != VersePoemSectionType.Second && s.GanjoorMetreId != null))
                                {
                                    return new RServiceResult<GanjoorPoemCorrectionViewModel>(null, "حذف وزن دوم در حالی که وزنهای سوم یا چهارم منتسب شده امکان ندارد.");
                                }

                                //the following block enusres updating related sections near the end of the method to work correctly (_UpdateRelatedSections):
                                secondMetreSection.OldGanjoorMetreId = secondMetreSection.GanjoorMetreId;
                                secondMetreSection.OldRhymeLetters = secondMetreSection.RhymeLetters;
                                secondMetreSection.GanjoorMetreId = null;
                                secondMetreSection.RhymeLetters = null;
                                secondMetreSection.Modified = true;

                                foreach (var section in sections.Where(s => s.GanjoorMetreRefSectionIndex == secondMetreSection.Index))
                                {
                                    _context.Remove(section);
                                    section.OldGanjoorMetreId = section.GanjoorMetreId;
                                    section.OldRhymeLetters = section.RhymeLetters;
                                    section.GanjoorMetreId = null;
                                    section.RhymeLetters = null;
                                    section.Modified = true;
                                }
                                _context.Remove(secondMetreSection);

                                foreach (var verse in poemVerses)
                                {
                                    verse.SectionIndex2 = null;
                                    verse.SectionIndex3 = null;
                                    _context.Update(verse);
                                }
                            }
                        }
                        else
                        {
                            if (secondMetreSection == null)
                            {
                                maxSections++;
                                VersePoemSectionType newSectionType = sections.Where(s => s.VerseType == VersePoemSectionType.Second).Any() ? VersePoemSectionType.Third : VersePoemSectionType.Second;
                                secondMetreSection = new GanjoorPoemSection()
                                {
                                    PoemId = dbPoem.Id,
                                    PoetId = mainSection.PoetId,
                                    SectionType = PoemSectionType.WholePoem,
                                    VerseType = newSectionType,
                                    Index = maxSections,
                                    Number = maxSections + 1,
                                    GanjoorMetreId = null,
                                    RhymeLetters = mainSection.RhymeLetters,
                                    HtmlText = mainSection.HtmlText,
                                    PlainText = mainSection.PlainText,
                                    PoemFormat = mainSection.PoemFormat,
                                };
                                secondMetreSection.CoupletsCount = poemVerses.Where(v => v.VersePosition == VersePosition.Left || v.VersePosition == VersePosition.CenteredVerse1).Count();
                                _context.Add(secondMetreSection);
                                sections.Add(secondMetreSection);

                                foreach (var verse in poemVerses)
                                {
                                    if (newSectionType == VersePoemSectionType.Second)
                                        verse.SectionIndex2 = secondMetreSection.Index;
                                    else
                                        verse.SectionIndex3 = secondMetreSection.Index;
                                }

                                foreach (var secondLevelSections in sections.Where(s => s.SectionType != PoemSectionType.WholePoem && s.VerseType == VersePoemSectionType.Second).OrderBy(s => s.Index))
                                {
                                    maxSections++;
                                    var newSection = new GanjoorPoemSection()
                                    {
                                        PoemId = secondLevelSections.PoemId,
                                        PoetId = secondLevelSections.PoetId,
                                        SectionType = secondLevelSections.SectionType,
                                        VerseType = VersePoemSectionType.Forth,
                                        Index = maxSections,
                                        Number = maxSections + 1,
                                        GanjoorMetreId = null,
                                        RhymeLetters = secondLevelSections.RhymeLetters,
                                        HtmlText = secondLevelSections.HtmlText,
                                        PlainText = secondLevelSections.PlainText,
                                        PoemFormat = secondLevelSections.PoemFormat,
                                        GanjoorMetreRefSectionIndex = secondMetreSection.Index,
                                    };
                                    newSection.CoupletsCount = poemVerses.Where(v => v.SectionIndex2 == secondLevelSections.Index && (v.VersePosition == VersePosition.Left || v.VersePosition == VersePosition.CenteredVerse1)).Count();
                                    _context.Add(newSection);
                                    sections.Add(newSection);

                                    foreach (var verse in poemVerses)
                                    {
                                        if (verse.SectionIndex2 == secondLevelSections.Index)
                                        {
                                            verse.SectionIndex4 = newSection.Index;
                                        }
                                    }
                                }
                                _context.UpdateRange(poemVerses);
                                await _context.SaveChangesAsync();
                            }
                            secondMetreSection.OldGanjoorMetreId = secondMetreSection.GanjoorMetreId;

                            var metre = await _context.GanjoorMetres.AsNoTracking().Where(m => m.Rhythm == moderation.Rhythm2).SingleOrDefaultAsync();
                            if (metre == null)
                            {
                                metre = new GanjoorMetre()
                                {
                                    Rhythm = moderation.Rhythm2,
                                    VerseCount = 0
                                };
                                _context.GanjoorMetres.Add(metre);
                                await _context.SaveChangesAsync();
                            }
                            secondMetreSection.GanjoorMetreId = metre.Id;
                            secondMetreSection.Modified = secondMetreSection.GanjoorMetreId != secondMetreSection.OldGanjoorMetreId;
                            _context.Update(secondMetreSection);

                            foreach (var section in sections.Where(s => s.GanjoorMetreRefSectionIndex == secondMetreSection.Index))
                            {
                                section.GanjoorMetreId = secondMetreSection.GanjoorMetreId;
                                section.Modified = secondMetreSection.Modified;
                                _context.Update(section);
                            }
                        }
                    }
                }

                if (dbCorrection.Language != null)
                {
                    if (moderation.LanguageReviewResult == CorrectionReviewResult.NotReviewed)
                        return new RServiceResult<GanjoorPoemCorrectionViewModel>(null, "تغییرات زبان بررسی نشده است.");
                    dbCorrection.LanguageReviewResult = moderation.LanguageReviewResult;
                    if (dbCorrection.LanguageReviewResult == CorrectionReviewResult.Approved)
                    {
                        dbCorrection.OriginalLanguage = dbPoem.Language;
                        dbCorrection.Language = moderation.Language;
                        dbCorrection.AffectedThePoem = true;
                        dbPoem.Language = dbCorrection.Language;
                        updatePoem = true;

                        foreach (var section in sections)
                        {
                            section.Language = dbCorrection.Language;
                            _context.Update(section);
                        }

                        var ganjoorLanguage = await _context.GanjoorLanguages.AsNoTracking().Where(l => l.Code == moderation.Language).SingleOrDefaultAsync();
                        if (ganjoorLanguage != null)
                        {
                            var dbVerses = await _context.GanjoorVerses.Where(v => v.PoemId == dbPoem.Id).ToListAsync();
                            foreach (var dbVerse in dbVerses)
                            {
                                dbVerse.LanguageId = ganjoorLanguage.Id;
                            }
                            _context.UpdateRange(dbVerses);
                        }
                    }
                }

                if (dbCorrection.PoemFormat != null)
                {
                    if (moderation.PoemFormatReviewResult == CorrectionReviewResult.NotReviewed)
                        return new RServiceResult<GanjoorPoemCorrectionViewModel>(null, "تغییرات قالب شعر بررسی نشده است.");
                    dbCorrection.PoemFormatReviewResult = moderation.PoemFormatReviewResult;
                    if (dbCorrection.PoemFormatReviewResult == CorrectionReviewResult.Approved)
                    {
                        if (mainSection == null)
                        {
                            return new RServiceResult<GanjoorPoemCorrectionViewModel>(null, "شعر فاقد بخش اصلی برای ذخیرهٔ قالب شعر است.");
                        }
                        if (poemVerses.Where(v => v.VersePosition == VersePosition.Paragraph).Any())
                        {
                            return new RServiceResult<GanjoorPoemCorrectionViewModel>(null, "امکان انتساب قالب به متون مخلوط از طریق ویرایشگر کاربر وجود ندارد.");
                        }

                        dbCorrection.AffectedThePoem = true;
                        dbCorrection.OriginalPoemFormat = mainSection.PoemFormat;
                        mainSection.PoemFormat = dbCorrection.PoemFormat;

                        mainSection.Modified = true;
                    }
                }

                if (dbCorrection.GeoDateTags != null && dbCorrection.GeoDateTags.Any())
                {
                    foreach (var dbGeoDateTag in dbCorrection.GeoDateTags)
                    {
                        var moderatedTag = moderation.GeoDateTags?.FirstOrDefault(g => g.Id == dbGeoDateTag.Id);
                        if (moderatedTag == null || moderatedTag.Result == CorrectionReviewResult.NotReviewed)
                        {
                            return new RServiceResult<GanjoorPoemCorrectionViewModel>(null, "همهٔ برچسب‌های جغرافیایی/تاریخی پیشنهادی باید بررسی شوند.");
                        }

                        dbGeoDateTag.Result = moderatedTag.Result;
                        dbGeoDateTag.ReviewNote = moderatedTag.ReviewNote;

                        if (moderatedTag.Result == CorrectionReviewResult.Approved)
                        {
                            dbCorrection.AffectedThePoem = true;

                            if (dbGeoDateTag.MarkForDelete)
                            {
                                // approving a delete-suggestion removes the existing, already-approved tag it targets.
                                // before removing it, snapshot its data onto this correction row itself (reusing the
                                // already-existing LocationId/LunarYear/etc. columns, which are otherwise unused for a
                                // pure delete-request) - this is what lets a later "undo this correction" roll the
                                // deletion back, since the live PoemGeoDateTag row won't exist to read from any more
                                if (dbGeoDateTag.ExistingTagId != null)
                                {
                                    var existingTag = await _context.PoemGeoDateTags.Where(t => t.Id == dbGeoDateTag.ExistingTagId).SingleOrDefaultAsync();
                                    if (existingTag != null)
                                    {
                                        dbGeoDateTag.LocationId = existingTag.LocationId;
                                        dbGeoDateTag.LunarYear = existingTag.LunarYear;
                                        dbGeoDateTag.LunarMonth = existingTag.LunarMonth;
                                        dbGeoDateTag.LunarDay = existingTag.LunarDay;
                                        dbGeoDateTag.PersonId = existingTag.PersonId;
                                        dbGeoDateTag.IgnoreInCategory = existingTag.IgnoreInCategory;

                                        _context.PoemGeoDateTags.Remove(existingTag);
                                    }
                                }
                            }
                            else
                            {
                                int? approvedLocationId = dbGeoDateTag.LocationId;
                                if (approvedLocationId == null && !string.IsNullOrWhiteSpace(dbGeoDateTag.SuggestedLocationName)
                                    && dbGeoDateTag.SuggestedLatitude != null && dbGeoDateTag.SuggestedLongitude != null)
                                {
                                    // a brand new, not-yet-catalogued location - create it so the new PoemGeoDateTag can reference it
                                    var newLocation = new GanjoorGeoLocation()
                                    {
                                        Name = dbGeoDateTag.SuggestedLocationName.Trim(),
                                        Latitude = (double)dbGeoDateTag.SuggestedLatitude,
                                        Longitude = (double)dbGeoDateTag.SuggestedLongitude,
                                    };
                                    _context.GanjoorGeoLocations.Add(newLocation);
                                    await _context.SaveChangesAsync(); // need its Id below
                                    approvedLocationId = newLocation.Id;
                                }

                                int? approvedPersonId = dbGeoDateTag.PersonId;
                                if (approvedPersonId == null && !string.IsNullOrWhiteSpace(dbGeoDateTag.SuggestedPersonGraphJson))
                                {
                                    // a brand new person (and possibly their relatives/relations, some of which may
                                    // also be brand new) - resolve/create everything the JSON describes and use
                                    // whichever person node it designates as the one actually being tagged here
                                    var personGraphResult = await _MaterializePersonGraphAsync(dbGeoDateTag.SuggestedPersonGraphJson);
                                    if (!string.IsNullOrEmpty(personGraphResult.Item2))
                                    {
                                        return new RServiceResult<GanjoorPoemCorrectionViewModel>(null, personGraphResult.Item2);
                                    }
                                    approvedPersonId = personGraphResult.Item1;
                                }

                                var newTag = new PoemGeoDateTag()
                                {
                                    PoemId = dbCorrection.PoemId,
                                    // PoemGeoDateTag.CoupletIndex uses 0 for "whole poem", matching a null CoupletIndex here
                                    CoupletIndex = dbGeoDateTag.CoupletIndex ?? 0,
                                    LocationId = approvedLocationId,
                                    LunarYear = dbGeoDateTag.LunarYear,
                                    LunarMonth = dbGeoDateTag.LunarMonth,
                                    LunarDay = dbGeoDateTag.LunarDay,
                                    PersonId = approvedPersonId,
                                    IgnoreInCategory = dbGeoDateTag.IgnoreInCategory,
                                    VerifiedDate = false,
                                    MachineGenerated = false,
                                    // carry the contributor's reasoning (e.g. "this couplet indicates she is
                                    // daughter of Jamshid") onto the live tag so it can be shown wherever the
                                    // tag itself is displayed, not just during moderation
                                    Note = string.IsNullOrWhiteSpace(dbGeoDateTag.SuggestionNote) ? null : dbGeoDateTag.SuggestionNote.Trim(),
                                };
                                newTag.LunarDateTotalNumber = _PrepareLunarDateTotalNumber(newTag);
                                _context.PoemGeoDateTags.Add(newTag);
                                await _context.SaveChangesAsync(); // need its Id below

                                // record which live tag this correction produced - repurposing ExistingTagId (normally
                                // only meaningful for a delete-request) so that "undo this correction" can later find
                                // and remove this exact tag, the same way it removes a newly-added verse or a title change
                                dbGeoDateTag.ExistingTagId = newTag.Id;
                            }
                        }
                    }
                }

                if (updatePoem)
                {
                    _context.Update(dbPoem);
                    _context.Update(dbPage);
                }

                dbCorrection.Reviewed = true;
                _context.GanjoorPoemCorrections.Update(dbCorrection);
                await _context.SaveChangesAsync();

                await _notificationService.PushNotification(dbCorrection.UserId,
                                   "بررسی ویرایش پیشنهادی شما",
                                   $"با سپاس از زحمت و همت شما ویرایش پیشنهادیتان برای <a href=\"https://ganjoor.net{dbPoem.FullUrl}\" target=\"_blank\">{dbPoem.FullTitle}</a> بررسی شد.{Environment.NewLine}" +
                                   $"جهت مشاهدهٔ نتیجهٔ بررسی در میز کاربری خود بخش «<a href=\"https://ganjoor.net/User/Edits\">ویرایش‌های من</a>» را مشاهده بفرمایید.{Environment.NewLine}"
                                   );

                foreach (var section in sections)
                {
                    if (section.Modified)
                    {
                        if (section.OldGanjoorMetreId != section.GanjoorMetreId || section.OldRhymeLetters != section.RhymeLetters)
                        {
                            _backgroundTaskQueue.QueueBackgroundWorkItem
                                    (
                                    async token =>
                                    {
                                        using (RMuseumDbContext inlineContext = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so context might be already been freed/collected by GC
                                        {
                                            if (section.OldGanjoorMetreId != null && !string.IsNullOrEmpty(section.OldRhymeLetters))
                                            {
                                                LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(inlineContext);
                                                var job = (await jobProgressServiceEF.NewJob($"بازسازی فهرست بخش‌های مرتبط", $"M: {section.OldGanjoorMetreId}, G: {section.OldRhymeLetters}")).Result;

                                                try
                                                {
                                                    await _UpdateRelatedSections(inlineContext, (int)section.OldGanjoorMetreId, section.OldRhymeLetters);
                                                    await inlineContext.SaveChangesAsync();

                                                    await jobProgressServiceEF.UpdateJob(job.Id, 100, "", true);
                                                }
                                                catch (Exception exp)
                                                {
                                                    await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, exp.ToString());
                                                }
                                            }

                                            if (section.GanjoorMetreId != null && !string.IsNullOrEmpty(section.RhymeLetters))
                                            {
                                                LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(inlineContext);
                                                var job = (await jobProgressServiceEF.NewJob($"بازسازی فهرست بخش‌های مرتبط", $"M: {section.GanjoorMetreId}, G: {section.RhymeLetters}")).Result;

                                                try
                                                {
                                                    await _UpdateRelatedSections(inlineContext, (int)section.GanjoorMetreId, section.RhymeLetters);
                                                    await inlineContext.SaveChangesAsync();
                                                    await jobProgressServiceEF.UpdateJob(job.Id, 100, "", true);
                                                }
                                                catch (Exception exp)
                                                {
                                                    await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, exp.ToString());
                                                }
                                            }
                                        }
                                    });
                        }
                    }
                }

                return new RServiceResult<GanjoorPoemCorrectionViewModel>(moderation);

            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorPoemCorrectionViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// materializes a GanjoorPoemGeoDateTagCorrection.SuggestedPersonGraphJson payload: creates
        /// any brand new person nodes it describes (or reuses an existing one when a node names
        /// ExistingPersonId), then creates the kinship (GanjoorPersonRelation) and/or non-family
        /// (GanjoorPersonAffiliation) edges between them. Returns the resolved id of the "person"
        /// node (the one that ends up assigned to the tag's PersonId), or an error message.
        /// </summary>
        /// <param name="json">SuggestedPersonGraphJson value - see its doc comment for the expected shape</param>
        private async Task<Tuple<int, string>> _MaterializePersonGraphAsync(string json)
        {
            PersonGraphSuggestion graph;
            try
            {
                graph = Newtonsoft.Json.JsonConvert.DeserializeObject<PersonGraphSuggestion>(json);
            }
            catch (Exception exp)
            {
                return new Tuple<int, string>(0, $"برچسب شخصیت پیشنهادی قابل تفسیر نیست: {exp.Message}");
            }

            if (graph?.Person == null || string.IsNullOrWhiteSpace(graph.Person.LocalKey))
            {
                return new Tuple<int, string>(0, "برچسب شخصیت پیشنهادی ناقص است.");
            }

            // first pass: resolve/create every node (the tagged person plus anyone else referenced),
            // building a localKey -> real database id map for the relations pass below. A node
            // referencing another not-yet-existing node has no real id to point at until this pass
            // finishes, which is exactly why this travels as one JSON blob instead of separate rows.
            var allNodes = new List<PersonGraphNode>() { graph.Person };
            if (graph.RelatedPeople != null)
            {
                allNodes.AddRange(graph.RelatedPeople);
            }

            var localKeyToPersonId = new Dictionary<string, int>();
            foreach (var node in allNodes)
            {
                if (string.IsNullOrWhiteSpace(node.LocalKey))
                {
                    return new Tuple<int, string>(0, "یکی از افراد برچسب پیشنهادی بدون کلید محلی است.");
                }
                if (localKeyToPersonId.ContainsKey(node.LocalKey))
                {
                    continue; // same local key reused (e.g. Person also listed in RelatedPeople) - already resolved
                }

                if (node.ExistingPersonId != null)
                {
                    var existingPerson = await _context.GanjoorRelatedPersons.Where(p => p.Id == node.ExistingPersonId).AnyAsync();
                    if (!existingPerson)
                    {
                        return new Tuple<int, string>(0, $"شخصیت موجود با کد {node.ExistingPersonId} پیدا نشد.");
                    }
                    localKeyToPersonId[node.LocalKey] = node.ExistingPersonId.Value;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(node.Name))
                {
                    return new Tuple<int, string>(0, "نام یکی از افراد پیشنهادی وارد نشده است.");
                }

                var importance = PersonImportance.Normal;
                if (!string.IsNullOrWhiteSpace(node.Importance) && !Enum.TryParse<PersonImportance>(node.Importance, out importance))
                {
                    return new Tuple<int, string>(0, $"درجهٔ اهمیت «{node.Importance}» نامعتبر است.");
                }

                var gender = PersonGender.Unknown;
                if (!string.IsNullOrWhiteSpace(node.Gender) && !Enum.TryParse<PersonGender>(node.Gender, out gender))
                {
                    return new Tuple<int, string>(0, $"جنسیت «{node.Gender}» نامعتبر است.");
                }

                var newPerson = new GanjoorRelatedPerson()
                {
                    Name = node.Name.Trim(),
                    Description = node.Description,
                    WikiUrl = node.WikiUrl,
                    BirthYearInLHijri = node.BirthYearInLHijri ?? 0,
                    DeathYearInLHijri = node.DeathYearInLHijri ?? 0,
                    ValidBirthDate = node.ValidBirthDate,
                    ValidDeathDate = node.ValidDeathDate,
                    BirthLocationId = node.BirthLocationId,
                    DeathLocationId = node.DeathLocationId,
                    FamilyTreeCaption = string.IsNullOrWhiteSpace(node.FamilyTreeCaption) ? null : node.FamilyTreeCaption.Trim(),
                    Importance = importance,
                    Gender = gender,
                    MachineGenerated = false,
                };
                _context.GanjoorRelatedPersons.Add(newPerson);
                await _context.SaveChangesAsync(); // need its Id below, other nodes/relations may reference it
                localKeyToPersonId[node.LocalKey] = newPerson.Id;
            }

            // second pass: now that every node has a real id, create the edges between them
            if (graph.Relations != null)
            {
                // ancestor-type (Parent/Ancestor) edges added earlier in THIS SAME batch, kept
                // separately because they're not queryable from _context until the final
                // SaveChangesAsync below - a single suggested-person-graph payload can easily
                // describe more than one generation at once (e.g. a new person plus their new
                // parent plus that parent's own already-known parent), so the cycle/parent-cap
                // checks below need to see these in-flight edges too, not just what's already live
                var newlyAddedAncestorEdges = new List<(int AncestorId, int DescendantId)>();
                var newlyAddedRelations = new List<(int Person1Id, int Person2Id, PersonRelationType RelationType)>();

                foreach (var relation in graph.Relations)
                {
                    if (!localKeyToPersonId.TryGetValue(relation.Person1 ?? "", out int person1Id) ||
                        !localKeyToPersonId.TryGetValue(relation.Person2 ?? "", out int person2Id))
                    {
                        return new Tuple<int, string>(0, "یکی از روابط پیشنهادی به شخصیتی خارج از این پیشنهاد اشاره می‌کند.");
                    }

                    if (person1Id == person2Id)
                    {
                        return new Tuple<int, string>(0, "دو طرف یک رابطه نمی‌توانند یک شخص باشند.");
                    }

                    if (string.Equals(relation.Kind, "affiliation", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!Enum.TryParse<PersonAffiliationType>(relation.AffiliationType, out var affiliationType))
                        {
                            return new Tuple<int, string>(0, $"نوع رابطهٔ غیرخویشاوندی «{relation.AffiliationType}» نامعتبر است.");
                        }
                        _context.GanjoorPersonAffiliations.Add(new GanjoorPersonAffiliation()
                        {
                            Person1Id = person1Id,
                            Person2Id = person2Id,
                            AffiliationType = affiliationType,
                            Note = relation.Note,
                        });
                    }
                    else
                    {
                        if (!Enum.TryParse<PersonRelationType>(relation.RelationType, out var relationType))
                        {
                            return new Tuple<int, string>(0, $"نوع رابطهٔ خویشاوندی «{relation.RelationType}» نامعتبر است.");
                        }

                        if (relationType == PersonRelationType.Parent || relationType == PersonRelationType.Ancestor)
                        {
                            if (await _WouldCreateAncestryCycleInGraphAsync(person1Id, person2Id, newlyAddedAncestorEdges))
                            {
                                return new Tuple<int, string>(0, "این رابطه باعث ایجاد حلقهٔ تناقض‌آمیز در شجره‌نامه می‌شود (مثلاً فردی نیای خود شناخته می‌شود).");
                            }
                        }

                        if (relationType == PersonRelationType.Parent)
                        {
                            var existingParentsCount = await _context.GanjoorPersonRelations
                                .Where(r => r.RelationType == PersonRelationType.Parent && r.Person2Id == person2Id)
                                .Select(r => r.Person1Id).Distinct().CountAsync();
                            var newParentsCount = newlyAddedAncestorEdges.Count(e => e.DescendantId == person2Id && e.AncestorId != person1Id);
                            if (existingParentsCount + newParentsCount >= 2)
                            {
                                return new Tuple<int, string>(0, "این نامبرده هم‌اکنون دو پدر/مادر دارد؛ نمی‌توان سومی را از طریق این برچسب افزود.");
                            }
                        }

                        var conflictingExisting = await _context.GanjoorPersonRelations
                            .Where(r =>
                                ((r.Person1Id == person1Id && r.Person2Id == person2Id) || (r.Person1Id == person2Id && r.Person2Id == person1Id))
                                && r.RelationType != relationType)
                            .AnyAsync();
                        var conflictingNew = newlyAddedRelations.Any(r =>
                            ((r.Person1Id == person1Id && r.Person2Id == person2Id) || (r.Person1Id == person2Id && r.Person2Id == person1Id))
                            && r.RelationType != relationType);
                        if (conflictingExisting || conflictingNew)
                        {
                            return new Tuple<int, string>(0, "نسبت دیگری با نوع متفاوت بین همین دو نفر از قبل (یا در همین پیشنهاد) ثبت شده که با این مغایرت دارد.");
                        }

                        _context.GanjoorPersonRelations.Add(new GanjoorPersonRelation()
                        {
                            Person1Id = person1Id,
                            Person2Id = person2Id,
                            RelationType = relationType,
                            DegreeHint = relation.DegreeHint,
                            Note = relation.Note,
                        });

                        if (relationType == PersonRelationType.Parent || relationType == PersonRelationType.Ancestor)
                        {
                            newlyAddedAncestorEdges.Add((person1Id, person2Id));
                        }
                        newlyAddedRelations.Add((person1Id, person2Id, relationType));
                    }
                }
                await _context.SaveChangesAsync();
            }

            return new Tuple<int, string>(localKeyToPersonId[graph.Person.LocalKey], null);
        }

        /// <summary>
        /// true if adding a directed ancestor-type edge ancestorId -&gt; descendantId (ancestorId
        /// becomes a parent/ancestor of descendantId) would create a cycle, counting both the
        /// already-live kinship graph AND any ancestor-type edges added earlier in the same
        /// in-progress _MaterializePersonGraphAsync batch (extraEdges) - mirrors
        /// GanjoorRelatedPersonService's own _WouldCreateAncestryCycleAsync, duplicated here since
        /// this runs in a different service/DbContext and also has to account for not-yet-saved,
        /// same-batch edges that the other copy never needs to.
        /// </summary>
        private async Task<bool> _WouldCreateAncestryCycleInGraphAsync(int ancestorId, int descendantId, List<(int AncestorId, int DescendantId)> extraEdges)
        {
            if (ancestorId == descendantId)
                return true;

            var edges = await _context.GanjoorPersonRelations
                .Where(r => r.RelationType == PersonRelationType.Parent || r.RelationType == PersonRelationType.Ancestor)
                .Select(r => new { r.Person1Id, r.Person2Id })
                .ToListAsync();

            var childrenOf = new Dictionary<int, List<int>>();
            void AddEdge(int parent, int child)
            {
                if (!childrenOf.TryGetValue(parent, out var list))
                {
                    list = new List<int>();
                    childrenOf[parent] = list;
                }
                list.Add(child);
            }
            foreach (var edge in edges)
            {
                AddEdge(edge.Person1Id, edge.Person2Id);
            }
            foreach (var edge in extraEdges)
            {
                AddEdge(edge.AncestorId, edge.DescendantId);
            }

            var visited = new HashSet<int>() { descendantId };
            var queue = new Queue<int>();
            queue.Enqueue(descendantId);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == ancestorId)
                    return true;
                if (!childrenOf.TryGetValue(current, out var children))
                    continue;
                foreach (var child in children)
                {
                    if (visited.Add(child))
                    {
                        queue.Enqueue(child);
                    }
                }
            }
            return false;
        }
    }
}
