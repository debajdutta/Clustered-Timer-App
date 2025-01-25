using MySqlX.XDevAPI.Common;
using StackExchange.Redis;

namespace ClusteredTimerApp
{
    public class RedisDistributedLock : IDistributedLock
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly IDatabase _database;

        public RedisDistributedLock(string connectionString)
        {
            _connectionMultiplexer = ConnectionMultiplexer.Connect(connectionString);
            _database = _connectionMultiplexer.GetDatabase();
        }

        public async Task<bool> AcquireLock(string jobName, string instanceId, TimeSpan lockDuration)
        {
            var lockKey = GetLockKey(jobName);
            var lockValue = instanceId;

            // Try to acquire the lock using SETNX (set if not exists) with an expiration time
            bool lockAcquired = await _database.StringSetAsync(lockKey, lockValue, lockDuration, When.NotExists);

            // If lock was acquired, we set the expiration time for the lock
            return lockAcquired;
        }

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

        private string GetLockKey(string jobName)
        {
            return $"lock:{jobName}";
        }
    }
}
