# Changelog

All notable changes to Wolfe.CommandLine will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-08-26

### Added

- **Shell tab completion for System.CommandLine apps** (bash, zsh, fish, pwsh). `root.AddCompletions("<app>")` wires the suggest directive and a `completion` command group:
- `completion <shell>` emits the completion script.
- `completion install <shell>` prefers a directory the shell loads completions from automatically (fish always; bash when bash-completion is present; zsh when a writable `$fpath` directory exists), falling back to a managed block in the shell's startup file (always for pwsh).
- `completion uninstall <shell>` removes the completion from every location it may have been installed to.

[0.1.0]: https://github.com/tom-wolfe/Wolfe.CommandLine/releases/tag/v0.1.0
