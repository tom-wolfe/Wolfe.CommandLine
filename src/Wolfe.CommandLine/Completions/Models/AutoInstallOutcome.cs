namespace Wolfe.CommandLine.Completions.Models;

/// <summary>
/// What an auto-install attempt did.
/// </summary>
internal enum AutoInstallOutcome
{
    /// <summary>
    /// The invocation was not a moment to act (completion callback, CI, no terminal, unknown shell).
    /// </summary>
    Skipped,

    /// <summary>
    /// A previous run already installed or was declined; nothing was asked again.
    /// </summary>
    AlreadyHandled,

    /// <summary>
    /// Completion was installed.
    /// </summary>
    Installed,

    /// <summary>
    /// The user declined the startup-file edit; the decline is remembered.
    /// </summary>
    Declined,
}
