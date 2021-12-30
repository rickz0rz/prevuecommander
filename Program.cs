using Prevue.Commands;
using Prevue.Commands.Model;
using SockNet.ClientSocket;

namespace PrevueCommander
{
    public static class Program
    {
        private static async Task PrintAndSendCommand(SocketClient client, BaseCommand command)
        {
            var bytes = command.Render();
            Console.WriteLine(string.Join(' ', bytes.Select(x => x.ToString("X2"))));
            await client.Send(bytes);
        }

        public static async Task Main(string[] args)
        {
            var client = new SocketClient("127.0.0.1", 1234);
            if (await client.Connect())
            {
                var date = DateTime.Now;

                await PrintAndSendCommand(client, new AddressCommand("*"));
                await PrintAndSendCommand(client, new ClockCommand(date));

                var poop = new Channel
                {
                    CallSign = "POOP",
                    ChannelNumber = "1",
                    SourceName = "POOP01",
                    TimeSlotMask = null
                };

                var prevue = new Channel
                {
                    CallSign = "PREVUE",
                    ChannelNumber = "2",
                    SourceName = "PREV01",
                    TimeSlotMask = null
                };

                await PrintAndSendCommand(client, new ChannelLineUpCommand(date, new[] { poop, prevue }));

                for (var i = 0; i < 64; i += 6)
                {
                    await PrintAndSendCommand(client, new ChannelProgramCommand((byte)i, date, poop.SourceName,
                        false, "You're watching the POOP CHANNEL."));
                }

                for (var i = 0; i < 64; i += 6)
                {
                    await PrintAndSendCommand(client, new ChannelProgramCommand((byte)i, date, prevue.SourceName,
                        false, "Before you view, %PREVUE%!"));
                }

                await PrintAndSendCommand(client, new LocalAdResetCommand());

                var adCommands = LocalAdCommand.GenerateAdCommands(new[]
                {
                    "%COLOR%%BLACK%%CYAN%You all want some..." +
                    "%CENTER%%COLOR%%BLACK%%YELLOW%... colored ads?" +
                    "%RIGHT%%COLOR%%BLACK%%RED%No problem!",
                    "Hello, world!"
                });

                foreach (var adCommand in adCommands)
                {
                    await PrintAndSendCommand(client, adCommand);
                }

                await PrintAndSendCommand(client, new TitleCommand("PREVUE GUIDE"));

                await PrintAndSendCommand(client, new BoxOffCommand());
            }
        }
    }
}