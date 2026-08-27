namespace Wolfe.CommandLine.Completions.Models.Shells;

/// <summary>
/// Bash. Installs into the XDG bash-completion user directory when the bash-completion package is present
/// (it lazy-loads files there by command name); plain bash has no such loader, so the install falls back
/// to a <c>~/.bashrc</c> block.
/// </summary>
internal sealed class BashShell() : Shell("bash")
{
    public override string Script(string command)
    {
        var function = $"_{FunctionSafe(command)}_complete";
        return $$"""
            # {{command}} bash completion.
            # Enable for the current shell:  source <({{command}} completion bash)
            # Install permanently:           {{command}} completion install bash
            {{function}}()
            {
                local completions
                completions="$("${COMP_WORDS[0]}" "[suggest:${COMP_POINT}]" "${COMP_LINE}" 2>/dev/null)"
                COMPREPLY=( $(compgen -W "${completions}" -- "${COMP_WORDS[COMP_CWORD]}") )
                return 0
            }
            complete -f -F {{function}} {{command}}

            """;
    }

    internal override string? CompletionFilePath(string command, CompletionEnvironment environment) =>
        environment.BashCompletionInstalled() ? UserCompletionFile(command, environment) : null;

    internal override IEnumerable<string> CandidateCompletionFilePaths(string command, CompletionEnvironment environment) =>
        [UserCompletionFile(command, environment)];

    internal override (string Path, string SourceLine) StartupFile(string command, CompletionEnvironment environment) =>
        (Path.Combine(environment.Home, ".bashrc"), $"source <({command} completion bash)");

    private static string UserCompletionFile(string command, CompletionEnvironment environment) =>
        Path.Combine(environment.DataHome, "bash-completion", "completions", command);
}
