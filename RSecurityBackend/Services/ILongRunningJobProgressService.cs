using RSecurityBackend.Models.Generic;
using RSecurityBackend.Models.Generic.Db;
using System;
using System.Threading.Tasks;

namespace RSecurityBackend.Services
{
    /// <summary>
    /// Long Running Job Progress Service
    /// </summary>
    public interface ILongRunningJobProgressService
    {
        /// <summary>
        /// new job
        /// </summary>
        /// <param name="name"></param>
        /// <param name="step"></param>
        /// <returns></returns>
        Task<RServiceResult<RLongRunningJobStatus>> NewJob(string name, string step);

        /// <summary>
        /// Get Job By Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<RLongRunningJobStatus>> GetJob(Guid id);

        /// <summary>
        /// Get Jobs
        /// </summary>
        /// <param name="succeeded"></param>
        /// <param name="failed"></param>
        /// <returns></returns>
        Task<RServiceResult<RLongRunningJobStatus[]>> GetJobs(bool succeeded = true, bool failed = true);


        /// <summary>
        /// update job
        /// </summary>
        /// <param name="id"></param>
        /// <param name="progress"></param>
        /// <param name="step"></param>
        /// <param name="succeeded"></param>
        /// <param name="exception"></param>
        /// <returns></returns>
        Task<RServiceResult<RLongRunningJobStatus>> UpdateJob(Guid id, double progress, string step = "", bool succeeded = false, string exception = "");

        /// <summary>
        /// delete job
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeleteJob(Guid id);

        /// <summary>
        /// clean up finished jobs
        /// </summary>
        /// <param name="succeededJobs"></param>
        /// <param name="failedJobs"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> CleanUp(bool succeededJobs, bool failedJobs);
    }
}
