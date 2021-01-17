using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RSecurityBackend.Services.Implementation
{
    /// <summary>
    /// BackgroundService implementation
    /// </summary>
    public class QueuedHostedService : BackgroundService
    {
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="taskQueue"></param>
        public QueuedHostedService(IBackgroundTaskQueue taskQueue)
        {
            TaskQueue = taskQueue;
        }

        /// <summary>
        /// task queue
        /// </summary>
        public IBackgroundTaskQueue TaskQueue { get; }

        /// <summary>
        /// execute
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected async override Task ExecuteAsync(
            CancellationToken cancellationToken)
        {

            while (!cancellationToken.IsCancellationRequested)
            {
                var workItem = await TaskQueue.DequeueAsync(cancellationToken);

                try
                {
                    await workItem(cancellationToken);
                }
                catch
                {
                }
            }

        }
    }
}
