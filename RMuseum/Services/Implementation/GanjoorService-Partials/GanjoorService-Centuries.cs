using Microsoft.EntityFrameworkCore;
using RMuseum.Models.Ganjoor;
using RSecurityBackend.Models.Generic;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using RMuseum.Models.Ganjoor.ViewModels;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {
        /// <summary>
        /// get centuries with published poets
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorCenturyViewModel[]>> GetCenturiesAsync()
        {
            var poets = (await GetPoets(true, false)).Result;
            var dbCenturies = await _context.GanjoorCenturies.AsNoTracking().Include(c => c.Poets).OrderBy(c => c.HalfCenturyOrder).ToListAsync();

            List<GanjoorCenturyViewModel> res = new List<GanjoorCenturyViewModel>();

            var pinned = await _context.GanjoorPoets.AsNoTracking().Where(p => p.PinOrder != 0).OrderBy(p => p.PinOrder).ToListAsync();
            if(pinned.Count > 0)
            {
                GanjoorCenturyViewModel model = new GanjoorCenturyViewModel()
                {
                    Id = 0,
                    Name = "",
                    HalfCenturyOrder = 0,
                    ShowInTimeLine = false,
                    StartYear = 0,
                    EndYear = 0,
                    Poets = new List<GanjoorPoetViewModel>()
                };
                foreach (var poet in pinned)
                {
                    model.Poets.Add(poets.Where(p => p.Id == poet.Id).Single());
                }
                res.Add(model);
            }

            foreach (var dbCentury in dbCenturies)
            {
                GanjoorCenturyViewModel model = new GanjoorCenturyViewModel()
                {
                    Id = dbCentury.Id,
                    Name = dbCentury.Name,
                    HalfCenturyOrder = dbCentury.HalfCenturyOrder,
                    ShowInTimeLine = dbCentury.ShowInTimeLine,
                    StartYear = dbCentury.StartYear,
                    EndYear = dbCentury.EndYear,
                    Poets = new List<GanjoorPoetViewModel>()
                };

                foreach (var poet in dbCentury.Poets)
                {
                    model.Poets.Add(poets.Where(p => p.Id == poet.PoetId).Single());
                }

                res.Add(model);
            }

            return new RServiceResult<GanjoorCenturyViewModel[]>(res.ToArray());
        }


        /// <summary>
        /// regenerate half centuries
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> RegenerateHalfCenturiesAsync()
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM GanjoorCenturyPoet");
                var oldOnes = await _context.GanjoorCenturies.ToArrayAsync();
                _context.RemoveRange(oldOnes);
                await _context.SaveChangesAsync();
                

                var periods = new List<GanjoorCentury>
                {
                    new GanjoorCentury()
                    {
                        HalfCenturyOrder = 1,
                        Name = "۳",
                        StartYear = 0,
                        EndYear = 300,
                        ShowInTimeLine = true,
                    },
                    new GanjoorCentury()
                    {
                        HalfCenturyOrder = 2,
                        Name = "۳",
                        StartYear = 250,
                        EndYear = 350,
                        ShowInTimeLine = false,
                    },
                     new GanjoorCentury()
                    {
                        HalfCenturyOrder = 3,
                        Name = "۴",
                        StartYear = 300,
                        EndYear = 400,
                        ShowInTimeLine = true,
                    },
                    new GanjoorCentury()
                    {
                        HalfCenturyOrder = 4,
                        Name = "۴",
                        StartYear = 350,
                        EndYear = 450,
                        ShowInTimeLine = false,
                    },
                     new GanjoorCentury()
                    {
                        HalfCenturyOrder = 5,
                        Name = "۵",
                        StartYear = 400,
                        EndYear = 500,
                        ShowInTimeLine = true,
                    },
                    new GanjoorCentury()
                    {
                        HalfCenturyOrder = 6,
                        Name = "۵",
                        StartYear = 450,
                        EndYear = 550,
                        ShowInTimeLine = false,
                    },
                     new GanjoorCentury()
                    {
                        HalfCenturyOrder = 7,
                        Name = "۶",
                        StartYear = 500,
                        EndYear = 600,
                        ShowInTimeLine = true,
                    },
                    new GanjoorCentury()
                    {
                        HalfCenturyOrder = 8,
                        Name = "۶",
                        StartYear = 550,
                        EndYear = 650,
                        ShowInTimeLine = false,
                    },
                     new GanjoorCentury()
                    {
                        HalfCenturyOrder = 9,
                        Name = "۷",
                        StartYear = 600,
                        EndYear = 700,
                        ShowInTimeLine = true,
                    },
                    new GanjoorCentury()
                    {
                        HalfCenturyOrder = 10,
                        Name = "۷",
                        StartYear = 650,
                        EndYear = 750,
                        ShowInTimeLine = false,
                    },
                     new GanjoorCentury()
                    {
                        HalfCenturyOrder = 11,
                        Name = "۸",
                        StartYear = 700,
                        EndYear = 800,
                        ShowInTimeLine = true,
                    },
                    new GanjoorCentury()
                    {
                        HalfCenturyOrder = 12,
                        Name = "۸",
                        StartYear = 750,
                        EndYear = 850,
                        ShowInTimeLine = false,
                    },
                     new GanjoorCentury()
                    {
                        HalfCenturyOrder = 13,
                        Name = "۹",
                        StartYear = 800,
                        EndYear = 900,
                        ShowInTimeLine = true,
                    },
                    new GanjoorCentury()
                    {
                        HalfCenturyOrder = 14,
                        Name = "۹",
                        StartYear = 850,
                        EndYear = 950,
                        ShowInTimeLine = false,
                    },
                     new GanjoorCentury()
                    {
                        HalfCenturyOrder = 15,
                        Name = "۱۰",
                        StartYear = 900,
                        EndYear = 1000,
                        ShowInTimeLine = true,
                    },
                    new GanjoorCentury()
                    {
                        HalfCenturyOrder = 16,
                        Name = "۱۰",
                        StartYear = 950,
                        EndYear = 1050,
                        ShowInTimeLine = false,
                    },
                     new GanjoorCentury()
                    {
                        HalfCenturyOrder = 17,
                        Name = "۱۱",
                        StartYear = 1000,
                        EndYear = 1100,
                        ShowInTimeLine = true,
                    },
                    new GanjoorCentury()
                    {
                        HalfCenturyOrder = 18,
                        Name = "۱۱",
                        StartYear = 1050,
                        EndYear = 1150,
                        ShowInTimeLine = false,
                    },
                     new GanjoorCentury()
                    {
                        HalfCenturyOrder = 19,
                        Name = "۱۲",
                        StartYear = 1100,
                        EndYear = 1200,
                        ShowInTimeLine = true,
                    },
                    new GanjoorCentury()
                    {
                        HalfCenturyOrder = 20,
                        Name = "۱۲",
                        StartYear = 1150,
                        EndYear = 1250,
                        ShowInTimeLine = false,
                    },
                     new GanjoorCentury()
                    {
                        HalfCenturyOrder = 21,
                        Name = "۱۳",
                        StartYear = 1200,
                        EndYear = 1300,
                        ShowInTimeLine = true,
                    },
                    new GanjoorCentury()
                    {
                        HalfCenturyOrder = 22,
                        Name = "۱۳",
                        StartYear = 1250,
                        EndYear = 1350,
                        ShowInTimeLine = false,
                    },
                     new GanjoorCentury()
                    {
                        HalfCenturyOrder = 23,
                        Name = "۱۴",
                        StartYear = 1300,
                        EndYear = 1400,
                        ShowInTimeLine = true,
                    },
                    new GanjoorCentury()
                    {
                        HalfCenturyOrder = 24,
                        Name = "۱۴",
                        StartYear = 1350,
                        EndYear = 1450,
                        ShowInTimeLine = false,
                    },
    
                };

                var poets = await _context.GanjoorPoets.AsNoTracking().Where(p => p.Published && p.BirthYearInLHijri != 0).OrderBy(p => p.BirthYearInLHijri).ToArrayAsync();

                foreach (var poet in poets)
                {
                    GanjoorCentury period = null;


                    var firstPeriod = periods.Where(p => p.StartYear <= poet.BirthYearInLHijri).LastOrDefault();
                    var lastPeriod = periods.Where(p => p.EndYear >= poet.DeathYearInLHijri).FirstOrDefault();

                    if(firstPeriod != null)
                    {
                        period = firstPeriod;
                        if (poet.DeathYearInLHijri - lastPeriod.StartYear >= 10)
                            period = lastPeriod;
                    }
                    
                    
                    if(period != null)
                    {
                        if (period.Poets == null)
                            period.Poets = new List<GanjoorCenturyPoet>();
                        period.Poets.Add
                            (
                            new GanjoorCenturyPoet()
                            {
                                PoetOrder = period.Poets.Count,
                                PoetId = poet.Id
                            }
                            );
                    }
                }

                foreach (var period in periods)
                {
                    if(period.Poets != null)
                    {
                        _context.Add(period);
                        await _context.SaveChangesAsync();
                    }
                }


                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }
    }
}
