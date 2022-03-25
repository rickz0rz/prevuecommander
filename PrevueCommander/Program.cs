using System.Net;
using System.Net.Sockets;
using PrevueCommander.Model;
using PrevueCommander.Model.PlaybookCommands;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using CommandChannel = Prevue.Commands.Model.Channel;

namespace PrevueCommander
{
    public static class Program
    {
        private static IEnumerable<(TagName tagName, Type tagType)> GenerateTags()
        {
            var type = typeof(IBasePlaybookCommand);
            var assignableTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p) && !p.IsInterface);

            return from assignableType in assignableTypes
                let name = System.Text.Json.JsonNamingPolicy.CamelCase.
                    ConvertName(assignableType.Name.Replace("PlaybookCommand", string.Empty))
                select (new TagName($"tag:yaml.org,2002:{name}"), assignableType);
        }

        private static string GenerateDefaultYaml()
        {
            var yamlSerializerBuidler = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance);
            yamlSerializerBuidler = GenerateTags().Aggregate(yamlSerializerBuidler,
                (current, tag) => current.WithTagMapping(tag.tagName, tag.tagType));
            var yamlSerializer = yamlSerializerBuidler.Build();

            var yaml = yamlSerializer.Serialize(new Playbook
            {
                Commands = new List<IBasePlaybookCommand>
                {
                    new AddressBasePlaybookCommand { Target = "*" },
                    new ConfigurationPlaybookCommand(),
                    new NewLookConfigurationPlaybookCommand(),
                    new ClockPlaybookCommand { UseCurrentDate = true },
                    new AdditionalConfigurationPlaybookCommand { Payload = 0x32 },
                    new AdditionalConfigurationPlaybookCommand { Payload = 0x33 },
                    new XmlTvImportPlaybookCommand { XmlFile = "xmltv.xml", SendChannelLineUp = true,
                        MaximumNumberOfChannels = 5 },
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
                    new TitlePlaybookCommand { Text = "PREVUE GUIDE"},
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

        public static async Task Main(string[] args)
        {
            var yamlDeserializerBuilder = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance);
            yamlDeserializerBuilder = GenerateTags().Aggregate(yamlDeserializerBuilder,
                (current, tag) => current.WithTagMapping(tag.tagName, tag.tagType));
            var yamlDeserializer = yamlDeserializerBuilder.Build();

            var targetYamlFile = args.Length == 0 ? "playbook.yaml" : args[0];
            if (!File.Exists(targetYamlFile))
            {
                await File.WriteAllTextAsync(targetYamlFile, GenerateDefaultYaml());
            }

            var playbook = yamlDeserializer.Deserialize<Playbook>(await File.ReadAllTextAsync(targetYamlFile));

            var ipAddress = IPAddress.Parse(playbook.Configuration.Hostname);
            var ipEndpoint = new IPEndPoint(ipAddress, playbook.Configuration.Port);
            var socket = new Socket(ipEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            var writer = new DataWriter(socket);

            await socket.ConnectAsync(ipEndpoint);

            if (socket.Connected)
            {
                foreach (var command in playbook.Commands)
                {
                    foreach (var subCommand in await command.Transform())
                    {
                        writer.AddCommandToBuffer(subCommand);
                    }
                }

                writer.FlushBuffer();
            }
        }
    }
}
