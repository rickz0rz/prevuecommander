using Prevue.Commands;
using Prevue.Commands.Model;

namespace PrevueCommander.Model.PlaybookCommands;

/// <summary>Sends the configuration record ('f'). ESQ saves it to config.dat.</summary>
public record NewLookConfigurationPlaybookCommand : SimplePlaybookCommand
{
    public NewLookConfiguration Configuration { get; init; } = new();

    protected override IReadOnlyList<BaseCommand> Transform(PlaybookContext context) =>
        [new NewLookConfigurationCommand(Configuration)];
}
