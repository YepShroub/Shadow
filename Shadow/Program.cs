using System.CommandLine;
using System.CommandLine.Parsing;
using Microsoft.Extensions.Logging;
using Serilog;
using Shadow;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};
CommandContext ctx = CreateRootCommand(cts);
ctx.Command.SetAction(Run);
ParseResult parseResult = ctx.Command.Parse(args);
if (ParseErrors(parseResult.Errors))
{
    return 1;
}

return parseResult.Invoke();

async Task<int> Run(ParseResult parseResult, CancellationToken ct)
{
    FileInfo? logFile = parseResult.GetValue(ctx.LogOption);
    var logConfig = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.Console(outputTemplate: "[{Timestamp:u} {Level:u3}] {Message:lj}{NewLine}{Exception}");
    if (logFile != null)
    {
        logConfig = logConfig.WriteTo.File(logFile.FullName, outputTemplate: "[{Timestamp:u} {Level:u3}] {Message:lj}{NewLine}{Exception}");
    }
    Log.Logger = logConfig.CreateLogger();

    using var logFactory = LoggerFactory.Create(builder => builder.AddSerilog(Log.Logger));

    var syncLogger = logFactory.CreateLogger<Sync>();
    var schedulerLogger = logFactory.CreateLogger<SyncScheduler>();

    DirectoryInfo sourcePath = parseResult.GetValue(ctx.SourceOption)!;
    DirectoryInfo destPath = parseResult.GetValue(ctx.DestOption)!;
    TimeSpan interval = parseResult.GetValue(ctx.IntervalOption);
    bool runOnce = parseResult.GetValue(ctx.OnceOption);

    if (sourcePath.FullName == destPath.FullName)
    {
        Log.Information("Source and destination directories are the same, exitting.");
        return 0;
    }

    Sync sync = new(syncLogger);
    if (runOnce)
    {
        sync.Run(sourcePath.FullName, destPath.FullName);
    }
    else
    {
        SyncScheduler scheduler = new(sync, TimeProvider.System, schedulerLogger);
        await scheduler.RunAsync(sourcePath.FullName, destPath.FullName, interval, ct);
    }

    return 0;
}

CommandContext CreateRootCommand(CancellationTokenSource cts)
{
    Option<DirectoryInfo> sourceOption = new("--source", "-s")
    {
        Required = true,
        Description = "Source directory.",
        Arity = ArgumentArity.ExactlyOne
    };
    sourceOption.Validators.Add(result =>
    {
        var value = result.GetValue(sourceOption)!;
        if (!value.Exists)
        {
            result.AddError($"Source directory '{value.FullName}' does not exist.");
        }
    });

    Option<DirectoryInfo> destOption = new("--destination", "-d")
    {
        Required = true,
        Description = "Destination directory.",
        Arity = ArgumentArity.ExactlyOne
    };
    destOption.Validators.Add(result =>
    {
        var value = result.GetValue(destOption)!;
        if (!value.Exists)
        {
            result.AddError($"Destination directory '{value.FullName}' does not exist.");
        }
    });

    Option<TimeSpan> intervalOption = new("--interval", "-i")
    {
        Required = true,
        Description = "Synchronization interval in seconds.",
        Arity = ArgumentArity.ExactlyOne,
        CustomParser = result =>
        {
            if (double.TryParse(result.Tokens.Single().Value, out var span) && span > 0)
            {
                try
                {
                    TimeSpan timeSpan = TimeSpan.FromSeconds(span);
                    return timeSpan;
                }
                catch { }
            }

            result.AddError("Interval must be a positive real number.");
            return TimeSpan.Zero;
        }
    };

    Option<FileInfo> logOption = new("--log", "-l")
    {
        Description = "Log file path.",
        Arity = ArgumentArity.ExactlyOne
    };

    Option<bool> onceOption = new("--once")
    {
        Description = "Sync only once.",
        Arity = ArgumentArity.ZeroOrOne
    };

    RootCommand rootCommand = [];
    rootCommand.Options.Add(sourceOption);
    rootCommand.Options.Add(destOption);
    rootCommand.Options.Add(intervalOption);
    rootCommand.Options.Add(logOption);
    rootCommand.Options.Add(onceOption);

    return new(rootCommand, sourceOption, destOption, intervalOption, logOption, onceOption);
}

static bool ParseErrors(IReadOnlyList<ParseError> errors)
{
    foreach (ParseError parseError in errors)
    {
        Console.WriteLine(parseError.Message);
    }
    return errors.Count > 0;
}

readonly record struct CommandContext(RootCommand Command, Option<DirectoryInfo> SourceOption, Option<DirectoryInfo> DestOption, Option<TimeSpan> IntervalOption, Option<FileInfo> LogOption, Option<bool> OnceOption)
{
}

