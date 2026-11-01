using Microsoft.Extensions.Logging;

namespace Shadow;

public sealed class Sync(ILogger<Sync> log)
{
    private readonly ILogger<Sync> _log = log;

    public void Run(string sourceRoot, string destRoot)
    {
        _log.LogInformation("Syncing {Source} to {Destination}", sourceRoot, destRoot);
    }
}
