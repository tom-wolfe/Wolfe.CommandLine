using Wolfe.CommandLine.Completions;
using Wolfe.CommandLine.Completions.Shells;

namespace Wolfe.CommandLine.Tests.Completions;

public sealed class ShellScriptSnapshotTests
{
    [Fact]
    public Task Bash() => Verify(Shell.Bash.Script("my-app"));

    [Fact]
    public Task Zsh() => Verify(Shell.Zsh.Script("my-app"));

    [Fact]
    public Task ZshCompletionFile() => Verify(Shell.Zsh.CompletionFileScript("my-app"));

    [Fact]
    public Task Fish() => Verify(Shell.Fish.Script("my-app"));

    [Fact]
    public Task Pwsh() => Verify(Shell.Pwsh.Script("my-app"));
}
