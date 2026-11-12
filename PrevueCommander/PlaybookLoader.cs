using System.Reflection;
using System.Text.Json;
using PrevueCommander.Model;
using PrevueCommander.Model.PlaybookCommands;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PrevueCommander;

/// <summary>Reads playbooks. The tag of each command, for example <c>!!localAds</c>, selects its class.</summary>
public static class PlaybookLoader
{
    private const string ExampleResource = "PrevueCommander.playbook.yaml";

    /// <summary>The YAML tags of the playbook commands and their classes.</summary>
    public static IEnumerable<(string Tag, Type Type)> CommandTags() =>
        typeof(IPlaybookCommand).Assembly.GetTypes()
            .Where(type => typeof(IPlaybookCommand).IsAssignableFrom(type) && type is { IsAbstract: false })
            .Select(type => (
                $"tag:yaml.org,2002:{JsonNamingPolicy.CamelCase.ConvertName(type.Name.Replace("PlaybookCommand", string.Empty))}",
                type));

    public static Playbook Parse(string yaml)
    {
        var builder = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance);
        foreach (var (tag, type) in CommandTags())
            builder = builder.WithTagMapping(tag, type);
        return builder.Build().Deserialize<Playbook?>(yaml) ?? new Playbook();
    }

    /// <summary>The example playbook of the repository, which the program writes when no playbook exists.</summary>
    public static string Example()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ExampleResource)
                           ?? throw new InvalidOperationException($"The resource {ExampleResource} is missing.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
