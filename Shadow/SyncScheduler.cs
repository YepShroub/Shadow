using Microsoft.Extensions.Logging;

namespace Shadow;

public sealed class SyncScheduler(Sync sync, TimeProvider time, ILogger<SyncScheduler> log)
{
    private readonly Sync _sync = sync;
    private readonly TimeProvider _time = time;
    private readonly ILogger<SyncScheduler> _log = log;

    public async Task RunAsync(string sourceRoot, string destRoot, TimeSpan interval, CancellationToken ct)
    {
        using var timer = new PeriodicTimer(interval, _time);

        _log.LogInformation("Starting.");

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
