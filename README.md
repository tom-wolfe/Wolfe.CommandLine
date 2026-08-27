# Wolfe.CommandLine

Wolfe.CommandLine is a set of utility extensions for the [System.CommandLine](https://github.com/dotnet/command-line-api) package, including automatic completion registration.

## Installation

Installation is as easy as just adding the package:

```bash
dotnet package add Wolfe.CommanLine
```

## Usage

### Tab completion

`System.CommandLine` does support suggestions out of the box using `dotnet suggest`, but they're not wired up anywhere. Your shell needs to know they
exist and how to access them, so this utility package puts that wiring in place with a `completion` command group.

```csharp
using System.CommandLine;
using Wolfe.CommandLine.Completions;

var root = new RootCommand()
    .AddCompletions("my-app");
```

This registers:

- the 'suggest' directive that the completion scripts call back into
- a `completion` command group:
  - `my-app completion <shell>` — emit the script (bash, zsh, fish, pwsh)
  - `my-app completion install <shell>` — install it
  - `my-app completion uninstall <shell>` — remove it

#### How install works

Installation will pick a directory that the shell loads completions from automatically, if one is available, so no startup file is edited:

- **fish** — `~/.config/fish/completions/` (always)
- **bash** — the XDG bash-completion user directory (when the bash-completion package is present)
- **zsh** — a writable `$fpath` directory, preferring brew's `site-functions` (when it exists)

When no such directory is available (always for **pwsh**), it falls back to a managed
`# >>> my-app completion >>>` block in the shell's startup file. Uninstall sweeps both.

The scripts are thin bridges: on tab they run `my-app [suggest:<cursor>] "<line>"` and feed the
candidates back to the shell, so completions always match the installed binary and never go stale.
