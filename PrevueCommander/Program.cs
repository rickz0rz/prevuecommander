using System.Net;
using System.Net.Sockets;
using Prevue.Commands;
using PrevueCommander.XmlTv;
using CommandChannel = Prevue.Commands.Model.Channel;

namespace PrevueCommander
{
    public static class Program
    {
        private static async Task PrintAndSendCommand(this Socket socket, BaseCommand command)
        {
            var bytes = command.Render();
            Console.WriteLine(string.Join(' ', bytes.Select(x => x.ToString("X2"))));

            var queue = new Queue<byte>(bytes);

            // Slow things down for testing... probably not necessary.
            while (queue.TryDequeue(out var b))
            {
                await socket.SendAsync(new[] { b }, SocketFlags.None);
            }
        }

        // TODO: Figure out what the configuration commands are used for.
        //       They're being sent by PrevueCLI so reverse this and re-implement them.
        //       Also, try to figure out how to remove as many as the +1/-1 hour manipulations
        //       being used on listings.
        //       The AdditionalConfigurationCommand(s) are listed in PrevueCLI as 'DSTCommand'
        //       so perhaps they're used for setting daylight savings time or timezones?

        public static async Task Main(string[] args)
        {
            const int port = 1234;
            var ipEndpoint = new IPEndPoint(IPAddress.Loopback, port);
            var socket = new Socket(ipEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            await socket.ConnectAsync(ipEndpoint);
            if (socket.Connected)
            {
                // Signal all boxes to listen
                await PrintAndSendCommand(socket, new AddressCommand("*"));

                var date = DateTime.Now;

                // Various configuration commands, part 1
                await PrintAndSendCommand(socket, new ConfigurationCommand());
                await PrintAndSendCommand(socket, new NewLookConfigurationCommand());

                // Set the clock of the guide.
                await PrintAndSendCommand(socket, new ClockCommand(date));

                // Various configuration commands, part 2
                await PrintAndSendCommand(socket, new AdditionalConfigurationCommand(0x32));
                await PrintAndSendCommand(socket, new AdditionalConfigurationCommand(0x33));

                // await CreateTestChannels(client, date);
                Console.WriteLine(args[0]);
                var xmlTvCommands = await XmlTvCore.ImportXml(date, args[0], 20);
                foreach (var xmlTvCommand in xmlTvCommands)
                {
                    await PrintAndSendCommand(socket, xmlTvCommand);
                }

                // Remove all local ads.
                await PrintAndSendCommand(socket, new LocalAdResetCommand());

                // Create some new local ads.
                var adCommands = LocalAdCommand.GenerateAdCommands(new[]
                {
                    "%COLOR%%BLACK%%CYAN%You all want some..." +
                    "%CENTER%%COLOR%%BLACK%%YELLOW%... colored ads?" +
                    "%RIGHT%%COLOR%%BLACK%%RED%No problem!",
                    "Hello, world!"
                });
                foreach (var adCommand in adCommands)
                {
                    await PrintAndSendCommand(socket, adCommand);
                }

                // Set the guide title.
                await PrintAndSendCommand(socket, new TitleCommand("PREVUE GUIDE"));

                // Tell all boxes to stop listening.
                await PrintAndSendCommand(socket, new BoxOffCommand());
            }
        }


    }
}
