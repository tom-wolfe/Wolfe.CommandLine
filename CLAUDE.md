# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Wolfe.CommandLine packs utility extensions for System.CommandLine. Its first feature is shell tab
completion: emit, install, and uninstall of the bridge scripts that call back into the app via the
suggest directive (`[suggest:<cursor>]`), so the app is its own completion provider.

Target framework: `net10.0`. `LangVersion=latest`, nullable, `TreatWarningsAsErrors=true`, central
package management (`Directory.Packages.props`).

## Instructions

- Never commit. Changes are reviewed and committed by the maintainer.
- Be brief. One short sentence is enough for XML docs and code comments.
- Public facing changes go in CHANGELOG.md, relative to the previous released version.

## Commands

- Build: `dotnet build Wolfe.CommandLine.slnx`
- Test (all): `dotnet test Wolfe.CommandLine.slnx`
- Test (one class): `dotnet test Wolfe.CommandLine.slnx --filter "FullyQualifiedName~CompletionInstallerTests"`

## Architecture

- `Shell` (`Completions/`) — smart enum (`Shell.Bash/Zsh/Fish/Pwsh`); each subclass owns everything
  shell-specific: its script, its completion-file variant, its auto-load directory, its startup file.
  Adding a shell is one new subclass; nothing switches on shell names elsewhere.
- `CompletionInstaller` — tiered install: a directory the shell auto-loads from first, a managed
  startup-file block as fallback. Uninstall sweeps every location an install could have written.
  Ctor takes the ambient (`command`, `CompletionEnvironment`); methods take the per-execution shell.
- `CompletionEnvironment` — the ambient-facts seam (home, XDG overrides, zsh `$fpath` probe,
  bash-completion probe). `Detect()` reads the real machine; tests inject a temp-rooted fixture.
- `StartupFileBlock` — the delimited `# >>> <command> completion >>>` block; markers carry the
  command name so multiple apps coexist in one startup file.
- `CompletionCommand` / `RootCommandExtensions.AddCompletions` — the drop-in surface; hosts with
  their own presentation layer can call the installer directly instead.
- `CompletionAutoInstall` — startup auto-install: silent for completion-directory installs, `[y/N]`
  prompt for startup-file edits, at most once per shell (`AutoInstallLedger` markers in the XDG
  state home remember installs and declines). Skips CI, non-interactive streams, `[suggest:…]`
  callbacks, and explicit `completion` invocations; the public `Run` never throws.
  `AutoInstallConsole` is the terminal seam, `Shell.DetectCurrent` picks the shell (PowerShell
  session signal wins over the login shell).

## Tests

- xUnit v3 (Microsoft.Testing.Platform via `global.json`), Shouldly, Verify for script snapshots.
- `// Arrange` / `// Act` / `// Assert` heading comments; a single `_sut` field where the SUT is an
  instance. Omit a heading with no content; single-expression tests need no headings.
- The scripts are an output surface — snapshot-tested (`Completions/Snapshots/`).
