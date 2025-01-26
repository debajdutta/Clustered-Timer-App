namespace ClusteredTimerApp
{
    /// <summary>
    /// Represents a persistent timer that executes a job at specified intervals,
    /// ensuring that only one instance of the job runs at a time across distributed systems.
    /// </summary>
    public class PersistentTimer
    {
        private readonly IDistributedLock _distributedLock;
        private readonly IPersistentJob _job;
        private readonly string _jobName;
        private readonly string _instanceId;
        private readonly TimeSpan _lockDuration;
        private readonly TimeSpan _timerInterval;
        private static bool isJobRunning = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="PersistentTimer"/> class.
        /// </summary>
        /// <param name="distributedLock">The distributed lock implementation.</param>
        /// <param name="job">The persistent job to be executed.</param>
        /// <param name="jobName">The name of the job.</param>
        /// <param name="lockDuration">The duration for which the lock is held.</param>
        /// <param name="timerInterval">The interval at which the job is executed.</param>
        public PersistentTimer(IDistributedLock distributedLock, IPersistentJob job, string jobName, TimeSpan lockDuration, TimeSpan timerInterval)
        {
            _distributedLock = distributedLock;
            _job = job;
            _jobName = jobName;
            _instanceId = Guid.NewGuid().ToString(); // Unique identifier for this instance
            _lockDuration = lockDuration;
            _timerInterval = timerInterval;
        }

        /// <summary>
        /// Starts the timer to execute the job at specified intervals.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        public void Start(CancellationToken cancellationToken)
        {
            Timer timer = new Timer(async _ => await TryExecuteJob(cancellationToken), null, TimeSpan.Zero, _timerInterval);
        }

        /// <summary>
        /// Attempts to execute the job if it is not already running and acquires the distributed lock.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        private async Task TryExecuteJob(CancellationToken cancellationToken)
        {
            if (isJobRunning)
            {
                ConsoleExtensions.Log($"Job is already running.");
                return;
            }

            isJobRunning = true;

            try
            {
                ConsoleExtensions.Log($"Instance {_instanceId} Attempting to acquire lock for job '{_jobName}'");
                if (await _distributedLock.AcquireLock(_jobName, _instanceId, _lockDuration))
                {
                    ConsoleExtensions.Log($"Instance {_instanceId} has acquired the lock.");
                    await ExecuteJobAsync(cancellationToken);
                }
            }
            finally
            {
                isJobRunning = false;
            }
        }

        /// <summary>
        /// Executes the job asynchronously and releases the lock after execution.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        private async Task ExecuteJobAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _job.ExecuteAsync(cancellationToken);
            }
            finally
            {
                await _distributedLock.ReleaseLock(_jobName, _instanceId);
                ConsoleExtensions.Log($"Instance {_instanceId} has released the lock.");
            }
        }
    }

}
