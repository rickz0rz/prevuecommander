using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using PrevueCommander.Model;

namespace PrevueCommander;

/// <summary>
/// Makes a JSON schema of the playbook from the playbook classes, so that a YAML editor can complete and check a
/// playbook.
/// </summary>
/// <remarks>
/// JSON schema has no YAML tags. An editor must know the tags, for example with the <c>yaml.customTags</c> setting of
/// the YAML extension of Visual Studio Code. A command must then match the settings of one of the commands.
/// </remarks>
public static class PlaybookSchema
{
    /// <summary>The address of the schema in the repository.</summary>
    public const string Address =
        "https://raw.githubusercontent.com/rickz0rz/prevuecommander/main/playbook.schema.json";

    private static readonly JsonSerializerOptions Options = new(JsonSerializerOptions.Default)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    /// <summary>The schema as JSON text.</summary>
    public static string Generate()
    {
        var exporter = new JsonSchemaExporterOptions
        {
            TreatNullObliviousAsNonNullable = true,
            TransformSchemaNode = Transform
        };

        // Make the schema of each command, and use them for the items of the list of commands.
        var commands = new JsonArray();
        foreach (var (tag, type) in PlaybookLoader.CommandTags().OrderBy(c => c.Tag, StringComparer.Ordinal))
        {
            var schema = Options.GetJsonSchemaAsNode(type, exporter).AsObject();
            schema["title"] = "!!" + ShortTag(tag);
            commands.Add(schema);
        }

        var root = Options.GetJsonSchemaAsNode(typeof(Playbook), exporter).AsObject();
        var properties = root["properties"]!.AsObject();
        properties["commands"] = new JsonObject
        {
            ["type"] = "array",
            ["items"] = new JsonObject { ["anyOf"] = commands }
        };

        var document = new JsonObject
        {
            ["$schema"] = "https://json-schema.org/draft/2020-12/schema",
            ["$id"] = Address,
            ["title"] = "Prevue Commander playbook"
        };
        foreach (var (name, value) in root.ToList())
        {
            root.Remove(name);
            document[name] = value;
        }

        return document.ToJsonString(Options).ReplaceLineEndings("\n") + "\n";
    }

    /// <summary>The YAML tags of the commands, as the <c>yaml.customTags</c> setting of Visual Studio Code.</summary>
    public static IEnumerable<string> CustomTags() =>
        PlaybookLoader.CommandTags().Select(c => $"!!{ShortTag(c.Tag)} mapping").Order(StringComparer.Ordinal);

    private static string ShortTag(string tag) => tag[(tag.LastIndexOf(':') + 1)..];

    private static JsonNode Transform(JsonSchemaExporterContext context, JsonNode schema)
    {
        if (schema is not JsonObject node)
            return schema;

        // The playbook reader stops at an unknown setting, so the schema does too. A misspelled setting then shows.
        if (context.TypeInfo.Kind == JsonTypeInfoKind.Object)
            node["additionalProperties"] = false;

        // A char setting is a string of one character.
        if (context.TypeInfo.Type == typeof(char))
        {
            node["minLength"] = 1;
            node["maxLength"] = 1;
        }

        return node;
    }
}
