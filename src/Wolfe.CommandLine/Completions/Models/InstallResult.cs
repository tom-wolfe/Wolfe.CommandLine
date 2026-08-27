using Wolfe.CommandLine.Completions.Models.Shells;

namespace Wolfe.CommandLine.Completions.Models;

/// <summary>
/// The result of an install: where the completion now lives and whether anything was written.
/// </summary>
internal sealed record InstallResult(Shell Shell, CompletionLocation Location, string Path, bool Changed);
