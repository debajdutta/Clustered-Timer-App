using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusteredTimerApp
{
    using System;
    using System.Data;
    using System.Threading.Tasks;
    using MySql.Data.MySqlClient;

    /// <summary>
    /// Represents a distributed lock using MySQL.
    /// </summary>
    public class MySqlDistributedLock : IDistributedLock
    {
        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="MySqlDistributedLock"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string to the MySQL database.</param>
        public MySqlDistributedLock(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Acquires a lock for a specified job.
        /// </summary>
        /// <param name="jobName">The name of the job to lock.</param>
        /// <param name="instanceId">The unique identifier for the instance requesting the lock.</param>
        /// <param name="lockDuration">The duration for which the lock should be held.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the lock was acquired.</returns>
        public async Task<bool> AcquireLock(string jobName, string instanceId, TimeSpan lockDuration)
        {
            var now = DateTime.UtcNow;
            var expiresAt = now.Add(lockDuration);

            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var transaction = await connection.BeginTransactionAsync();

            try
            {
                // Try to insert a new lock if it doesn't exist
                var insertCommand = new MySqlCommand(
                    "INSERT INTO locks (job_name, lock_holder, expires_at) VALUES (@jobName, @instanceId, @expiresAt) " +
                    "ON DUPLICATE KEY UPDATE " +
                    "lock_holder = IF(expires_at <= @now, @instanceId, lock_holder), " +
                    "expires_at = IF(expires_at <= @now, @expiresAt, expires_at);",
                    connection, transaction);

                insertCommand.Parameters.AddWithValue("@jobName", jobName);
                insertCommand.Parameters.AddWithValue("@instanceId", instanceId);
                insertCommand.Parameters.AddWithValue("@expiresAt", expiresAt);
                insertCommand.Parameters.AddWithValue("@now", now);

                var rowsAffected = await insertCommand.ExecuteNonQueryAsync();

                // Check if the lock was acquired
                var selectCommand = new MySqlCommand(
                    "SELECT lock_holder FROM locks WHERE job_name = @jobName;",
                    connection, transaction);

                selectCommand.Parameters.AddWithValue("@jobName", jobName);

                var lockHolder = (string)(await selectCommand.ExecuteScalarAsync() ?? "");

                if (lockHolder == instanceId)
                {
                    await transaction.CommitAsync();
                    return true;
                }

                await transaction.RollbackAsync();
                return false;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Releases the lock for a specified job.
        /// </summary>
        /// <param name="jobName">The name of the job to unlock.</param>
        /// <param name="instanceId">The unique identifier for the instance releasing the lock.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task ReleaseLock(string jobName, string instanceId)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new MySqlCommand(
                "DELETE FROM locks WHERE job_name = @jobName AND lock_holder = @instanceId;",
                connection);

            command.Parameters.AddWithValue("@jobName", jobName);
            command.Parameters.AddWithValue("@instanceId", instanceId);

            await command.ExecuteNonQueryAsync();
        }
    }

}
