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

    /// <summary>
    /// The <c>XDG_STATE_HOME</c> override, when set.
    /// </summary>
    public string? XdgStateHome { get; init; }

    /// <summary>
    /// The user's login shell (the <c>SHELL</c> environment variable), when known.
    /// </summary>
    public string? LoginShell { get; init; }

    /// <summary>
    /// Whether the process was launched from a PowerShell session.
    /// </summary>
    public bool RunningInPowerShell { get; init; }

    /// <summary>
    /// Whether the process is running on a CI agent.
    /// </summary>
    public bool IsContinuousIntegration { get; init; }

    internal string ConfigHome => XdgConfigHome is { Length: > 0 } ? XdgConfigHome : Path.Combine(Home, ".config");

    internal string DataHome => XdgDataHome is { Length: > 0 } ? XdgDataHome : Path.Combine(Home, ".local", "share");

    internal string StateHome => XdgStateHome is { Length: > 0 } ? XdgStateHome : Path.Combine(Home, ".local", "state");

    /// <summary>
    /// The real environment.
    /// </summary>
    public static CompletionEnvironment Detect() => new()
    {
        Home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        XdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME"),
        XdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME"),
        XdgStateHome = Environment.GetEnvironmentVariable("XDG_STATE_HOME"),
        ZshFunctionPath = ProbeZshFunctionPath,
        BashCompletionInstalled = ProbeBashCompletion,
        IsWindows = OperatingSystem.IsWindows(),
        LoginShell = Environment.GetEnvironmentVariable("SHELL"),
        // pwsh exports PSModulePath; on Windows it is a machine-wide variable, so it only signals pwsh elsewhere.
        RunningInPowerShell = !OperatingSystem.IsWindows()
            && Environment.GetEnvironmentVariable("PSModulePath") is { Length: > 0 },
        // CI covers GitHub Actions, GitLab, CircleCI, Travis; TF_BUILD is Azure DevOps; JENKINS_URL is Jenkins.
        IsContinuousIntegration = Environment.GetEnvironmentVariable("CI") is { Length: > 0 }
            || Environment.GetEnvironmentVariable("TF_BUILD") is { Length: > 0 }
            || Environment.GetEnvironmentVariable("JENKINS_URL") is { Length: > 0 },
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
