namespace Wolfe.CommandLine.Completions.Models.Shells;

/// <summary>
/// PowerShell. Native-command completers only register from <c>$PROFILE</c> — there is no directory
/// PowerShell loads them from automatically — so the install always writes the profile block.
/// </summary>
internal sealed class PwshShell() : Shell("pwsh")
{
    public override string Script(string command) =>
        $$"""
        # {{command}} PowerShell completion.
        # Enable for the current session:  {{command}} completion pwsh | Out-String | Invoke-Expression
        # Install permanently:             {{command}} completion install pwsh
        Register-ArgumentCompleter -Native -CommandName {{command}} -ScriptBlock {
            param($wordToComplete, $commandAst, $cursorPosition)
            $line = $commandAst.ToString()
            & {{command}} "[suggest:$cursorPosition]" "$line" 2>$null | ForEach-Object {
                [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_)
            }
        }

        """;

    internal override string? CompletionFilePath(string command, CompletionEnvironment environment) => null;

    internal override IEnumerable<string> CandidateCompletionFilePaths(string command, CompletionEnvironment environment) => [];

    internal override (string Path, string SourceLine) StartupFile(string command, CompletionEnvironment environment) =>
        (Profile(environment), $"{command} completion pwsh | Out-String | Invoke-Expression");

    // PowerShell 7+'s current-user profile lives under Documents on Windows and the config home elsewhere.
    private static string Profile(CompletionEnvironment environment) => environment.IsWindows
        ? Path.Combine(environment.Home, "Documents", "PowerShell", "Microsoft.PowerShell_profile.ps1")
        : Path.Combine(environment.ConfigHome, "powershell", "Microsoft.PowerShell_profile.ps1");
}
