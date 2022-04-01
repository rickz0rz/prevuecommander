using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using PrevueCommander.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using CommandChannel = Prevue.Commands.Model.Channel;

namespace PrevueCommander
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("===============================================");
            Console.WriteLine("| PrevueCommander                             |");
            Console.WriteLine("| https://github.com/rickz0rz/prevuecommander |");
            Console.WriteLine("===============================================");
            Console.WriteLine();

            var yamlDeserializerBuilder = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance);
            yamlDeserializerBuilder = Utilities.GenerateTags().Aggregate(yamlDeserializerBuilder,
                (current, tag) => current.WithTagMapping(tag.tagName, tag.tagType));
            var yamlDeserializer = yamlDeserializerBuilder.Build();

            var targetYamlFile = args.Length == 0 ? "playbook.yaml" : args[0];
            if (!File.Exists(targetYamlFile))
            {
                await File.WriteAllTextAsync(targetYamlFile, Utilities.GenerateDefaultYaml());
            }

            var playbook = yamlDeserializer.Deserialize<Playbook>(await File.ReadAllTextAsync(targetYamlFile));

            var calculatedHostName = string.IsNullOrWhiteSpace(playbook.Configuration?.Hostname)
                ? "127.0.0.1"
                : playbook.Configuration?.Hostname;

            if (!IPAddress.TryParse(calculatedHostName, out var ipAddress))
            {
                var hostAddresses = await Dns.GetHostAddressesAsync(calculatedHostName);
                if (hostAddresses.Any())
                {
                    ipAddress = hostAddresses.First();
                }
            }

            if (ipAddress == null)
                throw new Exception("Invalid hostname in configuration");

            var ipEndpoint = new IPEndPoint(ipAddress, playbook.Configuration?.Port ?? 1234);
            var socket = new Socket(ipEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            var writer = new DataWriter(socket, playbook.Configuration?.Output == Output.Verbose);

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
