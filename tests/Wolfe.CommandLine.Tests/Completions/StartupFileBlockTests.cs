using Wolfe.CommandLine.Completions;

namespace Wolfe.CommandLine.Tests.Completions;

public sealed class StartupFileBlockTests
{
    private const string SourceLine = "source <(my-app completion bash)";

    private readonly StartupFileBlock _sut = new("my-app");

    [Fact]
    public void Apply_EmptyFile_WritesJustTheBlock()
    {
        // Act
        var result = _sut.Apply("", SourceLine);

        // Assert
        result.ShouldBe($"{_sut.BeginMarker}\n{SourceLine}\n{_sut.EndMarker}\n");
    }

    [Fact]
    public void Apply_ExistingContent_AppendsBlockAfterABlankLine()
    {
        // Arrange
        const string existing = "export PATH=$PATH:/usr/local/bin\n";

        // Act
        var result = _sut.Apply(existing, SourceLine);

        // Assert — original content is preserved, then a blank line, then the managed block.
        result.ShouldBe(existing + "\n" + $"{_sut.BeginMarker}\n{SourceLine}\n{_sut.EndMarker}\n");
    }

    [Fact]
    public void Apply_IsIdempotent()
    {
        // Arrange
        var once = _sut.Apply("alias ll='ls -la'\n", SourceLine);

        // Act
        var twice = _sut.Apply(once, SourceLine);

        // Assert
        twice.ShouldBe(once);
    }

    [Fact]
    public void Apply_ReplacesAnExistingBlockRatherThanDuplicating()
    {
        // Arrange — a stale block (e.g. from an old version) should be rewritten in place.
        var stale = _sut.Apply("", "source <(my-app completion zsh)");

        // Act
        var result = _sut.Apply(stale, SourceLine);

        // Assert
        result.ShouldContain(SourceLine);
        result.ShouldNotContain("zsh");
        result.Split(_sut.BeginMarker).Length.ShouldBe(2); // exactly one block
    }

    [Fact]
    public void Apply_MarkersCarryTheCommandName()
    {
        // Act
        var result = _sut.Apply("", SourceLine);

        // Assert — two commands' blocks in one startup file must not collide.
        result.ShouldContain("# >>> my-app completion >>>");
        result.ShouldContain("# <<< my-app completion <<<");
    }

    [Fact]
    public void Remove_DeletesTheBlockAndTheBlankLineAdded()
    {
        // Arrange
        const string original = "export EDITOR=vim\n";
        var withBlock = _sut.Apply(original, SourceLine);

        // Act
        var result = _sut.Remove(withBlock);

        // Assert
        result.ShouldBe(original);
    }

    [Fact]
    public void Remove_WithoutABlock_LeavesContentUntouched()
    {
        // Arrange
        const string original = "export EDITOR=vim\n";

        // Act / Assert
        _sut.Remove(original).ShouldBe(original);
    }

    [Fact]
    public void Remove_AnotherCommandsBlock_IsLeftAlone()
    {
        // Arrange
        var other = new StartupFileBlock("other-app").Apply("", "source <(other-app completion bash)");

        // Act / Assert
        _sut.Remove(other).ShouldBe(other);
    }
}
