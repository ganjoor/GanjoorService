using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// geo location service implementation
    /// </summary>
    public class GeoLocationService : IGeoLocationService
    {
        /// <summary>
        /// add new location
        /// </summary>
        /// <param name="name"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorGeoLocationViewModel>> AddLocationAsync(string name, double x, double y)
        {
            name = name == null ? "" : name.Trim();
            if (string.IsNullOrEmpty(name))
                return new RServiceResult<GanjoorGeoLocationViewModel>(null, "نام اجباری است.");
            if (null != await _context.GanjoorGeoLocations.Where(l => l.Name == name).FirstOrDefaultAsync())
                return new RServiceResult<GanjoorGeoLocationViewModel>(null, "نام  تکراری است.");
            Point point = new Point(x, y);
            if(null != await _context.GanjoorGeoLocations.Where(l => l.Location == point).FirstOrDefaultAsync())
                return new RServiceResult<GanjoorGeoLocationViewModel>(null, "مختصات  تکراری است.");
            try
            {
                GanjoorGeoLocation location = new GanjoorGeoLocation()
                {
                    Name = name,
                    Location = point
                };
                _context.GanjoorGeoLocations.Add(location);
                await _context.SaveChangesAsync();
                return new RServiceResult<GanjoorGeoLocationViewModel>
                    (
                    new GanjoorGeoLocationViewModel()
                    {
                        Id = location.Id,
                        Name = location.Name,
                        X = location.Location.X,
                        Y = location.Location.Y
                    }
                    );
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorGeoLocationViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// update an existing location
        /// </summary>
        /// <param name="updated"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> UpdateLocationAsync(GanjoorGeoLocationViewModel updated)
        {
            try
            {
                var location = await _context.GanjoorGeoLocations.Where(l => l.Id == updated.Id).SingleOrDefaultAsync();
                if (location == null)
                    return new RServiceResult<bool>(false, "اطلاعات مکان یافت نشد.");

                location.Name = updated.Name;
                location.Location.X = updated.X;
                location.Location.Y = updated.Y;
                _context.Update(location);

                await _context.SaveChangesAsync();
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// delete location
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeleteLocationAsync(int id)
        {
            try
            {
                var location = await _context.GanjoorGeoLocations.Where(l => l.Id == id).SingleOrDefaultAsync();
                if (location == null)
                    return new RServiceResult<bool>(false, "اطلاعات طرح مکان یافت نشد.");

                _context.Remove(location);

                await _context.SaveChangesAsync();
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// get location by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorGeoLocationViewModel>> GetLocationAsync(int id)
        {
            try
            {
                return new RServiceResult<GanjoorGeoLocationViewModel>
                    (
                    await _context.GanjoorGeoLocations
                    .Where(l => l.Id == id)
                    .Select(l =>
                        new GanjoorGeoLocationViewModel()
                        {
                            Id = l.Id,
                            Name = l.Name,
                            X = l.Location.X,
                            Y = l.Location.Y
                        })
                    .SingleOrDefaultAsync()
                    );
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorGeoLocationViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get all locations
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorGeoLocationViewModel[]>> GetLocationsAsync()
        {
            try
            {
                return new RServiceResult<GanjoorGeoLocationViewModel[]>
                    (
                    await _context.GanjoorGeoLocations
                    .Select(l =>
                        new GanjoorGeoLocationViewModel()
                        {
                            Id = l.Id,
                            Name = l.Name,
                            X = l.Location.X,
                            Y = l.Location.Y
                        })
                    .OrderBy(l => l.Name).ToArrayAsync()
                    );
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorGeoLocationViewModel[]>(null, exp.ToString());
            }
        }


        /// <summary>
        /// Database Context
        /// </summary>
        protected readonly RMuseumDbContext _context;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="context"></param>
        public GeoLocationService(RMuseumDbContext context)
        {
            _context = context;
        }
    }
}
