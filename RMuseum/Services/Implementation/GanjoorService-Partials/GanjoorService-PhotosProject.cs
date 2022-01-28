using Microsoft.EntityFrameworkCore;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Generic;
using System;
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
        /// return list of suggested spec lines
        /// </summary>
        /// <param name="poetId"></param>
        /// <param name="userId"></param>
        /// <param name="includeUnpublished"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPoetSuggestedSpecLineViewModel[]>> GetPoetSuggestedSpecLinesAsync(int poetId, Guid? userId, bool includeUnpublished)
        {
            return new RServiceResult<GanjoorPoetSuggestedSpecLineViewModel[]>
                (
                 await _context.GanjoorPoetSuggestedSpecLines
                         .Where
                         (
                         r => r.PoetId == poetId
                         &&
                         (includeUnpublished || r.Published == true)
                         &&
                         (userId == null || r.SuggestedById == userId)
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
                    LineOrder = model.LineOrder,
                    Contents = model.Contents,
                    Published = false,
                    SuggestedById = model.SuggestedById,
                };
                _context.Add(dbModel);
                await _context.SaveChangesAsync();
                model.Published = false;
                model.Id = dbModel.Id;
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

                if(publishIsChanged && model.Published && dbModel.SuggestedById != null)
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
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeletePoetSuggestedSpecLinesAsync(int id, Guid deleteUserId)
        {
            try
            {
                var dbModel = await _context.GanjoorPoetSuggestedSpecLines.Where(s => s.Id == id).SingleAsync();


                if (!dbModel.Published && dbModel.SuggestedById != null && deleteUserId != dbModel.SuggestedById)
                {
                    var userRes = await _appUserService.GetUserInformation((Guid)dbModel.SuggestedById);
                    var poet = await _context.GanjoorPoets.AsNoTracking().Where(p => p.Id == dbModel.PoetId).SingleAsync();
                    await _notificationService.PushNotification((Guid)dbModel.SuggestedById,
                                      $"عدم پذیرش مشارکت شما در مشخصات {poet.Nickname}",
                                      $"متأسفانه پیشنهاد شما برای مشخصات {poet.Nickname} مورد پذیرش قرار نگرفت. پیشنها شما: {Environment.NewLine}" +
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
    }
}