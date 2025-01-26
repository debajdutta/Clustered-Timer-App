namespace ClusteredTimerApp
{
    /// <summary>
    /// Represents a persistent job that can be executed asynchronously.
    /// </summary>
    public interface IPersistentJob
    {
        /// <summary>
        /// Executes the job asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ExecuteAsync(CancellationToken cancellationToken);
    }
}
