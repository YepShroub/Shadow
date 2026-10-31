using Microsoft.Extensions.Logging;

namespace Shadow;

public sealed class Sync(ILogger<Sync> log)
{
    private readonly ILogger<Sync> _log = log;

    public void Run(string sourceRoot, string destRoot)
    {

    }
}
