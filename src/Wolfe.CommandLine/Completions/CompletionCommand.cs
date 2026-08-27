using System.CommandLine;
using Wolfe.CommandLine.Completions.Models;
using Wolfe.CommandLine.Completions.Models.Shells;

namespace Wolfe.CommandLine.Completions;

/// <summary>
/// The <c>completion</c> command group: <c>completion &lt;shell&gt;</c> emits the script (the default action);
/// <c>completion install/uninstall &lt;shell&gt;</c> manage it.
/// </summary>
public static class CompletionCommand
{
    /// <summary>
    /// Builds the <c>completion</c> command group for <paramref name="command"/>.
    /// </summary>
    public static Command Create(string command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);

        var shell = ShellArgument();

        // The subcommand names never collide with the shell names, so the positional stays unambiguous.
        var completion = new Command("completion", $"Output a shell tab-completion script for {command}, or install/uninstall it.");
        completion.Arguments.Add(shell);
        completion.Subcommands.Add(Install(command));
        completion.Subcommands.Add(Uninstall(command));

        completion.SetAction((parseResult, _) =>
        {
            Console.Out.Write(Shell.Parse(parseResult.GetValue(shell)!).Script(command));
            return Task.CompletedTask;
        });

        return completion;
    }

    /// <summary>Builds the <c>shell</c> positional accepted by <c>completion</c> and its subcommands.</summary>
    private static Argument<string> ShellArgument()
    {
        var shell = new Argument<string>("shell")
        {
            Description = $"The shell to target ({string.Join(", ", Shell.All)}).",
        };
        shell.AcceptOnlyFromAmong([.. Shell.All.Select(known => known.Name)]);
        return shell;
    }

    private static Command Install(string command)
    {
        var shell = ShellArgument();

        var install = new Command("install", "Install the completion script for the shell.");
        install.Arguments.Add(shell);

        install.SetAction(async (parseResult, cancellationToken) =>
        {
            var target = Shell.Parse(parseResult.GetValue(shell)!);
            var outcome = await new CompletionInstaller(command).Install(target, cancellationToken);
            Console.WriteLine(Describe(outcome));
        });

        return install;
    }

    private static Command Uninstall(string command)
    {
        var shell = ShellArgument();

        var uninstall = new Command("uninstall", "Remove the completion script for the shell.");
        uninstall.Arguments.Add(shell);

        uninstall.SetAction(async (parseResult, cancellationToken) =>
        {
            var target = Shell.Parse(parseResult.GetValue(shell)!);
            var outcome = await new CompletionInstaller(command).Uninstall(target, cancellationToken);
            Console.WriteLine(outcome.Changed
                ? $"Removed {outcome.Shell} completion from {string.Join(", ", outcome.RemovedFrom)}."
                : $"No {outcome.Shell} completion found.");
        });

        return uninstall;
    }

    private static string Describe(InstallResult outcome) => outcome switch
    {
        { Changed: false } => $"{outcome.Shell} completion is already installed at {outcome.Path}.",
        { Location: CompletionLocation.CompletionDirectory } => $"Installed {outcome.Shell} completion at {outcome.Path}. New shell sessions load it automatically.",
        _ => $"Installed {outcome.Shell} completion in {outcome.Path}. Restart your shell to enable it.",
    };
}
