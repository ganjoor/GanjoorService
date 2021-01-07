using Microsoft.EntityFrameworkCore;
using RSecurityBackend.DbContext;
using RSecurityBackend.Models.Auth.Db;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Models.Generic.Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RSecurityBackend.Services.Implementation
{
    /// <summary>
    /// Long Running Job Progress Service
    /// </summary>
    public class LongRunningJobProgressServiceEF : ILongRunningJobProgressService
    {
        /// <summary>
        /// new job
        /// </summary>
        /// <param name="name"></param>
        /// <param name="step"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RLongRunningJobStatus>> NewJob(string name, string step)
        {
            try
            {
                RLongRunningJobStatus job = new RLongRunningJobStatus()
                {
                    Name = name,
                    Step = step,
                    Progress = 0,
                    StartTime = DateTime.Now,
                    Succeeded = false,
                    Exception = ""
                };
                _context.LongRunningJobs.Add(job);
                await _context.SaveChangesAsync();
                return new RServiceResult<RLongRunningJobStatus>(job);
            }
            catch(Exception exp)
            {
                return new RServiceResult<RLongRunningJobStatus>(null, exp.ToString());
            }
        }

        /// <summary>
        /// Get Jobs
        /// </summary>
        /// <param name="succeeded"></param>
        /// <param name="failed"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RLongRunningJobStatus[]>> GetJobs(bool succeeded = true, bool failed = true)
        {
            try
            {
                var jobs = await _context.LongRunningJobs.Where(j => (succeeded || (!succeeded && j.Succeeded == false)) && (failed || (!failed && j.Exception == ""))).OrderByDescending(j => j.StartTime).ToArrayAsync();
               
                return new RServiceResult<RLongRunningJobStatus[]>(jobs);
            }
            catch (Exception exp)
            {
                return new RServiceResult<RLongRunningJobStatus[]> (null, exp.ToString());
            }
        }

        /// <summary>
        /// Get Job By Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RLongRunningJobStatus>> GetJob(Guid id)
        {
            try
            {
                return new RServiceResult<RLongRunningJobStatus>(await _context.LongRunningJobs.Where(j => j.Id == id).SingleOrDefaultAsync());
            }
            catch (Exception exp)
            {
                return new RServiceResult<RLongRunningJobStatus>(null, exp.ToString());
            }
        }

        /// <summary>
        /// update job
        /// </summary>
        /// <param name="id"></param>
        /// <param name="progress"></param>
        /// <param name="step"></param>
        /// <param name="succeeded"></param>
        /// <param name="exception"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RLongRunningJobStatus>> UpdateJob(Guid id, double progress, string step = "", bool succeeded = false, string exception = "")
        {
            try
            {
                RLongRunningJobStatus job = await _context.LongRunningJobs.Where(j => j.Id == id).SingleOrDefaultAsync();

                if(!string.IsNullOrEmpty(step))
                {
                    job.Step = step;
                }

                if(succeeded || !string.IsNullOrEmpty(exception))
                {
                    job.EndTime = DateTime.Now;
                }

                job.Progress = progress;
                job.Succeeded = succeeded;
                job.Exception = exception;

                _context.Update(job);

                await _context.SaveChangesAsync();

                return new RServiceResult<RLongRunningJobStatus>(job);
            }
            catch (Exception exp)
            {
                return new RServiceResult<RLongRunningJobStatus>(null, exp.ToString());
            }
        }

        /// <summary>
        /// delete job
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeleteJob(Guid id)
        {
            try
            {
                RLongRunningJobStatus job = await _context.LongRunningJobs.Where(j => j.Id == id).SingleOrDefaultAsync();
                _context.LongRunningJobs.Remove(job);
                await _context.SaveChangesAsync();
                return new RServiceResult<bool>(true);
            }
            catch(Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// clean up finished jobs
        /// </summary>
        /// <param name="succeededJobs"></param>
        /// <param name="failedJobs"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> CleanUp(bool succeededJobs, bool failedJobs)
        {
            try
            {
                var jobs = await _context.LongRunningJobs.Where(j => (succeededJobs && j.Succeeded == true) || (failedJobs && j.Exception != "")).ToListAsync();
                if(jobs.Count > 0)
                {
                    _context.LongRunningJobs.RemoveRange(jobs);
                    await _context.SaveChangesAsync();
                }
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// Database Contetxt
        /// </summary>
        protected readonly RSecurityDbContext<RAppUser, RAppRole, Guid> _context;
        

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="context"></param>
        public LongRunningJobProgressServiceEF(RSecurityDbContext<RAppUser, RAppRole, Guid> context)
        {
            _context = context;
        }
    }
}
