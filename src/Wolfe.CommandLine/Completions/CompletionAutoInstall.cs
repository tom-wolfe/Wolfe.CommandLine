using Wolfe.CommandLine.Completions.Models;
using Wolfe.CommandLine.Completions.Models.Shells;

namespace Wolfe.CommandLine.Completions;

/// <summary>
/// Installs tab completion for the user's current shell on app startup, without a package-manager hook:
/// silently when the install lands in a directory the shell loads automatically, behind a yes/no prompt
/// when it would edit a startup file. Runs at most once per shell (a decline is remembered), and only when
/// a person is at an interactive terminal — never on CI, in a pipe, or during a completion callback.
/// </summary>
public static class CompletionAutoInstall
{
    /// <summary>
    /// Offers completion install for this invocation of <paramref name="command"/>. Call it once at startup,
    /// before invoking the parsed command, passing the raw <paramref name="args"/>. Never throws: completion
    /// is a convenience and must not break the command the user actually ran.
    /// </summary>
    public static async Task Run(string command, IReadOnlyList<string> args, CancellationToken ct = default)
    {
        try
        {
            await Run(command, args, CompletionEnvironment.Detect(), AutoInstallConsole.System(), ct);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
        }
    }

    internal static async Task<AutoInstallOutcome> Run(
        string command,
        IReadOnlyList<string> args,
        CompletionEnvironment environment,
        AutoInstallConsole console,
        CancellationToken ct = default
    )
    {
        // A completion callback must answer with candidates only, and managing completion explicitly
        // is the user already doing this by hand.
        if (args.Any(argument => argument.StartsWith("[suggest", StringComparison.Ordinal))
            || args.FirstOrDefault() == "completion")
        {
            return AutoInstallOutcome.Skipped;
        }

        if (environment.IsContinuousIntegration || !console.IsInteractive())
        {
            return AutoInstallOutcome.Skipped;
        }

        if (Shell.DetectCurrent(environment) is not { } shell)
        {
            return AutoInstallOutcome.Skipped;
        }

        var ledger = new AutoInstallLedger(command, environment);
        if (ledger.Contains(shell))
        {
            return AutoInstallOutcome.AlreadyHandled;
        }

        var installer = new CompletionInstaller(command, environment);

        // A completion-directory install touches nothing the user owns, so it needs no permission — just a notice.
        if (shell.CompletionFilePath(command, environment) is not null)
        {
            var installed = await installer.Install(shell, ct);
            ledger.Record(shell, AutoInstallOutcome.Installed);
            console.Notify(
                $"Installed {shell} tab completion at {installed.Path}. " +
                $"Remove with `{command} completion uninstall {shell}`.");
            return AutoInstallOutcome.Installed;
        }

        var (startupFile, _) = shell.StartupFile(command, environment);
        if (!console.Confirm($"Install {shell} tab completion? This adds a managed block to {startupFile}."))
        {
            ledger.Record(shell, AutoInstallOutcome.Declined);
            console.Notify($"Skipped. Run `{command} completion install {shell}` to install later.");
            return AutoInstallOutcome.Declined;
        }

        var outcome = await installer.Install(shell, ct);
        ledger.Record(shell, AutoInstallOutcome.Installed);
        console.Notify($"Installed in {outcome.Path}. Restart your shell to enable it.");
        return AutoInstallOutcome.Installed;
    }
}
