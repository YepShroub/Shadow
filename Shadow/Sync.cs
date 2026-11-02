using Microsoft.Extensions.Logging;

namespace Shadow;

public sealed class Sync(ILogger<Sync> log)
{
    private static readonly StringComparer _stringComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
    private readonly Dictionary<string, long> _fileSizeCache = [];
    private readonly ILogger<Sync> _log = log;

    public void Run(string sourceRoot, string destRoot, DateTime lastSync)
    {
        _log.LogInformation("Syncing {Source} to {Destination}", sourceRoot, destRoot);

        if (_stringComparer.Compare(sourceRoot, destRoot) == 0)
        {
            _log.LogInformation("Sync aborting, {Source} and {Destination} are the same.", sourceRoot, destRoot);
            return;
        }

        SyncDirectories(sourceRoot, destRoot, lastSync);

        // foreach (FileInfo fi in sourceRoot.GetFiles())
        // {
        //     if (fi.LastWriteTimeUtc < lastSync && fi.Length == _fileSizeCache.GetValueOrDefault(fi.FullName, -1))
        //     {
        //         _log.LogDebug("Skipping sync of file {Source} because it hasn't changed.", fi.FullName);
        //         continue;
        //     }

        //     CopyFile(fi, new());
        // }

    }

    private void SyncDirectories(string src, string dest, DateTime? lastSync)
    {
        HashSet<string> sourcePaths = Directory.EnumerateFiles(src, "*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(src, f))
            .ToHashSet(_stringComparer);

        var deletedFiles = Directory.EnumerateFiles(dest, "*", SearchOption.AllDirectories)
            .Where(f => !sourcePaths.Contains(Path.GetRelativePath(dest, f)));

        foreach (var f in deletedFiles)
        {
            var fp = Path.Combine(dest, f);
            _log.LogInformation("Deleting {Deleted}.", fp);
            File.Delete(Path.Combine(dest, fp));
        }

        foreach (var fp in Directory.EnumerateFiles(src, "*", SearchOption.AllDirectories))
        {
            FileInfo srcFile = new(fp);
            if (srcFile.LastWriteTimeUtc < lastSync! && srcFile.Length == _fileSizeCache.GetValueOrDefault(fp, -1))
            {
                _log.LogDebug("Skipping sync of file {Source} because it hasn't changed.", fp);
                continue;
            }

            _fileSizeCache[fp] = srcFile.Length;

            CopyFile(srcFile, new(Path.Join(dest, Path.GetRelativePath(src, fp))));
        }

        HashSet<string> sourceDirs = Directory.EnumerateDirectories(src, "*", SearchOption.AllDirectories)
            .Select(d => Path.GetRelativePath(src, d))
            .ToHashSet(_stringComparer);

        HashSet<string> destDirs = Directory.EnumerateDirectories(dest, "*", SearchOption.AllDirectories)
            .Select(d => Path.GetRelativePath(dest, d))
            .ToHashSet(_stringComparer);

        foreach (var d in sourceDirs.Except(destDirs, _stringComparer))
        {
            var dd = Path.Combine(dest, d);
            _log.LogInformation("Creating empty directory {Directory}.", dd);
            Directory.CreateDirectory(dd);
        }

        foreach (var d in destDirs.Except(sourceDirs, _stringComparer))
        {
            var dd = Path.Combine(dest, d);
            _log.LogInformation("Deleting empty directory {Directory}.", dd);
            Directory.Delete(dd);
        }

    }

    private void CopyFile(FileInfo src, FileInfo dst)
    {
        Directory.CreateDirectory(dst.DirectoryName!);
        var tmp = dst + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            src.CopyTo(tmp);

            if (!dst.Exists)
            {
                _log.LogInformation("Creating file {Destination}.", dst.FullName);
            }
            _log.LogInformation("Copying {Source} -> {Destination}.", src.FullName, dst.FullName);

            new FileInfo(tmp).MoveTo(dst.FullName, true);
        }
        catch
        {
            try
            {
                File.Delete(tmp);
            }
            catch { }
            throw;
        }
    }
}
