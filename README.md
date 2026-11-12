# Prevue Commander

## What is this?

Prevue Commander sends data to an emulated Prevue Guide system. It sends the channel lineup, the listings, local text ads, configuration, the clock, the daylight saving time window and files. It sends them on the serial data line of the Prevue program, as the satellite data feed did.

## What is Prevue Guide?

If you had cable TV in the 1980s or 1990s, you possibly remember [this channel](https://www.youtube.com/watch?v=ceukujHQvwM) in your lineup. An Amiga at the cable company ran it. The program of the Amiga is ESQ ("Esquire"), and it [was recovered](https://prevueguide.com/wiki/Prevue_(ESQ)).

## Requirements

- The [.NET 10 SDK](https://dotnet.microsoft.com/download).
- An emulated Prevue system with its serial port on a TCP port. For example, use [Esquire](https://prevueguide.neocities.org/guides/Esquire), which opens the serial port on TCP port 5400. An Amiga emulator with a TCP serial port also works.
- Guide data: an XMLTV file (for example from [zap2xml](https://github.com/jef/zap2xml)), or a [Channels DVR](https://getchannels.com/dvr-server/) server.

## Run it

1. Start the emulated Prevue system, and wait until the grid shows.
2. Run `dotnet run --project PrevueCommander -- playbook.yaml`.

If the playbook does not exist, Prevue Commander writes an example playbook to that path and stops. Edit the playbook, and run the command again.

A playbook has a `configuration` section and a list of `commands`. Prevue Commander makes all the commands first. If a command fails, for example because the guide data cannot be read, it sends nothing. Then it connects and sends the commands in the order of the list.

[`playbook.yaml`](playbook.yaml) is the example playbook.

## Configuration

| Setting | Default | Description |
|---|---|---|
| `hostname` | `127.0.0.1` | The host of the serial bridge. |
| `port` | `5400` | The TCP port of the serial bridge. |
| `output` | `Minimal` | `Minimal` shows each command. `Verbose` also shows its bytes. |
| `baudRate` | `2400` | The speed of the line. Prevue Commander sends a maximum of `baudRate / 10` bytes each second. Use `0` only when the emulator paces the line. ESQ has no flow control, and it loses data when its buffer is full. |
| `commandDelay` | `100` | The pause in milliseconds after each command. |

## Commands

Each command has a YAML tag, for example `!!localAds`. A command without settings is `!!save {}`.

### Session

| Command | Settings | Description |
|---|---|---|
| `!!address` | `target` (`*`) | Selects the units that accept the next commands. ESQ accepts no other command before it. `*` selects all units. |
| `!!save` | | Tells ESQ to save the received data to its drive at the next box off. Without it, the data stays only in memory. |
| `!!boxOff` | | Ends the session. Send it last. |
| `!!reset` | `target` (`*`) | Reboots the Amiga after about 10 seconds. ESQ accepts a reset only directly after an address command, so this command sends both. |

### Configuration and time

| Command | Settings | Description |
|---|---|---|
| `!!configuration` | `configuration`: `timeZone` (`'6'`), `scrollSpeed` (`'3'`), `daylightSavingTime` (`true`), `clockOffsetMinutes` (`0`) | The status packet ('F'). ESQ needs it before a channel lineup or a change of the local ads. |
| `!!newLookConfiguration` | `configuration`: the fields of [`NewLookConfiguration`](Prevue.Commands/Model/NewLookConfiguration.cs) | The configuration record ('f'). ESQ saves it to `config.dat`. The defaults are the values of the original data files. |
| `!!clock` | `date`, `useCurrentDate` | Sets the clock to the local time, or to `date`, for example `2020-11-01T16:00`. ESQ accepts the clock only when `clockCommandInFSetting` of the 'f' configuration is `'2'` (the default). |
| `!!configureDst` | `year` | Sends the daylight saving time window of the year, from the time zone of this computer. |

ESQ adds (time zone - 6) hours to the clock and to the times of the grid. With the time zone `'6'`, ESQ shows the times as they are sent. Prevue Commander sends local times, so keep `'6'` unless you want another time on the screen.

### Listings

| Command | Settings | Description |
|---|---|---|
| `!!xmlTvGuideDataImport` | `xmlTvFiles`: a list of `path` and `maximumNumberOfChannels` | Imports XMLTV files. A relative path starts at the directory of the playbook. |
| `!!channelsDVRGuideDataImport` | `channelsDVRServers`: a list of `serverAddress`, `maximumNumberOfChannels`, `filterOnlyHDChannels` (`false`) and `filterMusicChoiceChannels` (`true`) | Imports the guide of Channels DVR servers. |

The two imports also have these settings:

| Setting | Default | Description |
|---|---|---|
| `sendChannelLineUp` | `true` | Sends the channel lineup. If the lineup changed, ESQ deletes the programs of the day, so all the programs follow it. |
| `channelNumberOrder` | `None` | `None`, `Ascending` or `Descending`. |
| `premiumChannels` | | Channel numbers or call signs. ESQ shows their programs on a red background. |
| `excludedChannels` | | Channel numbers or call signs to leave out. |

ESQ keeps the listings of two broadcast days. A broadcast day starts at 5:00 AM and has 48 half-hour slots. ESQ changes to the next day at 5:30 AM. An import sends the lineup and the programs of the current day and of the next day.

The import follows these rules:

- ESQ keeps a maximum of 200 channels. An import sends the first 200 channels and shows a warning.
- Each channel gets a source name of 1 to 6 letters or digits from its call sign. ESQ shows a maximum of 6 characters of the call sign.
- A program goes into the slot in which it starts. If it does not start at :00 or :30, the text starts with the time, for example `( 8:15) News`.
- The text has the format of the listings tool of Esquire. A movie is `"Title" (year) summary (rating)`. A sports event is `Title: event title`. Programs with closed captions end with `|`, which ESQ shows as the closed caption symbol.
- Characters outside ASCII become the nearest ASCII characters, or are removed. The text has a maximum of 120 characters.

### Local ads

| Command | Settings | Description |
|---|---|---|
| `!!localAds` | `ads`: a list of texts | Deletes the local ads of ESQ and sends the new ads. ESQ keeps a maximum of 45 ads. |

ESQ shows the local ads only when `localTextAdSource` of the 'f' configuration is `'R'` or `'S'` (the default is `'S'`). An ad has a maximum of 6 lines of 40 characters. ESQ does not wrap words.

An ad can have these tokens:

| Token | Description |
|---|---|
| `%COLOR%` and two colors | The background color and then the text color of the text that follows, for example `%COLOR%%BLACK%%CYAN%`. |
| `%TRANSPARENT%`, `%WHITE%`, `%BLACK%`, `%YELLOW%`, `%RED%`, `%CYAN%`, `%GRAY%`, `%BLUE%` | The colors 0 to 7 of the default palette. |
| `%LEFT%`, `%CENTER%`, `%RIGHT%` | The alignment of a line. A token that is not at the start of a line starts a new line. |
| `%%` | A `%` character. |

### Files and utilities

| Command | Settings | Description |
|---|---|---|
| `!!fileTransfer` | `source`, `target`, `address` (`*`) | Sends a local file to the drive of ESQ. The target must start with a volume of 4 characters, for example `DF0:Logos/MYAD.IFF`. ESQ accepts one file for each address command, so this command first sends a box off command and an address command. |
| `!!utility` | `action` | Tells ESQ to reload files or to show a screen. The actions are `ReloadAdsAndLogos`, `ScanLogos`, `RebuildBrushList`, `ReloadBrushes`, `ReloadGradient`, `ReloadBanner`, `ReloadDefaults`, `ReloadQTable`, `ReloadSourceConfiguration`, `ToggleMenu`, `ShowDiagnostics` and `ShowVersion`. |

ESQ has no command for a title text. To change the banner, send a new banner file with `!!fileTransfer`, and then send `!!utility` with `ReloadBanner`.

## Build and test

```sh
dotnet build
dotnet test
```

`build-osx.sh` makes a universal macOS program. `build-win.sh` makes a Windows x64 program. Do not trim the output: YamlDotNet and the XMLTV reader need the metadata that trimming removes.

## Test with Esquire

Esquire can show what ESQ does with each command. Run a second Esquire on a copy of its drive, with its own serial port, so that you do not change a running Esquire:

```sh
Esquire build/target/ESQ --listing build/target/ESQ.lst --drive /tmp/prevue-drive --serial-port 5401 \
    --serial-speed 4 --prevue-feed-trace /tmp/feed.log --screenshot /tmp/prevue.png --seconds 120 \
    --screenshot-every 15
```

Set `port: 5401` in the playbook, and run Prevue Commander when the grid shows. `/tmp/feed.log` shows each command that ESQ reads. A change of `_DATACErrs` shows a checksum error.

## Project layout

| Project | Description |
|---|---|
| `Prevue.Core` | Broadcast days, slots, group codes and text conversion. |
| `Prevue.Commands` | The commands of the serial data feed, and the listings builder. Each class tells the format that ESQ reads. |
| `PrevueCommander.XmlTv` | The XMLTV reader. |
| `PrevueCommander.ChannelsDVR` | The Channels DVR client. |
| `PrevueCommander` | The program and the playbook commands. |
| `PrevueCommander.Tests` | The tests. |
