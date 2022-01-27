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
    }
}