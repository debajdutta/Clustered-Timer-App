using ClusteredTimerApp;
using MongoDB.Driver;

internal class Program
{
    public static IDistributedLock GetDistributedLock(string databaseType, string connectionString)
    {
        return databaseType switch
        {
            "MySQL" => new MySqlDistributedLock(connectionString),
            "MongoDB" => new MongoDbDistributedLock(new MongoClient(connectionString).GetDatabase("TimerDB")),
            _ => throw new NotSupportedException("Unsupported database type.")
        };
    }

    public static void Main(string[] args)
    {
        ConsoleExtensions.Log($"Application started.");

        // Supported Databases: MYSQL | MongoDB
        var databaseType = "MongoDB"; 
        // Sample connection string for MySQL "Server=127.0.0.1;Database=TimerDB;User=root;Password=P@ssword1234;SslMode=Required;";
        // Sample connection string from MongoDB "mongodb://localhost:27017"
        var connectionString = "mongodb://localhost:27017";

        // Select the distributed lock implementation
        var distributedLock = GetDistributedLock(databaseType, connectionString);

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