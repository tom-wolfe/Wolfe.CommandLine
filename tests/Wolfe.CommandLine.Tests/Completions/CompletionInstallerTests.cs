using Wolfe.CommandLine.Completions;
using Wolfe.CommandLine.Completions.Models;
using Wolfe.CommandLine.Completions.Models.Shells;

namespace Wolfe.CommandLine.Tests.Completions;

public sealed class CompletionInstallerTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("wolfe-completions-").FullName;

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private CompletionEnvironment EnvironmentWith(
        bool bashCompletion = false,
        IReadOnlyList<string>? fpath = null,
        bool isWindows = false) => new()
        {
            Home = _root,
            ZshFunctionPath = () => fpath ?? [],
            BashCompletionInstalled = () => bashCompletion,
            IsWindows = isWindows,
        };

    private CompletionInstaller CreateSut(CompletionEnvironment environment) => new("my-app", environment);

    [Fact]
    public async Task Install_Fish_WritesTheAutoLoadedCompletionFile()
    {
        // Arrange
        var sut = CreateSut(EnvironmentWith());

        // Act
        var outcome = await sut.Install(Shell.Fish, TestContext.Current.CancellationToken);

        // Assert
        outcome.Location.ShouldBe(CompletionLocation.CompletionDirectory);
        outcome.Changed.ShouldBeTrue();
        outcome.Path.ShouldBe(Path.Combine(_root, ".config", "fish", "completions", "my-app.fish"));
        File.ReadAllText(outcome.Path).ShouldBe(Shell.Fish.Script("my-app"));
    }

    [Fact]
    public async Task Install_SecondTime_ReportsNoChange()
    {
        // Arrange
        var sut = CreateSut(EnvironmentWith());
        await sut.Install(Shell.Fish, TestContext.Current.CancellationToken);

        // Act
        var second = await sut.Install(Shell.Fish, TestContext.Current.CancellationToken);

        // Assert
        second.Changed.ShouldBeFalse();
    }

    [Fact]
    public async Task Install_BashWithBashCompletion_WritesTheXdgCompletionFile()
    {
        // Arrange
        var sut = CreateSut(EnvironmentWith(bashCompletion: true));

        // Act
        var outcome = await sut.Install(Shell.Bash, TestContext.Current.CancellationToken);

        // Assert
        outcome.Location.ShouldBe(CompletionLocation.CompletionDirectory);
        outcome.Path.ShouldBe(Path.Combine(_root, ".local", "share", "bash-completion", "completions", "my-app"));
    }

    [Fact]
    public async Task Install_BashWithoutBashCompletion_FallsBackToTheStartupFile()
    {
        // Arrange
        var sut = CreateSut(EnvironmentWith(bashCompletion: false));

        // Act
        var outcome = await sut.Install(Shell.Bash, TestContext.Current.CancellationToken);

        // Assert
        outcome.Location.ShouldBe(CompletionLocation.StartupFile);
        outcome.Path.ShouldBe(Path.Combine(_root, ".bashrc"));
        File.ReadAllText(outcome.Path).ShouldContain("source <(my-app completion bash)");
    }

    [Fact]
    public async Task Install_ZshWithAWritableFpathDirectory_WritesTheAutoloadedFile()
    {
        // Arrange
        var fpath = Directory.CreateDirectory(Path.Combine(_root, "fpath")).FullName;
        var sut = CreateSut(EnvironmentWith(fpath: [fpath]));

        // Act
        var outcome = await sut.Install(Shell.Zsh, TestContext.Current.CancellationToken);

        // Assert
        outcome.Location.ShouldBe(CompletionLocation.CompletionDirectory);
        outcome.Path.ShouldBe(Path.Combine(fpath, "_my-app"));
        File.ReadAllText(outcome.Path).ShouldStartWith("#compdef my-app\n");
    }

    [Fact]
    public async Task Install_Zsh_PrefersASiteFunctionsDirectory()
    {
        // Arrange
        var plain = Directory.CreateDirectory(Path.Combine(_root, "functions")).FullName;
        var site = Directory.CreateDirectory(Path.Combine(_root, "site-functions")).FullName;
        var sut = CreateSut(EnvironmentWith(fpath: [plain, site]));

        // Act
        var outcome = await sut.Install(Shell.Zsh, TestContext.Current.CancellationToken);

        // Assert
        outcome.Path.ShouldBe(Path.Combine(site, "_my-app"));
    }

    [Fact]
    public async Task Install_ZshWithNoUsableFpath_FallsBackToTheStartupFile()
    {
        // Arrange — the sole fpath entry does not exist.
        var sut = CreateSut(EnvironmentWith(fpath: [Path.Combine(_root, "missing")]));

        // Act
        var outcome = await sut.Install(Shell.Zsh, TestContext.Current.CancellationToken);

        // Assert
        outcome.Location.ShouldBe(CompletionLocation.StartupFile);
        outcome.Path.ShouldBe(Path.Combine(_root, ".zshrc"));
    }

    [Fact]
    public async Task Install_Pwsh_AlwaysWritesTheProfileBlock()
    {
        // Arrange
        var sut = CreateSut(EnvironmentWith());

        // Act
        var outcome = await sut.Install(Shell.Pwsh, TestContext.Current.CancellationToken);

        // Assert
        outcome.Location.ShouldBe(CompletionLocation.StartupFile);
        outcome.Path.ShouldBe(Path.Combine(_root, ".config", "powershell", "Microsoft.PowerShell_profile.ps1"));
        File.ReadAllText(outcome.Path).ShouldContain("my-app completion pwsh | Out-String | Invoke-Expression");
    }

    [Fact]
    public async Task Install_PreservesExistingStartupFileContent()
    {
        // Arrange
        var bashrc = Path.Combine(_root, ".bashrc");
        await File.WriteAllTextAsync(bashrc, "export EDITOR=vim\n", TestContext.Current.CancellationToken);
        var sut = CreateSut(EnvironmentWith(bashCompletion: false));

        // Act
        await sut.Install(Shell.Bash, TestContext.Current.CancellationToken);

        // Assert
        File.ReadAllText(bashrc).ShouldStartWith("export EDITOR=vim\n");
    }

    [Fact]
    public async Task Uninstall_SweepsBothTheCompletionFileAndTheStartupBlock()
    {
        // Arrange — a completion file (from a bash-completion era) and an rc block (from before it) both exist.
        var environment = EnvironmentWith(bashCompletion: true);
        var sut = CreateSut(environment);
        await sut.Install(Shell.Bash, TestContext.Current.CancellationToken);
        await CreateSut(EnvironmentWith(bashCompletion: false)).Install(Shell.Bash, TestContext.Current.CancellationToken);

        // Act
        var outcome = await sut.Uninstall(Shell.Bash, TestContext.Current.CancellationToken);

        // Assert
        outcome.Changed.ShouldBeTrue();
        outcome.RemovedFrom.Count.ShouldBe(2);
        File.Exists(Path.Combine(_root, ".local", "share", "bash-completion", "completions", "my-app")).ShouldBeFalse();
        File.ReadAllText(Path.Combine(_root, ".bashrc")).ShouldNotContain("my-app completion");
    }

    [Fact]
    public async Task Uninstall_NothingInstalled_ReportsNoChange()
    {
        // Arrange
        var sut = CreateSut(EnvironmentWith());

        // Act
        var outcome = await sut.Uninstall(Shell.Fish, TestContext.Current.CancellationToken);

        // Assert
        outcome.Changed.ShouldBeFalse();
        outcome.RemovedFrom.ShouldBeEmpty();
    }

    [Fact]
    public async Task Uninstall_RoundTripsTheStartupFile()
    {
        // Arrange
        var profile = Path.Combine(_root, ".config", "powershell", "Microsoft.PowerShell_profile.ps1");
        Directory.CreateDirectory(Path.GetDirectoryName(profile)!);
        await File.WriteAllTextAsync(profile, "Set-Alias ll Get-ChildItem\n", TestContext.Current.CancellationToken);
        var sut = CreateSut(EnvironmentWith());
        await sut.Install(Shell.Pwsh, TestContext.Current.CancellationToken);

        // Act
        var outcome = await sut.Uninstall(Shell.Pwsh, TestContext.Current.CancellationToken);

        // Assert
        outcome.RemovedFrom.ShouldBe([profile]);
        File.ReadAllText(profile).ShouldBe("Set-Alias ll Get-ChildItem\n");
    }
}
