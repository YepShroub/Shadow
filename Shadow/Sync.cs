using Microsoft.Extensions.Logging;

namespace Shadow;

public sealed class Sync(ILogger<Sync> log)
{
    private static readonly StringComparer _stringComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
    private readonly ILogger<Sync> _log = log;

    public void Run(string sourceRoot, string destRoot)
    {
        _log.LogInformation("Syncing {Source} to {Destination}", sourceRoot, destRoot);

        if (_stringComparer.Compare(sourceRoot, destRoot) == 0)
        {
            _log.LogInformation("Sync aborting, {Source} and {Destination} are the same.", sourceRoot, destRoot);
            return;
        }

        SyncDirectories(sourceRoot, destRoot);

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

    private bool NeedsCopy(FileInfo src, FileInfo? dest)
    {
        return dest is null || dest.Length != src.Length || dest.LastWriteTimeUtc != src.LastWriteTimeUtc;
    }

    private void SyncDirectories(string src, string dest)
    {
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            MatchType = MatchType.Simple,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint,
        };

        var destFiles = new DirectoryInfo(dest).EnumerateFiles("*", options)
            .ToDictionary(e => Path.GetRelativePath(dest, e.FullName), _stringComparer);

        foreach (var srcFile in new DirectoryInfo(src).EnumerateFiles("*", options))
        {
            var relPath = Path.GetRelativePath(src, srcFile.FullName);
            if (NeedsCopy(srcFile, destFiles.GetValueOrDefault(relPath)))
            {
                CopyFile(srcFile, destFiles.GetValueOrDefault(relPath) ?? new(Path.Combine(dest, relPath)));
            }
            destFiles.Remove(relPath);
        }

        foreach (var leftoverFile in destFiles.Values)
        {
            _log.LogInformation("Deleting {Deleted}.", leftoverFile.FullName);
            leftoverFile.Delete();
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
            if (Path.Exists(dd))
            {
                _log.LogInformation("Deleting directory {Directory}.", dd);
                Directory.Delete(dd, true);
            }
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
