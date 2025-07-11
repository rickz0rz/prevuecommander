using System.Text.Json;
using Prevue.Core;
using PrevueCommander.Model;
using PrevueCommander.Model.PlaybookCommands;
using PrevueCommander.Model.PlaybookCommands.CommandObjects;
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
            let name = JsonNamingPolicy.CamelCase.ConvertName(
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
            Commands =
            [
                new AddressPlaybookCommand { Target = "*" },
                new ConfigurationPlaybookCommand(),
                new NewLookConfigurationPlaybookCommand
                {
                    Configuration = new NewLookConfiguration
                    {
                        DisplayMode = 'S'
                    }
                },

                new ClockPlaybookCommand { UseCurrentDate = true },
                new ConfigureDstPlaybookCommand(),
                /*new XmlTvGuideDataImportPlaybookCommand
                {
                    XmlTvFiles =
                    [
                        new XmlTvFile
                        {
                            Path = "xmltv.xml",
                            MaximumNumberOfChannels = 5
                        }
                    ],
                    SendChannelLineUp = true,
                    ChannelNumberOrder = SortMode.None,
                },*/
                new ChannelsDVRGuideDataImportPlaybookCommand
                {
                    ChannelsDVRServers = new List<ChannelsDVRServer>
                    {
                        new()
                        {
                            ServerAddress = "http://192.168.0.119:8089",
                            MaximumNumberOfChannels = 100
                        }
                    },
                    SendChannelLineUp = true,
                    ChannelNumberOrder = SortMode.None
                },
                new LocalAdsPlaybookCommand
                {
                    Ads =
                    [
                        "%COLOR%%BLACK%%CYAN%You all want some..." +
                        "%CENTER%%COLOR%%BLACK%%YELLOW%... colored ads?" +
                        "%RIGHT%%COLOR%%BLACK%%RED%No problem!",

                        "Hello, world!"
                    ]
                },

                new TitlePlaybookCommand { Text = "PREVUE GUIDE" },
                new SavePlaybookCommand(),
                new ReloadPlaybookCommand(),
                new BoxOffPlaybookCommand()
            ],
            Configuration = new Configuration
            {
                Hostname = "127.0.0.1",
                Port = 1234,
                Output = Output.Verbose
            }
        });

        return yaml;
    }
}