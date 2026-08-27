using System.Diagnostics;

namespace Wolfe.CommandLine.Completions.Models;

/// <summary>
/// The ambient facts install-target resolution depends on. <see cref="Detect"/> reads the real machine;
/// tests supply a fixture rooted in a temporary directory.
/// </summary>
public sealed class CompletionEnvironment
{
    /// <summary>
    /// The user's home directory.
    /// </summary>
    public required string Home { get; init; }

    /// <summary>
    /// The <c>XDG_CONFIG_HOME</c> override, when set.
    /// </summary>
    public string? XdgConfigHome { get; init; }

    /// <summary>
    /// The <c>XDG_DATA_HOME</c> override, when set.
    /// </summary>
    public string? XdgDataHome { get; init; }

    /// <summary>
    /// Returns zsh's <c>$fpath</c> entries, or empty when zsh is unavailable.
    /// </summary>
    public Func<IReadOnlyList<string>> ZshFunctionPath { get; init; } = () => [];

    /// <summary>
    /// Whether the bash-completion package (bash's lazy completion loader) is present.
    /// </summary>
    public Func<bool> BashCompletionInstalled { get; init; } = () => false;

    /// <summary>
    /// Whether this is Windows (decides the PowerShell profile location).
    /// </summary>
    public bool IsWindows { get; init; }

    internal string ConfigHome => XdgConfigHome is { Length: > 0 } ? XdgConfigHome : Path.Combine(Home, ".config");

    internal string DataHome => XdgDataHome is { Length: > 0 } ? XdgDataHome : Path.Combine(Home, ".local", "share");

    /// <summary>The real environment.</summary>
    public static CompletionEnvironment Detect() => new()
    {
        Home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        XdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME"),
        XdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME"),
        ZshFunctionPath = ProbeZshFunctionPath,
        BashCompletionInstalled = ProbeBashCompletion,
        IsWindows = OperatingSystem.IsWindows(),
    };

    private static readonly string[] BashCompletionMarkers =
    [
        "/usr/share/bash-completion/bash_completion",
        "/usr/local/share/bash-completion/bash_completion",
        "/opt/homebrew/share/bash-completion/bash_completion",
        "/etc/bash_completion",
    ];

    private static bool ProbeBashCompletion() =>
        Environment.GetEnvironmentVariable("BASH_COMPLETION_USER_DIR") is { Length: > 0 }
        || BashCompletionMarkers.Any(File.Exists);

    private static IReadOnlyList<string> ProbeZshFunctionPath()
    {
        try
        {
            var startInfo = new ProcessStartInfo("zsh")
            {
                ArgumentList = { "-c", "print -rl -- $fpath" },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return [];
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(TimeSpan.FromSeconds(5));
            return output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
        catch (Exception)
        {
            // No zsh on the PATH (or it refused to run) just means no fpath install target.
            return [];
        }
    }
}
