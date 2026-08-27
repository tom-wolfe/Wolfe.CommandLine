namespace Wolfe.CommandLine.Completions;

/// <summary>
/// The delimited completion block one command manages in a shell startup file. The block <em>sources</em>
/// <c>command completion &lt;shell&gt;</c> rather than embedding the script, so completions always track the
/// installed binary, and removal takes exactly the block that was written.
/// </summary>
internal sealed class StartupFileBlock(string command)
{
    public string BeginMarker { get; } = $"# >>> {command} completion >>>";

    public string EndMarker { get; } = $"# <<< {command} completion <<<";

    /// <summary>
    /// Inserts the managed block into <paramref name="contents"/>, replacing any block already present so the
    /// operation is idempotent. A blank line separates the block from preceding content.
    /// </summary>
    public string Apply(string contents, string sourceLine)
    {
        var block = $"{BeginMarker}\n{sourceLine}\n{EndMarker}\n";

        if (TryFind(contents, out var start, out var end))
        {
            return contents[..start] + block + contents[end..];
        }

        if (contents.Length == 0)
        {
            return block;
        }

        var separator = contents.EndsWith('\n') ? "\n" : "\n\n";
        return contents + separator + block;
    }

    /// <summary>Removes the managed block from <paramref name="contents"/>, including the blank line added before it.</summary>
    public string Remove(string contents)
    {
        if (!TryFind(contents, out var start, out var end))
        {
            return contents;
        }

        var before = contents[..start];
        if (before.EndsWith("\n\n", StringComparison.Ordinal))
        {
            before = before[..^1];
        }

        return before + contents[end..];
    }

    /// <summary>Locates the managed block, reporting the index of <see cref="BeginMarker"/> and one past the block's trailing newline.</summary>
    private bool TryFind(string contents, out int start, out int end)
    {
        start = contents.IndexOf(BeginMarker, StringComparison.Ordinal);
        end = -1;
        if (start < 0)
        {
            return false;
        }

        var marker = contents.IndexOf(EndMarker, start, StringComparison.Ordinal);
        if (marker < 0)
        {
            start = -1;
            return false;
        }

        end = marker + EndMarker.Length;
        if (end < contents.Length && contents[end] == '\n')
        {
            end++;
        }

        return true;
    }
}
