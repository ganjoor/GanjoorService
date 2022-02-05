using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Artifact;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Generic;
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
                  await _context.GanjoorPoetSuggestedPictures.Include(r => r.Picture)
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

        protected IPictureFileService _pictureFileService;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="context"></param>
        /// <param name="pictureFileService"></param>
        public PoetPhotoSuggestionService(RMuseumDbContext context, IPictureFileService pictureFileService)
        {
            _context = context;
            _pictureFileService = pictureFileService;
        }
    }
}
