using PrevueCommander.Model;
using PrevueCommander.Model.PlaybookCommands;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PrevueCommander;

public static class Utilities
{
    public static IEnumerable<(TagName tagName, Type tagType)> GenerateTags()
    {
        var type = typeof(IBasePlaybookCommand);
        var assignableTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => type.IsAssignableFrom(p) && !p.IsInterface);

        return from assignableType in assignableTypes
            let name = System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(
                assignableType.Name.Replace("PlaybookCommand", string.Empty))
            select (new TagName($"tag:yaml.org,2002:{name}"), assignableType);
    }

    public static string GenerateDefaultYaml()
    {
        var yamlSerializerBuidler = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance);
        yamlSerializerBuidler = GenerateTags().Aggregate(yamlSerializerBuidler,
            (current, tag) => current.WithTagMapping(tag.tagName, tag.tagType));
        var yamlSerializer = yamlSerializerBuidler.Build();

        var yaml = yamlSerializer.Serialize(new Playbook
        {
            Commands = new List<IBasePlaybookCommand>
            {
                new AddressPlaybookCommand { Target = "*" },
                new ConfigurationPlaybookCommand(),
                new NewLookConfigurationPlaybookCommand(),
                new ClockPlaybookCommand { UseCurrentDate = true },
                new ConfigureDstPlaybookCommand { Payload = 0x32 },
                new ConfigureDstPlaybookCommand { Payload = 0x33 },
                new XmlTvImportPlaybookCommand
                {
                    XmlFile = "xmltv.xml", SendChannelLineUp = true,
                    MaximumNumberOfChannels = 5
                },
                new LocalAdsPlaybookCommand
                {
                    Ads = new List<string>
                    {
                        "%COLOR%%BLACK%%CYAN%You all want some..." +
                        "%CENTER%%COLOR%%BLACK%%YELLOW%... colored ads?" +
                        "%RIGHT%%COLOR%%BLACK%%RED%No problem!",
                        "Hello, world!"
                    }
                },
                new TitlePlaybookCommand { Text = "PREVUE GUIDE" },
                new SavePlaybookCommand(),
                new BoxOffPlaybookCommand()
            },
            Configuration = new Configuration
            {
                Hostname = "127.0.0.1",
                Port = 1234
            }
        });

        return yaml;
    }
}
