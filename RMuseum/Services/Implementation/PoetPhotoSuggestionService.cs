using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Artifact;
using RMuseum.Models.Auth.Memory;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// poet photo suggestion service
    /// </summary>
    public class PoetPhotoSuggestionService : IPoetPhotoSuggestionService
    {
        /// <summary>
        /// return list of suggested photos for a poet
        /// </summary>
        /// <param name="poetId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPoetSuggestedPictureViewModel[]>> GetPoetSuggestedPhotosAsync(int poetId)
        {
            return new RServiceResult<GanjoorPoetSuggestedPictureViewModel[]>
                (
                  await _context.GanjoorPoetSuggestedPictures.AsNoTracking().Include(r => r.Picture)
                          .Where
                          (
                          r => r.Published == true && r.PoetId == poetId
                          )
                          .OrderBy(r => r.PicOrder)
                          .Select
                          (
                      r => new GanjoorPoetSuggestedPictureViewModel()
                      {
                          Id = r.Id,
                          PoetId = r.PoetId,
                          Title = r.Picture.Title,
                          Description = r.Picture.Description,
                          PicOrder = r.PicOrder,
                          Published = r.Published,
                          ChosenOne = r.ChosenOne,
                          SuggestedById = r.SuggestedById,
                          ImageUrl = $"api/rimages/{r.PictureId}.jpg"
                      }
                      )
                         .ToArrayAsync()
                );
        }


        /// <summary>
        /// suggest a new photo for a poet
        /// </summary>
        /// <param name="poetId"></param>
        /// <param name="userId"></param>
        /// <param name="imageStream"></param>
        /// <param name="fileName"></param>
        /// <param name="title"></param>
        /// <param name="description"></param>
        /// <param name="srcUrl"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPoetSuggestedPictureViewModel>> SuggestPhotoForPoet(int poetId, Guid userId, Stream imageStream, string fileName, string title, string description, string srcUrl)
        {
            RServiceResult<RPictureFile> imageRes = await _pictureFileService.Add(title, description, 0, null, srcUrl, imageStream, fileName, "PoetsPhotoSuggestions");

            if (!string.IsNullOrEmpty(imageRes.ExceptionString))
            {
                return new RServiceResult<GanjoorPoetSuggestedPictureViewModel>(null, imageRes.ExceptionString);
            }
            try
            {
                var image = imageRes.Result;

                int picOrder = 1 + (await _context.GanjoorPoetSuggestedPictures.Where(p => p.PoetId == poetId).CountAsync());

                GanjoorPoetSuggestedPicture picture = new GanjoorPoetSuggestedPicture()
                {
                    PoetId = poetId,
                    PicOrder = picOrder,
                    Picture = image,
                    SuggestedById = userId,
                    Published = false,
                    ChosenOne = false
                };

                _context.GanjoorPoetSuggestedPictures.Add(picture);
                await _context.SaveChangesAsync();

                var moderators = await _appUserService.GetUsersHavingPermission(RMuseumSecurableItem.GanjoorEntityShortName, RMuseumSecurableItem.ModeratePoetPhotos);
                if (string.IsNullOrEmpty(moderators.ExceptionString)) //if not, do nothing!
                {
                    var poet = await _context.GanjoorPoets.AsNoTracking().Where(p => p.Id == poetId).SingleAsync();
                    foreach (var moderator in moderators.Result)
                    {
                        await _notificationService.PushNotification
                                        (
                                            (Guid)moderator.Id,
                                            "ثبت تصویر پیشنهادی جدید برای شاعر",
                                            $"درخواستی برای ثبت تصویر پیشنهادی جدیدی برای «{poet.Nickname}» ثبت شده است. در صورت تمایل به بررسی، بخش مربوط به شاعر را <a href=\"/User/SuggestedPoetPhotos\">اینجا</a> ببینید.{ Environment.NewLine}" +
                                            $"توجه فرمایید که اگر کاربر دیگری که دارای مجوز بررسی تصاویر است پیش از شما به آن رسیدگی کرده باشد آن را در صف نخواهید دید."
                                        );
                    }
                }


                return new RServiceResult<GanjoorPoetSuggestedPictureViewModel>
                    (
                    new GanjoorPoetSuggestedPictureViewModel()
                    {
                        Id = picture.Id,
                        PoetId = poetId,
                        Title = image.Title,
                        Description = image.Description,
                        PicOrder = picture.PicOrder,
                        Published = picture.Published,
                        ChosenOne = picture.ChosenOne,
                        SuggestedById = picture.SuggestedById,
                        ImageUrl = $"api/rimages/{picture.PictureId}.jpg"
                    }
                    );
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorPoetSuggestedPictureViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// next unpublished suggested photo for poets
        /// </summary>
        /// <param name="skip"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPoetSuggestedPictureViewModel>> GetNextUnmoderatedPoetSuggestedPhotoAsync(int skip)
        {
            try
            {
                return new RServiceResult<GanjoorPoetSuggestedPictureViewModel>
                 (
                  await _context.GanjoorPoetSuggestedPictures.AsNoTracking().Include(r => r.Picture)
                          .Where
                          (
                          r => r.Published == false
                          )
                          .Skip(skip)
                          .Select
                          (
                      r => new GanjoorPoetSuggestedPictureViewModel()
                      {
                          Id = r.Id,
                          PoetId = r.PoetId,
                          Title = r.Picture.Title,
                          Description = r.Picture.Description,
                          PicOrder = r.PicOrder,
                          Published = r.Published,
                          ChosenOne = r.ChosenOne,
                          SuggestedById = r.SuggestedById,
                          ImageUrl = $"api/rimages/{r.PictureId}.jpg"
                      }
                      ).FirstOrDefaultAsync()
                 );
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorPoetSuggestedPictureViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// unpublished suggested photos count for poets
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<int>> GetNextUnmoderatedPoetSuggestedPhotosCountAsync()
        {
            try
            {
                return new RServiceResult<int>
                 (
                  await _context.GanjoorPoetSuggestedPictures
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
        /// modify a suggested photo for poets
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> ModifyPoetSuggestedPhotoAsync(GanjoorPoetSuggestedPictureViewModel model)
        {
            try
            {

                var dbModel = await _context.GanjoorPoetSuggestedPictures.Include(p => p.Picture).Where(s => s.Id == model.Id).SingleAsync();
                bool publishIsChanged = model.Published != dbModel.Published;
                dbModel.PicOrder = model.PicOrder;
                dbModel.Picture.Title = model.Title;
                dbModel.Picture.Description = model.Description;
                dbModel.ChosenOne = model.ChosenOne;
                dbModel.Published = model.Published;
                _context.Update(dbModel);
                await _context.SaveChangesAsync();

                if (publishIsChanged && model.Published && dbModel.SuggestedById != null)
                {
                    var userRes = await _appUserService.GetUserInformation((Guid)dbModel.SuggestedById);
                    var poet = await _context.GanjoorPoets.AsNoTracking().Where(p => p.Id == dbModel.PoetId).SingleAsync();
                    await _notificationService.PushNotification((Guid)dbModel.SuggestedById,
                                      $"انتشار تصویر پیشنهادی شما برای {poet.Nickname}",
                                      $"با سپاس! پیشنهاد شما برای تصویر {poet.Nickname} در فهرست تصاویر قابل انتخاب برای شاعر قابل مشاهده است."
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
        /// delete  a suggested photo for poets
        /// </summary>
        /// <param name="id"></param>
        /// <param name="deleteUserId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeletePoetSuggestedPhotoAsync(int id, Guid deleteUserId)
        {
            try
            {
                var dbModel = await _context.GanjoorPoetSuggestedPictures.Where(s => s.Id == id).SingleAsync();


                if (!dbModel.Published && dbModel.SuggestedById != null && deleteUserId != dbModel.SuggestedById)
                {
                    var userRes = await _appUserService.GetUserInformation((Guid)dbModel.SuggestedById);
                    var poet = await _context.GanjoorPoets.AsNoTracking().Where(p => p.Id == dbModel.PoetId).SingleAsync();
                    await _notificationService.PushNotification((Guid)dbModel.SuggestedById,
                                      $"عدم پذیرش تصویر ارسالی شما برای {poet.Nickname}",
                                      $"متأسفانه تصویر پیشنهادی شما برای مشخصات {poet.Nickname} مورد پذیرش قرار نگرفت" 
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
        /// Database Context
        /// </summary>
        protected readonly RMuseumDbContext _context;

        /// <summary>
        /// picture file service
        /// </summary>

        protected readonly IPictureFileService _pictureFileService;

        /// <summary>
        /// Messaging service
        /// </summary>
        protected readonly IRNotificationService _notificationService;

        /// <summary>
        /// IAppUserService instance
        /// </summary>
        protected readonly IAppUserService _appUserService;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="context"></param>
        /// <param name="pictureFileService"></param>
        /// <param name="notificationService"></param>
        /// <param name="appUserService"></param>
        public PoetPhotoSuggestionService(RMuseumDbContext context, IPictureFileService pictureFileService, IRNotificationService notificationService, IAppUserService appUserService)
        {
            _context = context;
            _pictureFileService = pictureFileService;
            _notificationService = notificationService;
            _appUserService = appUserService;
        }
    }
}
