using Prevue.Commands;
using Prevue.Core;

namespace PrevueCommander.Model.PlaybookCommands;

/// <summary>The data that all the commands of one run share.</summary>
/// <param name="Now">The local time of the run. All the commands use the same time.</param>
/// <param name="BaseDirectory">The directory of the playbook. Relative file paths start there.</param>
/// <param name="Log">The output for messages.</param>
public sealed record PlaybookContext(DateTime Now, string BaseDirectory, TextWriter Log)
{
    /// <summary>The characters that the text of the listings, the local ads and the promos can have.</summary>
    public CharacterSet Characters { get; init; } = CharacterSet.Latin1;

    public string ResolvePath(string path) => Path.GetFullPath(path, BaseDirectory);
}

/// <summary>
/// A command of a playbook. The YAML tag of a command is the camel-case name of its class without
/// "PlaybookCommand", for example <c>!!localAds</c> for <see cref="LocalAdsPlaybookCommand"/>.
/// </summary>
public interface IPlaybookCommand
{
    Task<IReadOnlyList<BaseCommand>> TransformAsync(PlaybookContext context, CancellationToken cancellationToken);
}

/// <summary>A playbook command that makes its commands without I/O.</summary>
public abstract record SimplePlaybookCommand : IPlaybookCommand
{
    public Task<IReadOnlyList<BaseCommand>> TransformAsync(PlaybookContext context,
        CancellationToken cancellationToken) => Task.FromResult(Transform(context));

    protected abstract IReadOnlyList<BaseCommand> Transform(PlaybookContext context);
}
