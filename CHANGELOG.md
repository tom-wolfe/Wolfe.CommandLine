# Changelog

All notable changes to Wolfe.CommandLine will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.0] - 2026-08-27

### Added

- **Auto-install on startup.** `CompletionAutoInstall.Run("<app>", args)` — called once before invoking the parsed command — installs completion for the user's current shell the first time the app runs: silently (with a notice on stderr) when the install lands in a directory the shell loads automatically, behind a `[y/N]` prompt when it would edit a startup file. It runs at most once per shell (a decline is remembered under the XDG state home), never throws, and stays quiet on CI, without an interactive terminal, during completion callbacks, and when the `completion` command itself is being run.

## [0.1.0] - 2026-08-27

### Added

- **Shell tab completion for System.CommandLine apps** (bash, zsh, fish, pwsh). `root.AddCompletions("<app>")` wires the suggest directive and a `completion` command group:
- `completion <shell>` emits the completion script.
- `completion install <shell>` prefers a directory the shell loads completions from automatically (fish always; bash when bash-completion is present; zsh when a writable `$fpath` directory exists), falling back to a managed block in the shell's startup file (always for pwsh).
- `completion uninstall <shell>` removes the completion from every location it may have been installed to.

[0.2.0]: https://github.com/tom-wolfe/Wolfe.CommandLine/releases/tag/v0.2.0
[0.1.0]: https://github.com/tom-wolfe/Wolfe.CommandLine/releases/tag/v0.1.0
