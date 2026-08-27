namespace Wolfe.CommandLine.Completions.Models.Shells;

/// <summary>
/// A shell that tab completion can be generated and installed for. Each shell owns its completion script
/// and knows where an install lands: a directory the shell loads completions from automatically, or a
/// managed block in its startup file.
/// </summary>
internal abstract class Shell
{
    /// <summary>
    /// The bash shell.
    /// </summary>
    public static readonly Shell Bash = new BashShell();

    /// <summary>
    /// The zsh shell.
    /// </summary>
    public static readonly Shell Zsh = new ZshShell();

    /// <summary>
    /// The fish shell.
    /// </summary>
    public static readonly Shell Fish = new FishShell();

    /// <summary>
    /// PowerShell.
    /// </summary>
    public static readonly Shell Pwsh = new PwshShell();

    /// <summary>
    /// Every supported shell.
    /// </summary>
    public static readonly IReadOnlyList<Shell> All = [Bash, Zsh, Fish, Pwsh];

    private protected Shell(string name) => Name = name;

    /// <summary>
    /// The shell's name as spelled on the command line.
    /// </summary>
    public string Name { get; }

    /// <inheritdoc />
    public override string ToString() => Name;

    /// <summary>
    /// Resolves a shell by name.
    /// </summary>
    public static Shell Parse(string name) =>
        All.FirstOrDefault(shell => shell.Name == name)
        ?? throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown shell.");

    /// <summary>
    /// The completion script for <paramref name="command"/>: registers a completer that, on tab, calls
    /// <c>command [suggest:&lt;cursor&gt;] "&lt;line&gt;"</c> (System.CommandLine's suggest directive) and feeds
    /// the candidates back to the shell. Suitable for sourcing into a live shell or from a startup file.
    /// </summary>
    public abstract string Script(string command);

    /// <summary>
    /// The script written into the shell's completion directory. Differs from <see cref="Script"/> only for
    /// zsh, whose autoloaded files declare themselves with a <c>#compdef</c> header instead of calling
    /// <c>compdef</c>.
    /// </summary>
    internal virtual string CompletionFileScript(string command) => Script(command);

    /// <summary>
    /// The file an install writes when this shell can load it automatically, or null when no such location
    /// is available and the install must fall back to the startup file.
    /// </summary>
    internal abstract string? CompletionFilePath(string command, CompletionEnvironment environment);

    /// <summary>
    /// Every completion file an install may ever have written, for uninstall to sweep.
    /// </summary>
    internal abstract IEnumerable<string> CandidateCompletionFilePaths(string command, CompletionEnvironment environment);

    /// <summary>
    /// The shell's startup file and the line that loads completion into a new session.
    /// </summary>
    internal abstract (string Path, string SourceLine) StartupFile(string command, CompletionEnvironment environment);

    /// <summary>
    /// The command's name reduced to characters safe in a shell function name.
    /// </summary>
    private protected static string FunctionSafe(string command) =>
        string.Concat(command.Select(character => char.IsAsciiLetterOrDigit(character) ? character : '_'));

    /// <summary>
    /// Whether a probe file can be created in <paramref name="directory"/>.
    /// </summary>
    private protected static bool IsWritable(string directory)
    {
        var probe = Path.Combine(directory, $".{Guid.NewGuid():N}.probe");
        try
        {
            File.Create(probe).Dispose();
            File.Delete(probe);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
