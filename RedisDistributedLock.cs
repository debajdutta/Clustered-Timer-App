using StackExchange.Redis;

namespace ClusteredTimerApp
{
    /// <summary>
    /// Represents a distributed lock using Redis.
    /// </summary>
    public class RedisDistributedLock : IDistributedLock
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly IDatabase _database;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisDistributedLock"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string for Redis.</param>
        public RedisDistributedLock(string connectionString)
        {
            _connectionMultiplexer = ConnectionMultiplexer.Connect(connectionString);
            _database = _connectionMultiplexer.GetDatabase();
        }

        /// <summary>
        /// Acquires a lock for a specified job.
        /// </summary>
        /// <param name="jobName">The name of the job to lock.</param>
        /// <param name="instanceId">The unique identifier for the instance acquiring the lock.</param>
        /// <param name="lockDuration">The duration for which the lock should be held.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the lock was acquired.</returns>
        public async Task<bool> AcquireLock(string jobName, string instanceId, TimeSpan lockDuration)
        {
            var lockKey = GetLockKey(jobName);
            var lockValue = instanceId;

            // Try to acquire the lock using SETNX (set if not exists) with an expiration time
            bool lockAcquired = await _database.StringSetAsync(lockKey, lockValue, lockDuration, When.NotExists);

            // If lock was acquired, we set the expiration time for the lock
            return lockAcquired;
        }

        /// <summary>
        /// Releases a lock for a specified job.
        /// </summary>
        /// <param name="jobName">The name of the job to unlock.</param>
        /// <param name="instanceId">The unique identifier for the instance releasing the lock.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task ReleaseLock(string jobName, string instanceId)
        {
            var lockKey = GetLockKey(jobName);
            var lockValue = instanceId;

            // Use a Lua script to ensure the lock is only released by the instance that holds it
            var script = @"
            if redis.call('GET', KEYS[1]) == ARGV[1] then
                return redis.call('DEL', KEYS[1])
            else
                return 0
            end";

            RedisResult result = await _database.ScriptEvaluateAsync(script, new RedisKey[] { lockKey }, new RedisValue[] { lockValue });

            if (result != null)
            {
                var str = result.ToString();
                if (int.TryParse(str, out int value) && value == 1)
                {
                    Console.WriteLine($"Lock for job '{jobName}' released by instance '{instanceId}'.");
                }
                else
                {
                    Console.WriteLine($"Failed to release lock for job '{jobName}' by instance '{instanceId}'. Result: {str}");
                }
            }
            else
            {
                Console.WriteLine($"Result is null for job '{jobName}' by instance '{instanceId}'.");
            }
        }

        /// <summary>
        /// Gets the lock key for a specified job.
        /// </summary>
        /// <param name="jobName">The name of the job.</param>
        /// <returns>The lock key for the job.</returns>
        private string GetLockKey(string jobName)
        {
            // Command to check keys: redis-cli KEYS "lock:*"
            return $"lock:{jobName}";
        }
    }
}
