using ClusteredTimerApp;
using MongoDB.Driver;

internal class Program
{
    public static class DistributedLockFactory
    {
        public static IDistributedLock Create(string databaseType, string connectionString)
        {
            return databaseType switch
            {
                "MySQL" => new MySqlDistributedLock(connectionString),
                "MongoDB" => new MongoDbDistributedLock(new MongoClient(connectionString).GetDatabase("TimerDB")),
                "Redis" => new RedisDistributedLock(connectionString),
                _ => throw new NotSupportedException("Unsupported database type.")
            };
        }
    }

    public static void Main(string[] args)
    {
        ConsoleExtensions.Log($"Application started.");

        // Supported Databases: MYSQL | MongoDB | Redis
        var databaseType = "Redis";
        // Sample connection string for MySQL "Server=127.0.0.1;Database=TimerDB;User=root;Password=P@ssword1234;SslMode=Required;";
        // Sample connection string from MongoDB "mongodb://localhost:27017"
        // Sample connection string for Redis "localhost:6379,abortConnect=False"
        var connectionString = "localhost:6379,abortConnect=False";

        // Select the distributed lock implementation
        var distributedLock = DistributedLockFactory.Create(databaseType, connectionString);
        ConsoleExtensions.Log($"Distributed lock created for {databaseType}.");

        // Define the job
        var job = new SampleJob();

        // Configure the PersistentTimer
        var jobName = "SampleJob";
        var lockDuration = TimeSpan.FromSeconds(30); // Lock expiration
        var timerInterval = TimeSpan.FromSeconds(10); // Interval for retrying the job

        var timer = new PersistentTimer(distributedLock, job, jobName, lockDuration, timerInterval);
        ConsoleExtensions.Log($"Persistent timer configured with Job Name: {jobName}, Lock Duration: {lockDuration.TotalSeconds} seconds, Timer Interval: {timerInterval.TotalSeconds} seconds.");

        // Start the timer
        using var cts = new CancellationTokenSource();
        ConsoleExtensions.Log($"Starting persistent timer. Press Enter to stop.");
        timer.Start(cts.Token);
        ConsoleExtensions.Log($"Timer started. Press Enter to exit.");

        // Wait for user input to stop
        Console.ReadLine();
        cts.Cancel();

        ConsoleExtensions.Log("Timer stopped.");

        ConsoleExtensions.Log($"Application stopped.");
    }
}