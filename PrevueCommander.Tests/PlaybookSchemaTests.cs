using System.Text.Json.Nodes;
using Xunit;

namespace PrevueCommander.Tests;

public class PlaybookSchemaTests
{
    private static string RepositoryFile(string name)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            var path = Path.Combine(directory.FullName, name);
            if (File.Exists(path))
                return path;
        }

        throw new FileNotFoundException($"{name} is not in a parent directory of the tests.");
    }

    [Fact]
    public void TheSchemaOfTheRepository_IsUpToDate()
    {
        // Run "dotnet run --project PrevueCommander -- schema > playbook.schema.json" after a change of a playbook class.
        var committed = File.ReadAllText(RepositoryFile("playbook.schema.json")).ReplaceLineEndings("\n");
        Assert.Equal(PlaybookSchema.Generate(), committed);
    }

    [Fact]
    public void TheSchema_HasEachCommand_AndRejectsUnknownSettings()
    {
        var schema = JsonNode.Parse(PlaybookSchema.Generate())!;
        var commands = schema["properties"]!["commands"]!["items"]!["anyOf"]!.AsArray();

        Assert.Equal(PlaybookLoader.CommandTags().Count(), commands.Count);
        Assert.Contains(commands, c => c!["title"]!.GetValue<string>() == "!!localAds");
        Assert.All(commands, c => Assert.False(c!["additionalProperties"]!.GetValue<bool>()));
        Assert.False(schema["properties"]!["configuration"]!["additionalProperties"]!.GetValue<bool>());
    }

    [Fact]
    public void TheExamplePlaybook_NamesTheSchema() =>
        Assert.StartsWith($"# yaml-language-server: $schema={PlaybookSchema.Address}", PlaybookLoader.Example());

    [Fact]
    public void CustomTags_HasEachCommand() =>
        Assert.Contains("!!localAds mapping", PlaybookSchema.CustomTags());
}
