using Wolfe.CommandLine.Completions.Models;
using Wolfe.CommandLine.Completions.Models.Shells;

namespace Wolfe.CommandLine.Completions;

/// <summary>
/// Installs and removes shell completion for one command. An install prefers a directory the shell loads
/// completions from automatically (no startup-file edit), falling back to a managed block in the shell's
/// startup file; an uninstall sweeps every location an install may have written.
/// </summary>
internal sealed class CompletionInstaller
{
    private readonly string _command;
    private readonly CompletionEnvironment _environment;
    private readonly StartupFileBlock _block;

    /// <summary>
    /// Creates an installer for <paramref name="command"/>, detecting the real environment unless one is supplied.
    /// </summary>
    public CompletionInstaller(string command, CompletionEnvironment? environment = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        _command = command;
        _environment = environment ?? CompletionEnvironment.Detect();
        _block = new StartupFileBlock(command);
    }

    /// <summary>
    /// Installs completion for <paramref name="shell"/>, reporting where it landed.
    /// </summary>
    public async Task<InstallResult> Install(Shell shell, CancellationToken cancellationToken = default)
    {
        if (shell.CompletionFilePath(_command, _environment) is { } file)
        {
            var script = shell.CompletionFileScript(_command);
            var existing = File.Exists(file) ? await File.ReadAllTextAsync(file, cancellationToken) : null;
            if (existing == script)
            {
                return new InstallResult(shell, CompletionLocation.CompletionDirectory, file, Changed: false);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            await File.WriteAllTextAsync(file, script, cancellationToken);
            return new InstallResult(shell, CompletionLocation.CompletionDirectory, file, Changed: true);
        }

        var (path, sourceLine) = shell.StartupFile(_command, _environment);
        var changed = await ApplyBlock(path, sourceLine, cancellationToken);
        return new InstallResult(shell, CompletionLocation.StartupFile, path, changed);
    }

    /// <summary>
    /// Removes completion for <paramref name="shell"/> from every location an install may have written.
    /// </summary>
    public async Task<UninstallResult> Uninstall(Shell shell, CancellationToken cancellationToken = default)
    {
        var removed = new List<string>();

        foreach (var file in shell.CandidateCompletionFilePaths(_command, _environment).Where(File.Exists))
        {
            try
            {
                File.Delete(file);
                removed.Add(file);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // A same-named file in a directory we cannot write (e.g. a distro-owned fpath entry) is not ours.
            }
        }

        var (path, _) = shell.StartupFile(_command, _environment);
        if (await RemoveBlock(path, cancellationToken))
        {
            removed.Add(path);
        }

        return new UninstallResult(shell, removed);
    }

    private async Task<bool> ApplyBlock(string path, string sourceLine, CancellationToken cancellationToken)
    {
        var existing = File.Exists(path) ? await File.ReadAllTextAsync(path, cancellationToken) : "";
        var updated = _block.Apply(existing, sourceLine);
        if (updated == existing)
        {
            return false;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, updated, cancellationToken);
        return true;
    }

    private async Task<bool> RemoveBlock(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return false;
        }

        var existing = await File.ReadAllTextAsync(path, cancellationToken);
        var updated = _block.Remove(existing);
        if (updated == existing)
        {
            return false;
        }

        await File.WriteAllTextAsync(path, updated, cancellationToken);
        return true;
    }
}
