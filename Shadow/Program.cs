using System.CommandLine;
using System.CommandLine.Parsing;

RootCommand rootCommand = CreateRootCommand();
ParseResult parseResult = rootCommand.Parse(args);
if (ParseErrors(parseResult.Errors))
{
    return 1;
}

// Start the scheduler...

return 0;

static RootCommand CreateRootCommand()
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

    Option<uint> intervalOption = new("--interval", "-i")
    {
        Required = true,
        Description = "Synchronization interval.",
        Arity = ArgumentArity.ExactlyOne
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

    RootCommand rootCommand = new();
    rootCommand.Options.Add(sourceOption);
    rootCommand.Options.Add(destOption);
    rootCommand.Options.Add(intervalOption);
    rootCommand.Options.Add(logOption);
    rootCommand.Options.Add(onceOption);

    return rootCommand;
}

static bool ParseErrors(IReadOnlyList<ParseError> errors)
{
    foreach (ParseError parseError in errors)
    {
        Console.WriteLine(parseError.Message);
    }
    return errors.Count > 0;
}
