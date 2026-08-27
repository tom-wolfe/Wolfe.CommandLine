using Wolfe.CommandLine.Completions;
using Wolfe.CommandLine.Completions.Models.Shells;

namespace Wolfe.CommandLine.Tests.Completions;

public sealed class ShellTests
{
    [Fact]
    public void All_ContainsEveryShellOnce()
    {
        Shell.All.Count.ShouldBe(4);
        Shell.All.Select(shell => shell.Name).ShouldBe(["bash", "zsh", "fish", "pwsh"]);
    }

    [Theory]
    [InlineData("bash")]
    [InlineData("zsh")]
    [InlineData("fish")]
    [InlineData("pwsh")]
    public void Parse_KnownShell_ResolvesTheSingleton(string name)
        => Shell.Parse(name).Name.ShouldBe(name);

    [Fact]
    public void Parse_UnknownShell_Throws()
        => Should.Throw<ArgumentOutOfRangeException>(() => Shell.Parse("powershell"));

    [Theory]
    [InlineData("bash", "complete -f -F _my_app_complete my-app")]
    [InlineData("zsh", "compdef _my_app_complete my-app")]
    [InlineData("fish", "complete -c my-app")]
    [InlineData("pwsh", "Register-ArgumentCompleter -Native -CommandName my-app")]
    public void Script_RegistersAndCallsTheSuggestBackend(string name, string registration)
    {
        var script = Shell.Parse(name).Script("my-app");

        // Every script must (a) register a completer for the command and (b) drive it off the `[suggest]` directive.
        script.ShouldContain(registration);
        script.ShouldContain("[suggest:");
    }

    [Fact]
    public void CompletionFileScript_Zsh_DeclaresTheCompdefHeader()
        => Shell.Zsh.CompletionFileScript("my-app").ShouldStartWith("#compdef my-app\n");
}
