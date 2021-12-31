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

        // TODO: Figure out what the configuration commands are used for.
        //       They're being sent by PrevueCLI so reverse this and re-implement them.
        //       Also, try to figure out how to remove as many as the +1/-1 hour manipulations
        //       being used on listings.
        //       The AdditionalConfigurationCommand(s) are listed in PrevueCLI as 'DSTCommand'
        //       so perhaps they're used for setting daylight savings time or timezones?

        public static async Task Main(string[] args)
        {
            var client = new SocketClient("127.0.0.1", 1234);
            if (await client.Connect())
            {
                // Signal all boxes to listen
                await PrintAndSendCommand(client, new AddressCommand("*"));

                var date = DateTime.Now;

                // Various configuration commands, part 1
                await PrintAndSendCommand(client, new ConfigurationCommand());
                await PrintAndSendCommand(client, new NewLookConfigurationCommand());

                // Set the clock of the guide.
                await PrintAndSendCommand(client, new ClockCommand(date));

                // Various configuration commands, part 2
                await PrintAndSendCommand(client, new AdditionalConfigurationCommand(0x32));
                await PrintAndSendCommand(client, new AdditionalConfigurationCommand(0x33));

                // Make some channels.
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

                // Add some programming for the channels.
                await PrintAndSendCommand(client, new ChannelProgramCommand(date, poop.SourceName,
                    false, "You're watching the POOP CHANNEL."));
                await PrintAndSendCommand(client, new ChannelProgramCommand(date, prevue.SourceName,
                    false, "Before you view, %PREVUE%!"));

                // Remove all local ads.
                await PrintAndSendCommand(client, new LocalAdResetCommand());

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
                    await PrintAndSendCommand(client, adCommand);
                }

                // Set the guide title.
                await PrintAndSendCommand(client, new TitleCommand("PREVUE GUIDE"));

                // Tell all boxes to stop listening.
                await PrintAndSendCommand(client, new BoxOffCommand());
            }
        }
    }
}
