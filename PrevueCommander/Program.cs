using System.Net;
using System.Net.Sockets;
using Prevue.Commands;
using PrevueCommander.XmlTv;
using CommandChannel = Prevue.Commands.Model.Channel;

namespace PrevueCommander
{
    public static class Program
    {
        const int MaximumNumberOfChannels = 100;

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
            var writer = new DataWriter(socket);

            await socket.ConnectAsync(ipEndpoint);
            if (socket.Connected)
            {
                // Signal all boxes to listen
                writer.AddCommandToBuffer(new AddressCommand("*"));

                var date = DateTime.Now;

                // Various configuration commands, part 1
                writer.AddCommandToBuffer(new ConfigurationCommand());
                writer.AddCommandToBuffer(new NewLookConfigurationCommand());

                // Set the clock of the guide.
                writer.AddCommandToBuffer(new ClockCommand(date));

                // Various configuration commands, part 2
                writer.AddCommandToBuffer(new AdditionalConfigurationCommand(0x32));
                writer.AddCommandToBuffer(new AdditionalConfigurationCommand(0x33));

                // Import XMLtv channels and program listings
                var xmlTvCommands = await XmlTvCore.ImportXml(date, args[0], MaximumNumberOfChannels);
                foreach (var xmlTvCommand in xmlTvCommands)
                {
                    writer.AddCommandToBuffer(xmlTvCommand);
                }

                // Remove all local ads. If you don't do this before writing
                // new local ads, you will freeze Esquire
                writer.AddCommandToBuffer(new LocalAdResetCommand());

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
                    writer.AddCommandToBuffer(adCommand);
                }

                // Set the guide title.
                writer.AddCommandToBuffer(new TitleCommand("PREVUE GUIDE"));

                // Tell all boxes to stop listening.
                writer.AddCommandToBuffer(new BoxOffCommand());

                writer.FlushBuffer();
            }
        }
    }
}
