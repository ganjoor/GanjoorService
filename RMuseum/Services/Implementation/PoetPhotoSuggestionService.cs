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
            await _context.SaveChangesAsync();
            try
            {
                var image = imageRes.Result;

                int picOrder = 1 + (await _context.GanjoorPoetSuggestedPictures.Where(p => p.PoetId == poetId).CountAsync());

                GanjoorPoetSuggestedPicture picture = new GanjoorPoetSuggestedPicture()
                {
                    PoetId = poetId,
                    PicOrder = picOrder,
                    PictureId = image.Id,
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
        public async Task<RServiceResult<int>> GetNextUnmoderatedPoetSuggestedSpecLinesCountAsync()
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
