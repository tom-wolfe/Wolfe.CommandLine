using Wolfe.CommandLine.Completions.Models;
using Wolfe.CommandLine.Completions.Models.Shells;

namespace Wolfe.CommandLine.Completions;

/// <summary>
/// Remembers, that auto-install already ran for a command, so the user is never asked twice.
/// One marker file per shell under the XDG state home.
/// </summary>
internal sealed class AutoInstallLedger(string command, CompletionEnvironment environment)
{
    /// <summary>
    /// Whether auto-install has already run for <paramref name="shell"/>.
    /// </summary>
    public bool Contains(Shell shell) => File.Exists(MarkerPath(shell));

    /// <summary>
    /// Records that auto-install ran for <paramref name="shell"/> with the given outcome.
    /// </summary>
    public void Record(Shell shell, AutoInstallOutcome outcome)
    {
        var path = MarkerPath(shell);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, $"{outcome}\n");
    }

    private string MarkerPath(Shell shell) => Path.Combine(environment.StateHome, command, $"completion.{shell.Name}");
}
