using System.CommandLine;
using System.CommandLine.Completions;
using Wolfe.CommandLine.Completions;

namespace Wolfe.CommandLine;

/// <summary>
/// Extension methods for <see cref="RootCommand"/>.
/// </summary>
public static class RootCommandExtensions
{
    extension(RootCommand root)
    {
        /// <summary>
        /// Wires tab completion for the app named <paramref name="appCommand"/>.
        /// </summary>
        public RootCommand AddCompletions(string appCommand)
        {
            root.Add(new SuggestDirective());
            root.Subcommands.Add(CompletionCommand.Create(appCommand));
            return root;
        }
    }
}
