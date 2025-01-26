namespace ClusteredTimerApp
{
    using System;
    using System.Threading.Tasks;
    using MongoDB.Driver;

    /// <summary>
    /// Represents a distributed lock using MongoDB.
    /// </summary>
    public class MongoDbDistributedLock : IDistributedLock
    {
        private readonly IMongoCollection<LockDocument> _lockCollection;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbDistributedLock"/> class.
        /// </summary>
        /// <param name="database">The MongoDB database instance.</param>
        /// <param name="collectionName">The name of the collection to store locks.</param>
        public MongoDbDistributedLock(IMongoDatabase database, string collectionName = "locks")
        {
            _lockCollection = database.GetCollection<LockDocument>(collectionName);

            // Ensure a unique index on jobName
            var indexKeys = Builders<LockDocument>.IndexKeys.Ascending(l => l.JobName);
            var indexOptions = new CreateIndexOptions { Unique = true };
            var indexModel = new CreateIndexModel<LockDocument>(indexKeys, indexOptions);

            _lockCollection.Indexes.CreateOne(indexModel);
        }

        /// <summary>
        /// Acquires a lock for a specified job.
        /// </summary>
        /// <param name="jobName">The name of the job to lock.</param>
        /// <param name="instanceId">The unique identifier of the instance acquiring the lock.</param>
        /// <param name="lockDuration">The duration for which the lock should be held.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the lock was acquired.</returns>
        public async Task<bool> AcquireLock(string jobName, string instanceId, TimeSpan lockDuration)
        {
            var now = DateTime.UtcNow;
            var expiresAt = now.Add(lockDuration);

            try
            {
                // Attempt to insert a new lock
                var newLock = new LockDocument
                {
                    JobName = jobName,
                    LockHolder = instanceId,
                    ExpiresAt = expiresAt
                };

                await _lockCollection.InsertOneAsync(newLock);
                return true; // Lock acquired successfully
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                // Lock already exists, check if it can be taken over
                var updateResult = await _lockCollection.UpdateOneAsync(
                    filter: Builders<LockDocument>.Filter.And(
                        Builders<LockDocument>.Filter.Eq(l => l.JobName, jobName),
                        Builders<LockDocument>.Filter.Lte(l => l.ExpiresAt, now) // Lock is expired
                    ),
                    update: Builders<LockDocument>.Update
                        .Set(l => l.LockHolder, instanceId)
                        .Set(l => l.ExpiresAt, expiresAt)
                );

                return updateResult.ModifiedCount > 0; // True if the lock was updated
            }
        }

        /// <summary>
        /// Releases a lock for a specified job.
        /// </summary>
        /// <param name="jobName">The name of the job to unlock.</param>
        /// <param name="instanceId">The unique identifier of the instance releasing the lock.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task ReleaseLock(string jobName, string instanceId)
        {
            await _lockCollection.DeleteOneAsync(
                Builders<LockDocument>.Filter.And(
                    Builders<LockDocument>.Filter.Eq(l => l.JobName, jobName),
                    Builders<LockDocument>.Filter.Eq(l => l.LockHolder, instanceId)
                )
            );
        }

        private class LockDocument
        {
            public string JobName { get; set; } = null!;
            public string LockHolder { get; set; } = null!;
            public DateTime ExpiresAt { get; set; }
        }
    }

}
