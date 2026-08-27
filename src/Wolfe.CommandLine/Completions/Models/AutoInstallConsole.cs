namespace Wolfe.CommandLine.Completions.Models;

/// <summary>
/// The terminal auto-install talks through. <see cref="System"/> uses the real console (messages and the
/// prompt go to stderr, so a piped stdout stays clean); tests supply a scripted fixture.
/// </summary>
internal sealed class AutoInstallConsole
{
    /// <summary>
    /// Whether a person is on the other end (stdin, stdout, and stderr are all attached to a terminal).
    /// </summary>
    public required Func<bool> IsInteractive { get; init; }

    /// <summary>
    /// Asks a yes/no question, defaulting to no.
    /// </summary>
    public required Func<string, bool> Confirm { get; init; }

    /// <summary>
    /// Writes a one-line notice.
    /// </summary>
    public required Action<string> Notify { get; init; }

    /// <summary>
    /// The real console.
    /// </summary>
    public static AutoInstallConsole System() => new()
    {
        IsInteractive = static () => !Console.IsInputRedirected && !Console.IsOutputRedirected && !Console.IsErrorRedirected,
        Confirm = static question =>
        {
            Console.Error.Write($"{question} [y/N] ");
            return Console.ReadLine()?.Trim().ToLowerInvariant() is "y" or "yes";
        },
        Notify = static message => Console.Error.WriteLine(message),
    };
}
