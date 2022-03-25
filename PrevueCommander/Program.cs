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
        // TODO: Figure out what the configuration commands are used for.
        //       They're being sent by PrevueCLI so reverse this and re-implement them.
        //       Also, try to figure out how to remove as many as the +1/-1 hour manipulations
        //       being used on listings.
        //       The AdditionalConfigurationCommand(s) are listed in PrevueCLI as 'DSTCommand'
        //       so perhaps they're used for setting daylight savings time or timezones?

        private static List<(TagName tagName, Type tagType)> GenerateTags()
        {
            return new List<(TagName tagName, Type tagType)>
            {
                (new TagName("tag:yaml.org,2002:additional-config"), typeof(AdditionalConfigurationPlaybookCommand)),
                (new TagName("tag:yaml.org,2002:address"), typeof(AddressBasePlaybookCommand)),
                (new TagName("tag:yaml.org,2002:boxoff"), typeof(BoxOffPlaybookCommand)),
                (new TagName("tag:yaml.org,2002:clock"), typeof(ClockPlaybookCommmand)),
                (new TagName("tag:yaml.org,2002:configuration"), typeof(ConfigurationPlaybookCommand)),
                (new TagName("tag:yaml.org,2002:local-ads"), typeof(LocalAdsPlaybookCommand)),
                (new TagName("tag:yaml.org,2002:new-look-configuration"), typeof(NewLookConfigurationPlaybookCommand)),
                (new TagName("tag:yaml.org,2002:title"), typeof(TitlePlaybookCommand)),
                (new TagName("tag:yaml.org,2002:xmltv-import"), typeof(XmlTvImportPlaybookCommand))
            };
        }

        public static async Task Main(string[] args)
        {
            var yamlDeserializerBuilder = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance);
            yamlDeserializerBuilder = GenerateTags().Aggregate(yamlDeserializerBuilder,
                (current, tag) => current.WithTagMapping(tag.tagName, tag.tagType));
            var yamlDeserializer = yamlDeserializerBuilder.Build();

            var targetYamlFile = args.Length == 0 ? "playbook.yaml" : args[0];
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
