using Wolfe.CommandLine.Completions.Models.Shells;

namespace Wolfe.CommandLine.Completions.Models;

/// <summary>
/// The result of an uninstall: every location a completion was removed from.
/// </summary>
internal sealed record UninstallResult(Shell Shell, IReadOnlyList<string> RemovedFrom)
{
    /// <summary>Whether anything was removed.</summary>
    public bool Changed => RemovedFrom.Count > 0;
}
