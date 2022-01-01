using Microsoft.EntityFrameworkCore;
using RMuseum.Models.Ganjoor;
using RSecurityBackend.Models.Generic;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using RMuseum.Models.Ganjoor.ViewModels;
using System.Globalization;

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
            if (pinned.Count > 0)
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
            var fa = new CultureInfo("fa-IR");
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

                model.Poets.Sort((a, b) => fa.CompareInfo.Compare(a.Nickname, b.Nickname));//sort each century poets alphabetically

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
                        Name = "قرن سوم",
                        StartYear = 0,
                        EndYear = 299,
                        ShowInTimeLine = true,
                    },
                    new GanjoorCentury()
                    {
                        HalfCenturyOrder = 2,
                        Name = "قرن چهارم",
                        StartYear = 300,
                        EndYear = 399,
                        ShowInTimeLine = true,
                    },
                    new GanjoorCentury()
                    {
                        HalfCenturyOrder = 3,
                        Name = "قرن پنجم",
                        StartYear = 400,
                        EndYear = 499,
                        ShowInTimeLine = true,
                    },
                    new GanjoorCentury()
                    {
                        HalfCenturyOrder = 4,
                        Name = "قرن ششم",
                        StartYear = 500,
                        EndYear = 599,
                        ShowInTimeLine = true,
                    },
                    new GanjoorCentury()
                    {
                        HalfCenturyOrder = 5,
                        Name = "قرن هفتم",
                        StartYear = 600,
                        EndYear = 699,
                        ShowInTimeLine = true,
                    },
                    new GanjoorCentury()
                    {
                        HalfCenturyOrder = 6,
                        Name = "قرن هشتم",
                        StartYear = 700,
                        EndYear = 799,
                        ShowInTimeLine = true,
                    },
                     new GanjoorCentury()
                    {
                        HalfCenturyOrder = 7,
                        Name = "قرن نهم",
                        StartYear = 800,
                        EndYear = 899,
                        ShowInTimeLine = true,
                    },
                     new GanjoorCentury()
                    {
                        HalfCenturyOrder = 8,
                        Name = "قرن دهم",
                        StartYear = 900,
                        EndYear = 999,
                        ShowInTimeLine = true,
                    },
                    new GanjoorCentury()
                    {
                        HalfCenturyOrder = 9,
                        Name = "قرن یازدهم",
                        StartYear = 1000,
                        EndYear = 1099,
                        ShowInTimeLine = true,
                    },

                     new GanjoorCentury()
                    {
                        HalfCenturyOrder = 10,
                        Name = "قرن دوازدهم",
                        StartYear = 1100,
                        EndYear = 1199,
                        ShowInTimeLine = true,
                    },
                     new GanjoorCentury()
                    {
                        HalfCenturyOrder = 11,
                        Name = "قرن سیزدهم",
                        StartYear = 1200,
                        EndYear = 1299,
                        ShowInTimeLine = true,
                    },
                    new GanjoorCentury()
                    {
                        HalfCenturyOrder = 12,
                        Name = "قرن چهاردهم",
                        StartYear = 1300,
                        EndYear = 1500,
                        ShowInTimeLine = true,
                    },


                };

                var poets = await _context.GanjoorPoets.AsNoTracking().Where(p => p.Published && p.BirthYearInLHijri != 0).OrderBy(p => p.BirthYearInLHijri).ToArrayAsync();

                foreach (var poet in poets)
                {
                    GanjoorCentury period = null;


                    var firstPeriod = periods.Where(p => p.StartYear <= poet.BirthYearInLHijri).LastOrDefault();
                    var lastPeriod = periods.Where(p => p.EndYear >= poet.DeathYearInLHijri).FirstOrDefault();

                    if (firstPeriod != null)
                    {
                        period = firstPeriod;
                        if(lastPeriod != null)
                        {
                            if ((poet.DeathYearInLHijri - lastPeriod.StartYear) > (firstPeriod.EndYear - poet.BirthYearInLHijri))
                                period = lastPeriod;
                        }
                    }
                    else
                    {
                        period = lastPeriod;
                    }



                    if (period != null)
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
                    if (period.Poets != null)
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
