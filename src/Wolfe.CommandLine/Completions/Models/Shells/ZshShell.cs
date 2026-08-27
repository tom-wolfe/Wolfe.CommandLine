namespace Wolfe.CommandLine.Completions.Models.Shells;

/// <summary>
/// Zsh. Installs an autoloaded <c>_command</c> file into a writable <c>$fpath</c> directory when one exists
/// (preferring <c>site-functions</c>, the conventional third-party location); stock zsh has no user-level
/// <c>$fpath</c> entry, so the install falls back to a <c>~/.zshrc</c> block.
/// </summary>
internal sealed class ZshShell() : Shell("zsh")
{
    public override string Script(string command)
    {
        var function = $"_{FunctionSafe(command)}_complete";
        return $$"""
            # {{command}} zsh completion.
            # Enable for the current shell:  source <({{command}} completion zsh)
            # Install permanently:           {{command}} completion install zsh
            {{function}}()
            {
                local completions="$("${words[1]}" "[suggest:${CURSOR}]" "${BUFFER}" 2>/dev/null)"
                _values 'completions' ${(ps:\n:)completions}
            }
            compdef {{function}} {{command}}

            """;
    }

    // The file body is the completion function; compinit binds it to the command from the #compdef header.
    internal override string CompletionFileScript(string command) =>
        $$"""
        #compdef {{command}}
        # {{command}} zsh completion, autoloaded from fpath.
        local completions="$("${words[1]}" "[suggest:${CURSOR}]" "${BUFFER}" 2>/dev/null)"
        _values 'completions' ${(ps:\n:)completions}

        """;

    internal override string? CompletionFilePath(string command, CompletionEnvironment environment)
    {
        var directories = environment.ZshFunctionPath().Where(Directory.Exists).ToList();
        var target = directories.FirstOrDefault(directory =>
                directory.Contains("site-functions", StringComparison.Ordinal) && IsWritable(directory))
            ?? directories.FirstOrDefault(IsWritable);
        return target is null ? null : Path.Combine(target, $"_{command}");
    }

    internal override IEnumerable<string> CandidateCompletionFilePaths(string command, CompletionEnvironment environment) =>
        environment.ZshFunctionPath().Select(directory => Path.Combine(directory, $"_{command}"));

    internal override (string Path, string SourceLine) StartupFile(string command, CompletionEnvironment environment) =>
        (Path.Combine(environment.Home, ".zshrc"), $"source <({command} completion zsh)");
}
