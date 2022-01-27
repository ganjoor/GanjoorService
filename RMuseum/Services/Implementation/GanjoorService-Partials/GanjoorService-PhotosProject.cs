using DNTPersianUtils.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.MusicCatalogue;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Models.Generic.Db;
using RSecurityBackend.Models.Image;
using RSecurityBackend.Services.Implementation;
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
        public async Task<RServiceResult<GanjoorPoetSuggestedSpecLine[]>> GetPoetSuggestedSpecLines(int poetId, Guid? userId, bool includeUnpublished)
        {
            return new RServiceResult<GanjoorPoetSuggestedSpecLine[]>
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
                         .ToArrayAsync()
                );
        }
    }
}