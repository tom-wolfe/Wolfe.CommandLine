namespace Wolfe.CommandLine.Completions.Models;

/// <summary>
/// Where an install landed.
/// </summary>
internal enum CompletionLocation
{
    /// <summary>
    /// A script file in a directory the shell loads completions from automatically.
    /// </summary>
    CompletionDirectory,

    /// <summary>
    /// A managed block in the shell's startup file.
    /// </summary>
    StartupFile,
}
