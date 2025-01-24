using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusteredTimerApp
{
    public class SampleJob : IPersistentJob
    {
        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                ConsoleExtensions.Log($"Executing job...");
                await Task.Delay(5000, cancellationToken); // Simulate some work
                ConsoleExtensions.Log($"Job execution completed.");
            }
            catch(Exception)
            {
                ConsoleExtensions.Log($"Job execution was canceled.");
                throw;
            }
        }
    }
}
