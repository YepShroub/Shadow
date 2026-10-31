using Microsoft.Extensions.Logging;

namespace Shadow;

public sealed class SyncScheduler(Sync sync, TimeProvider time, ILogger<SyncScheduler> log)
{
    private readonly Sync _sync = sync;
    private readonly TimeProvider _time = time;
    private readonly ILogger<SyncScheduler> _log = log;

    public async Task RunAsync(string sourceRoot, string destRoot, TimeSpan inverval, CancellationToken ct)
    {

    }
}
