using System.CommandLine;
using System.CommandLine.Completions;
using Wolfe.CommandLine.Completions;

namespace Wolfe.CommandLine.Tests.Completions;

public sealed class CompletionCommandTests
{
    private readonly RootCommand _sut = new RootCommand("Test host.").AddCompletions("my-app");

    [Theory]
    [InlineData("bash")]
    [InlineData("zsh")]
    [InlineData("fish")]
    [InlineData("pwsh")]
    public void Completion_AcceptsEachKnownShell(string shell)
        => _sut.Parse(["completion", shell]).Errors.ShouldBeEmpty();

    [Fact]
    public void Completion_RejectsAnUnknownShell()
        => _sut.Parse(["completion", "powershell"]).Errors.ShouldNotBeEmpty();

    [Fact]
    public void Completion_RequiresAShellArgument()
        => _sut.Parse(["completion"]).Errors.ShouldNotBeEmpty();

    [Theory]
    [InlineData("install")]
    [InlineData("uninstall")]
    public void Completion_AcceptsInstallAndUninstallSubcommands(string verb)
        => _sut.Parse(["completion", verb, "bash"]).Errors.ShouldBeEmpty();

    [Theory]
    [InlineData("install")]
    [InlineData("uninstall")]
    public void CompletionInstallVerbs_RequireAShell(string verb)
        => _sut.Parse(["completion", verb]).Errors.ShouldNotBeEmpty();

    [Fact]
    public void AddCompletions_RegistersTheSuggestDirective()
        // The shell scripts call `my-app [suggest:…]`; that directive must be wired on the root.
        => _sut.Directives.ShouldContain(directive => directive is SuggestDirective);

    [Fact]
    public void GetCompletions_SurfacesTheCompletionCommandItself()
    {
        // The dynamic data the scripts feed back to the shell — prove a subcommand completion is produced.
        var completions = _sut.Parse("comp").GetCompletions().Select(item => item.Label);

        completions.ShouldContain("completion");
    }

    [Fact]
    public void Create_RejectsABlankCommandName()
        => Should.Throw<ArgumentException>(() => CompletionCommand.Create(" "));
}
