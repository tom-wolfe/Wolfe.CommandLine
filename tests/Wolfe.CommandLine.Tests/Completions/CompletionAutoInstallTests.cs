using Wolfe.CommandLine.Completions;
using Wolfe.CommandLine.Completions.Models;

namespace Wolfe.CommandLine.Tests.Completions;

public sealed class CompletionAutoInstallTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("wolfe-auto-install-").FullName;
    private readonly List<string> _notices = [];
    private readonly List<string> _questions = [];

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private CompletionEnvironment EnvironmentWith(
        string? loginShell = "/bin/fish",
        bool bashCompletion = false,
        bool powerShell = false,
        bool continuousIntegration = false) => new()
        {
            Home = _root,
            BashCompletionInstalled = () => bashCompletion,
            LoginShell = loginShell,
            RunningInPowerShell = powerShell,
            IsContinuousIntegration = continuousIntegration,
        };

    private AutoInstallConsole ConsoleWith(bool interactive = true, bool accept = false) => new()
    {
        IsInteractive = () => interactive,
        Confirm = question =>
        {
            _questions.Add(question);
            return accept;
        },
        Notify = _notices.Add,
    };

    private static Task<AutoInstallOutcome> Run(
        CompletionEnvironment environment,
        AutoInstallConsole console,
        IReadOnlyList<string>? args = null) =>
        CompletionAutoInstall.Run("my-app", args ?? ["plan"], environment, console, TestContext.Current.CancellationToken);

    [Fact]
    public async Task SuggestCallback_IsSkipped()
    {
        // Act
        var outcome = await Run(EnvironmentWith(), ConsoleWith(), ["[suggest:8]", "my-app pl"]);

        // Assert — a tab-completion callback must answer with candidates only.
        outcome.ShouldBe(AutoInstallOutcome.Skipped);
        _notices.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExplicitCompletionCommand_IsSkipped()
    {
        // Act
        var outcome = await Run(EnvironmentWith(), ConsoleWith(), ["completion", "install", "bash"]);

        // Assert
        outcome.ShouldBe(AutoInstallOutcome.Skipped);
    }

    [Fact]
    public async Task NonInteractiveTerminal_IsSkipped()
    {
        // Act
        var outcome = await Run(EnvironmentWith(), ConsoleWith(interactive: false));

        // Assert
        outcome.ShouldBe(AutoInstallOutcome.Skipped);
        _questions.ShouldBeEmpty();
    }

    [Fact]
    public async Task ContinuousIntegration_IsSkipped()
    {
        // Act — the terminal may look interactive on a CI agent; the CI signal wins.
        var outcome = await Run(EnvironmentWith(continuousIntegration: true), ConsoleWith());

        // Assert
        outcome.ShouldBe(AutoInstallOutcome.Skipped);
    }

    [Fact]
    public async Task UnrecognisedShell_IsSkipped()
    {
        // Act
        var outcome = await Run(EnvironmentWith(loginShell: "/bin/tcsh"), ConsoleWith());

        // Assert
        outcome.ShouldBe(AutoInstallOutcome.Skipped);
    }

    [Fact]
    public async Task CompletionDirectoryShell_InstallsSilentlyWithANotice()
    {
        // Act — fish always installs into its auto-loaded completions directory.
        var outcome = await Run(EnvironmentWith(loginShell: "/usr/local/bin/fish"), ConsoleWith());

        // Assert — installed without a prompt, and the notice names the undo.
        outcome.ShouldBe(AutoInstallOutcome.Installed);
        _questions.ShouldBeEmpty();
        _notices.ShouldHaveSingleItem().ShouldContain("my-app completion uninstall fish");
        File.Exists(Path.Combine(_root, ".config", "fish", "completions", "my-app.fish")).ShouldBeTrue();
    }

    [Fact]
    public async Task StartupFileShell_AsksBeforeEditing()
    {
        // Act — bash without bash-completion falls back to a .bashrc block, which needs permission.
        var outcome = await Run(EnvironmentWith(loginShell: "/bin/bash"), ConsoleWith(accept: true));

        // Assert
        outcome.ShouldBe(AutoInstallOutcome.Installed);
        _questions.ShouldHaveSingleItem().ShouldContain(Path.Combine(_root, ".bashrc"));
        File.ReadAllText(Path.Combine(_root, ".bashrc")).ShouldContain("source <(my-app completion bash)");
    }

    [Fact]
    public async Task Decline_WritesNothingAndIsRemembered()
    {
        // Arrange
        var environment = EnvironmentWith(loginShell: "/bin/bash");

        // Act
        var first = await Run(environment, ConsoleWith(accept: false));
        var second = await Run(environment, ConsoleWith(accept: false));

        // Assert — no startup file appears, and the user is never asked twice.
        first.ShouldBe(AutoInstallOutcome.Declined);
        second.ShouldBe(AutoInstallOutcome.AlreadyHandled);
        File.Exists(Path.Combine(_root, ".bashrc")).ShouldBeFalse();
        _questions.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Install_IsRememberedAcrossRuns()
    {
        // Arrange
        var environment = EnvironmentWith(loginShell: "/usr/local/bin/fish");
        await Run(environment, ConsoleWith());

        // Act
        var second = await Run(environment, ConsoleWith());

        // Assert
        second.ShouldBe(AutoInstallOutcome.AlreadyHandled);
        _notices.Count.ShouldBe(1);
    }

    [Fact]
    public async Task PowerShellSession_TargetsPwshWhateverTheLoginShell()
    {
        // Act — pwsh on a Unix box: SHELL still names the login shell, but the session signal wins.
        var outcome = await Run(EnvironmentWith(loginShell: "/bin/zsh", powerShell: true), ConsoleWith(accept: true));

        // Assert
        outcome.ShouldBe(AutoInstallOutcome.Installed);
        File.Exists(Path.Combine(_root, ".config", "powershell", "Microsoft.PowerShell_profile.ps1")).ShouldBeTrue();
    }
}
