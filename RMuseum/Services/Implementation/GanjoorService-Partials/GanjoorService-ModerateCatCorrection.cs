using Microsoft.EntityFrameworkCore;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Generic;
using System;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {

        /// <summary>
        /// moderate cat correction
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="moderation"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorCatCorrectionViewModel>> ModerateCatCorrectionAsync(Guid userId,
            GanjoorCatCorrectionViewModel moderation)
        {
            try
            {
                var dbCorrection = await _context.GanjoorCatCorrections.Include(c => c.User)
                .Where(c => c.Id == moderation.Id)
                .FirstOrDefaultAsync();

                if (dbCorrection == null)
                    return new RServiceResult<GanjoorCatCorrectionViewModel>(null);

                dbCorrection.ReviewerUserId = userId;
                dbCorrection.ReviewDate = DateTime.Now;
                dbCorrection.ApplicationOrder = await _context.GanjoorCatCorrections.Where(c => c.Reviewed).AnyAsync() ? 1 + await _context.GanjoorCatCorrections.Where(c => c.Reviewed).MaxAsync(c => c.ApplicationOrder) : 1;

                dbCorrection.AffectedTheCat = false;
                dbCorrection.ReviewNote = moderation.ReviewNote;

                int catId = dbCorrection.CatId;

                var dbCat = await _context.GanjoorCategories.Where(c => c.Id == catId).SingleAsync();
                var dbPage = await _context.GanjoorPages.Where(p => p.GanjoorPageType == GanjoorPageType.CatPage && p.CatId == catId).SingleOrDefaultAsync();
                if (dbPage == null)
                {
                    dbPage = await _context.GanjoorPages.Where(p => p.GanjoorPageType == GanjoorPageType.PoetPage && p.PoetId == dbCat.PoetId).SingleAsync();
                }

                bool updateCat = false;
                if (dbCorrection.DescriptionHtml != null)
                {
                    if (moderation.Result == CorrectionReviewResult.NotReviewed)
                        return new RServiceResult<GanjoorCatCorrectionViewModel>(null, "تغییرات شرح بررسی نشده است.");
                    dbCorrection.Result = moderation.Result;

                    if (dbCorrection.Result == CorrectionReviewResult.Approved)
                    {

                        GanjoorPageSnapshot snapshot = new GanjoorPageSnapshot()
                        {
                            GanjoorPageId = dbPage.Id,
                            MadeObsoleteByUserId = userId,
                            RecordDate = DateTime.Now,
                            Note = $"بررسی پیشنهاد ویرایش بخش با شناسهٔ {dbCorrection.Id}",
                            Title = dbPage.Title,
                            UrlSlug = dbPage.UrlSlug,
                            HtmlText = dbPage.HtmlText,
                        };
                        _context.GanjoorPageSnapshots.Add(snapshot);
                        await _context.SaveChangesAsync();

                        dbCorrection.AffectedTheCat = true;
                        dbCat.DescriptionHtml = moderation.DescriptionHtml.Replace("ۀ", "هٔ").Replace("ك", "ک");
                        dbCat.Description = moderation.Description;

                        if(dbCat.ParentId == null)
                        {
                            var dbPoet = await _context.GanjoorPoets.Where(p => p.Id == dbCat.PoetId).SingleAsync();
                            dbPoet.Description = dbCat.Description;
                            _context.Update(dbPoet);
                        }


                        updateCat = true;
                    }
                }

   
                if (updateCat)
                {
                    _context.Update(dbCat);
                    await _context.SaveChangesAsync();
                    var tocRes = await GenerateTableOfContents(userId, dbCorrection.CatId, dbCat.TableOfContentsStyle);
                    if (!string.IsNullOrEmpty(tocRes.ExceptionString))
                    {
                        return new RServiceResult<GanjoorCatCorrectionViewModel>(null, tocRes.ExceptionString);
                    }
                    dbPage.HtmlText = tocRes.Result;
                    _context.Update(dbPage);
                }

                dbCorrection.Reviewed = true;
                _context.GanjoorCatCorrections.Update(dbCorrection);
                await _context.SaveChangesAsync();


                await _notificationService.PushNotification(dbCorrection.UserId,
                                   "بررسی ویرایش پیشنهادی شما برای بخش",
                                   $"با سپاس از زحمت و همت شما ویرایش پیشنهادیتان برای <a href=\"https://ganjoor.net{dbCat.FullUrl}\" target=\"_blank\">{dbPage.FullTitle}</a> بررسی شد.{Environment.NewLine}" +
                                   $"جهت مشاهدهٔ نتیجهٔ بررسی در میز کاربری خود بخش «<a href=\"https://ganjoor.net/User/CatEdits\">ویرایش‌های بخش‌های من</a>» را مشاهده بفرمایید.{Environment.NewLine}"
                                   );

         

                return new RServiceResult<GanjoorCatCorrectionViewModel>(moderation);

            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorCatCorrectionViewModel>(null, exp.ToString());
            }
        }
    }
}
