using Microsoft.Extensions.Logging;

namespace Shadow;

public sealed class SyncScheduler(Sync sync, TimeProvider time, ILogger<SyncScheduler> log)
{
    private readonly Sync _sync = sync ?? throw new ArgumentNullException();
    private readonly TimeProvider _time = time ?? throw new ArgumentNullException();
    private readonly ILogger<SyncScheduler> _log = log;

    /// <summary>
    /// Start the scheduler, syncing the two provided directories.
    /// </summary>
    /// <param name="sourceRoot">Source directory</param>
    /// <param name="destRoot">Destination directory</param>
    /// <param name="interval">Timespan between each sync</param>
    /// <param name="ct">Cancellation token used to cancel scheduling</param>
    /// <exception cref="ArgumentNullException"><paramref name="interval">interval</paramref>'s period must be <c>InfiniteTimeSpan</c> or represent a number of milliseconds equal to or larger than 1 and smaller than <c>MaxValue</c></exception>
    public async Task RunAsync(string sourceRoot, string destRoot, TimeSpan interval, CancellationToken ct)
    {
        _log.LogInformation("Starting sync scheduler from {Source} to {Destination}, syncing every {Interval:0.0} s.", sourceRoot, destRoot, interval.TotalSeconds);

        using var timer = new PeriodicTimer(interval, _time);

        try
        {
            do
            {
                _sync.Run(sourceRoot, destRoot);
            }
            while (await timer.WaitForNextTickAsync(ct));
        }
        catch (OperationCanceledException)
        {
            _log.LogInformation("Shutdown requested.");
        }

        _log.LogInformation("Stopping.");
    }
}
