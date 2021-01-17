using System;
using System.Threading;
using System.Threading.Tasks;

namespace RSecurityBackend.Services
{
    /// <summary>
    /// background task queue
    /// </summary>
    public interface IBackgroundTaskQueue
    {
        /// <summary>
        /// queue new task
        /// </summary>
        /// <param name="workItem"></param>
        void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem);

        /// <summary>
        /// get task from que
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<Func<CancellationToken, Task>> DequeueAsync(
            CancellationToken cancellationToken);

        /// <summary>
        /// number of tasks
        /// </summary>
        int Count { get; }
    }
}
