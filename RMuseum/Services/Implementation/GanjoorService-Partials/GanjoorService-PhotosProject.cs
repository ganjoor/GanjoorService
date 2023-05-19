using Microsoft.EntityFrameworkCore;
using RMuseum.Models.Auth.Memory;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Auth.Memory;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Models.Image;
using System;
using System.Data;
using System.IO;
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
        /// return list of suggested spec lines
        /// </summary>
        /// <param name="poetId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPoetSuggestedSpecLineViewModel[]>> GetPoetSuggestedSpecLinesAsync(int poetId)
        {
            return new RServiceResult<GanjoorPoetSuggestedSpecLineViewModel[]>
                (
                 await _context.GanjoorPoetSuggestedSpecLines.AsNoTracking()
                         .Where
                         (
                         r => r.PoetId == poetId
                         &&
                         r.Published == true
                         )
                         .OrderBy(r => r.LineOrder)
                         .Select
                         (
                     r => new GanjoorPoetSuggestedSpecLineViewModel()
                     {
                         Id = r.Id,
                         PoetId = r.PoetId,
                         LineOrder = r.LineOrder,
                         Contents = r.Contents,
                         Published = r.Published,
                         SuggestedById = r.SuggestedById
                     }
                     )
                         .ToArrayAsync()
                );
        }

        /// <summary>
        /// returns specific suggested line for poets
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPoetSuggestedSpecLineViewModel>> GetPoetSuggestedSpecLineAsync(int id)
        {
            try
            {

                return new RServiceResult<GanjoorPoetSuggestedSpecLineViewModel>
                 (
                  await _context.GanjoorPoetSuggestedSpecLines.AsNoTracking()
                          .Where
                          (
                          r => r.Id == id
                          )
                          .Select
                          (
                      r => new GanjoorPoetSuggestedSpecLineViewModel()
                      {
                          Id = r.Id,
                          PoetId = r.PoetId,
                          LineOrder = r.LineOrder,
                          Contents = r.Contents,
                          Published = r.Published,
                          SuggestedById = r.SuggestedById
                      }
                      ).SingleAsync()
                 );
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorPoetSuggestedSpecLineViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// next unpublished suggested line for poets
        /// </summary>
        /// <param name="skip"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPoetSuggestedSpecLineViewModel>> GetNextUnmoderatedPoetSuggestedSpecLineAsync(int skip)
        {
            try
            {
                return new RServiceResult<GanjoorPoetSuggestedSpecLineViewModel>
                 (
                  await _context.GanjoorPoetSuggestedSpecLines.AsNoTracking()
                          .Where
                          (
                          r => r.Published == false
                          )
                          .Skip(skip)
                          .Select
                          (
                      r => new GanjoorPoetSuggestedSpecLineViewModel()
                      {
                          Id = r.Id,
                          PoetId = r.PoetId,
                          LineOrder = r.LineOrder,
                          Contents = r.Contents,
                          Published = r.Published,
                          SuggestedById = r.SuggestedById
                      }
                      ).FirstOrDefaultAsync()
                 );
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorPoetSuggestedSpecLineViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// upublished suggested lines count for poets
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<int>> GetNextUnmoderatedPoetSuggestedSpecLinesCountAsync()
        {
            try
            {
                return new RServiceResult<int>
                 (
                  await _context.GanjoorPoetSuggestedSpecLines
                          .Where
                          (
                          r => r.Published == false
                          )
                          .CountAsync()
                 );
            }
            catch (Exception exp)
            {
                return new RServiceResult<int>(0, exp.ToString());
            }
        }

        /// <summary>
        /// add a suggestion for poets spec lines
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>

        public async Task<RServiceResult<GanjoorPoetSuggestedSpecLineViewModel>> AddPoetSuggestedSpecLinesAsync(GanjoorPoetSuggestedSpecLineViewModel model)
        {
            try
            {
                var dbModel = new GanjoorPoetSuggestedSpecLine()
                {
                    PoetId = model.PoetId,
                    Contents = model.Contents,
                    Published = false,
                    SuggestedById = model.SuggestedById,
                };
                dbModel.LineOrder = await _context.GanjoorPoetSuggestedSpecLines.Where(s => s.PoetId == model.PoetId).CountAsync() + 1;
                _context.Add(dbModel);
                await _context.SaveChangesAsync();
                model.Published = false;
                model.Id = dbModel.Id;
                var moderators = await _appUserService.GetUsersHavingPermission(RMuseumSecurableItem.GanjoorEntityShortName, RMuseumSecurableItem.ModeratePoetPhotos);
                if (string.IsNullOrEmpty(moderators.ExceptionString)) //if not, do nothing!
                {
                    var poet = await _context.GanjoorPoets.AsNoTracking().Where(p => p.Id == model.PoetId).SingleAsync();
                    foreach (var moderator in moderators.Result)
                    {
                        await _notificationService.PushNotification
                                        (
                                            (Guid)moderator.Id,
                                            "ثبت مشخصات جدید برای سخنور",
                                            $"درخواستی برای ثبت مشخصات جدید برای «{poet.Nickname}» ثبت شده است. در صورت تمایل به بررسی، بخش مربوط به سخنور را <a href=\"/User/SuggestedPoetSpecLines\">اینجا</a> ببینید.{ Environment.NewLine}" +
                                            $"توجه فرمایید که اگر کاربر دیگری که دارای مجوز بررسی مشخصات است پیش از شما به آن رسیدگی کرده باشد آن را در صف نخواهید دید."
                                        );
                    }
                }

                return new RServiceResult<GanjoorPoetSuggestedSpecLineViewModel>(model);
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorPoetSuggestedSpecLineViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// modify a suggestion for poets spec lines
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> ModifyPoetSuggestedSpecLinesAsync(GanjoorPoetSuggestedSpecLineViewModel model)
        {
            try
            {

                var dbModel = await _context.GanjoorPoetSuggestedSpecLines.Where(s => s.Id == model.Id).SingleAsync();
                bool publishIsChanged = model.Published != dbModel.Published;
                dbModel.LineOrder = model.LineOrder;
                dbModel.Contents = model.Contents;
                dbModel.Published = model.Published;
                _context.Update(dbModel);
                await _context.SaveChangesAsync();

                if (publishIsChanged && model.Published && dbModel.SuggestedById != null)
                {
                    var userRes = await _appUserService.GetUserInformation((Guid)dbModel.SuggestedById);
                    var poet = await _context.GanjoorPoets.AsNoTracking().Where(p => p.Id == dbModel.PoetId).SingleAsync();
                    await _notificationService.PushNotification((Guid)dbModel.SuggestedById,
                                      $"پذیرش مشارکت شما در مشخصات {poet.Nickname}",
                                      $"با سپاس! پیشنهاد شما برای مشخصات {poet.Nickname} مورد پذیرش قرار گرفت. پیشنها شما: {Environment.NewLine}" +
                                      $"{model.Contents}"
                                      );
                }

                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// delete  a suggestion for poets spec lines
        /// </summary>
        /// <param name="id"></param>
        /// <param name="deleteUserId"></param>
        /// <param name="rejectionCause"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> RejectPoetSuggestedSpecLinesAsync(int id, Guid deleteUserId, string rejectionCause)
        {
            try
            {
                var dbModel = await _context.GanjoorPoetSuggestedSpecLines.Where(s => s.Id == id).SingleAsync();


                if (!dbModel.Published && dbModel.SuggestedById != null)
                {
                    var userRes = await _appUserService.GetUserInformation((Guid)dbModel.SuggestedById);
                    var poet = await _context.GanjoorPoets.AsNoTracking().Where(p => p.Id == dbModel.PoetId).SingleAsync();
                    string causePhrase = string.IsNullOrEmpty(rejectionCause) ? "" : $" به دلیل {rejectionCause} ";
                    await _notificationService.PushNotification((Guid)dbModel.SuggestedById,
                                      $"عدم پذیرش مشارکت شما در مشخصات {poet.Nickname}",
                                      $"متأسفانه پیشنهاد شما برای مشخصات {poet.Nickname}{causePhrase} مورد پذیرش قرار نگرفت. پیشنها شما: {Environment.NewLine}" +
                                      $"{dbModel.Contents}"
                                      );
                }

                _context.Remove(dbModel);
                await _context.SaveChangesAsync();

                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// delete published suggested spec line
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeletePoetSuggestedSpecLinesAsync(int id)
        {
            try
            {
                var dbModel = await _context.GanjoorPoetSuggestedSpecLines.Where(s => s.Id == id).SingleAsync();
                if (!dbModel.Published)
                {
                    return new RServiceResult<bool>(false, "برای رد مشخصات تأیید نشده از تابع reject استفاده کنید.");
                }
                _context.Remove(dbModel);
                await _context.SaveChangesAsync();
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }



    }
}