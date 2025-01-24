using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusteredTimerApp
{
    /// <summary>
    /// Interface for distributed locking mechanism.
    /// </summary>
    public interface IDistributedLock
    {
        /// <summary>
        /// Acquires a lock for a specified job.
        /// </summary>
        /// <param name="jobName">The name of the job to lock.</param>
        /// <param name="instanceId">The unique identifier for the instance requesting the lock.</param>
        /// <param name="lockDuration">The duration for which the lock should be held.</param>
        /// <returns>A task that represents the asynchronous operation, containing a boolean indicating if the lock was acquired.</returns>
        Task<bool> AcquireLock(string jobName, string instanceId, TimeSpan lockDuration);

        /// <summary>
        /// Releases the lock for a specified job.
        /// </summary>
        /// <param name="jobName">The name of the job to unlock.</param>
        /// <param name="instanceId">The unique identifier for the instance releasing the lock.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task ReleaseLock(string jobName, string instanceId);
    }

}
