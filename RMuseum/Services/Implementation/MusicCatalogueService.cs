using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RMuseum.DbContext;
using RMuseum.Models.MusicCatalogue;
using RMuseum.Models.MusicCatalogue.ViewModels;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Models.Generic.Db;
using RSecurityBackend.Services.Implementation;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// Music Catalgue Service Implementation
    /// </summary>
    public class MusicCatalogueService : IMusicCatalogueService
    {
        /// <summary>
        /// get golha collection programs
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GolhaProgramViewModel[]>> GetGolhaCollectionPrograms(int id)
        {
            return new RServiceResult<GolhaProgramViewModel[]>
                (
                await _context.GolhaPrograms.Where(p => p.GolhaCollectionId == id).OrderBy(p => p.Id)
                    .Select(p => new GolhaProgramViewModel()
                    {
                        Id = p.Id,
                        Title = p.Title,
                        Url = p.Url,
                        ProgramOrder = p.ProgramOrder,
                        Mp3 = p.Mp3
                    }).ToArrayAsync()
                );
        }


        /// <summary>
        /// get golha program tracks
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GolhaTrackViewModel[]>> GetGolhaProgramTracks(int id)
        {
            return new RServiceResult<GolhaTrackViewModel[]>
                (
                await _context.GolhaTracks.Where(p => p.GolhaProgramId == id).OrderBy(p => p.Id)
                    .Select(p => new GolhaTrackViewModel()
                    {
                        Id = p.Id,
                        TrackNo = p.TrackNo,
                        Timing = p.Timing,
                        Title = p.Title,
                    }).ToArrayAsync()
                ); ;
        }

        /// <summary>
        /// Configuration
        /// </summary>
        protected IConfiguration Configuration { get; }

        /// <summary>
        /// Database Contetxt
        /// </summary>
        protected readonly RMuseumDbContext _context;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="context"></param>
        public MusicCatalogueService(IConfiguration configuration, RMuseumDbContext context)
        {
            Configuration = configuration;
            _context = context;
        }

    }
}
