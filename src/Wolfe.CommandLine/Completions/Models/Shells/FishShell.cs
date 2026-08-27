namespace Wolfe.CommandLine.Completions.Models.Shells;

/// <summary>
/// Fish. Always installs into the user completions directory — fish lazy-loads
/// <c>~/.config/fish/completions/&lt;command&gt;.fish</c> by command name, so no startup-file edit is ever needed.
/// </summary>
internal sealed class FishShell() : Shell("fish")
{
    public override string Script(string command)
    {
        var function = $"__{FunctionSafe(command)}_complete";
        return $$"""
            # {{command}} fish completion.
            # Enable for the current shell:  {{command}} completion fish | source
            # Install permanently:           {{command}} completion install fish
            function {{function}}
                set -l line (commandline -cp)
                {{command}} "[suggest:"(string length -- $line)"]" "$line" 2>/dev/null
            end
            complete -c {{command}} -f -a '({{function}})'

            """;
    }

    internal override string CompletionFilePath(string command, CompletionEnvironment environment) =>
        Path.Combine(environment.ConfigHome, "fish", "completions", $"{command}.fish");

    internal override IEnumerable<string> CandidateCompletionFilePaths(string command, CompletionEnvironment environment) =>
        [CompletionFilePath(command, environment)];

    internal override (string Path, string SourceLine) StartupFile(string command, CompletionEnvironment environment) =>
        (Path.Combine(environment.ConfigHome, "fish", "config.fish"), $"{command} completion fish | source");
}
