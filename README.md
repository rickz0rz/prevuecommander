# Prevue Commander

## What is this?

Prevue Commander sends data to an emulated Prevue Guide system. It sends the channel lineup, the listings, local text ads, configuration, the clock, the daylight saving time window and files. It sends them on the serial data line of the Prevue program, as the satellite data feed did.

Prevue Commander can also send commands on the 110 baud control line. These commands show promos of programs and logos in the top half of the screen.

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
| `controlPort` | `0` | The TCP port of the control line bridge. Esquire opens it with `--prevue-ctrl-port`. `0` means that the playbook does not use the control line. |
| `controlBaudRate` | `110` | The speed of the control line. ESQ reads it at 110 baud, so a promo takes about 2 seconds. Esquire keeps the bytes until the line sends them, so `0` also works with Esquire. |

Prevue Commander connects to a line only when a command needs it. A playbook with only control line commands does not connect to the data line.

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
| `!!utility` | `action` | Tells ESQ to reload files or to show a screen. The actions are `ReloadGraphicAds`, `ReloadLogoList`, `RebuildBrushList`, `ReloadBrushes`, `ReloadGradient`, `ReloadBanner`, `ReloadDefaults`, `ReloadQTable`, `ReloadSourceConfiguration`, `ToggleMenu`, `ShowDiagnostics` and `ShowVersion`. |
| `!!shell` | `commandLine` | Runs an AmigaDOS command line on the Amiga, for example `Delete DF0:OLD.IFF`. The command line has a maximum of 78 characters. ESQ shows no output. |

ESQ first writes a received file to `RAM:`, in the same directories as the target. Then it copies the file to the target. If the directory does not exist in `RAM:`, ESQ ignores the file. Thus, for a target in a directory, `!!fileTransfer` first sends `MakeDir` commands for the directories in `RAM:`.

`ReloadGraphicAds` reloads only `gfx/g_ads.data`. `ReloadLogoList` also reads `LOGO.LST` again and starts at its first line. In a test with Esquire, ESQ stopped with a memory error after `ReloadLogoList`. Test it before you use it. ESQ always reads `LOGO.LST` when it starts.

ESQ has no command for a title text. To change the banner, send a new banner file with `!!fileTransfer`, and then send `!!utility` with `ReloadBanner`.

### Control line

The control line commands go to `controlPort`. ESQ reads them only when its display is not busy. Put a `!!pause` between two promos.

| Command | Settings | Description |
|---|---|---|
| `!!promo` | `right`, `left`: a box with `title`, `channels` (`*`) and `brush`. `first` (`Right`) | Shows a promo in the top half of the screen. |
| `!!clearTopHalf` | | Removes the promo or the logo. The top half shows the genlock video. |
| `!!showLogo` | | Shows the loaded logo of `LOGO.LST`. ESQ then loads the next logo. |
| `!!controlPacket` | `type` (1 to 22), `body` | Sends one packet, for the types that have no command. In a double-quoted YAML text, `\x12` is the separator of the two boxes. |
| `!!pause` | `seconds` (`1`) | Waits before the next command. It does not send data. |

A promo has a box on the right, a box on the left, or both boxes. ESQ finds the program in its listings, so send the listings first. A box has these settings:

- `title` is the title to find. A part of the title is sufficient, and upper case and lower case are the same. Put the title in quotes to find only the exact title.
- `channels` is a pattern for the call letters, for example `KTIV*`.
- `brush` is the ID of the background in `BRUSH.INI` on the drive, for example `AT` (orange "around town") or `DT` (blue TV Guide). Without it, the box keeps its background.

The box shows the call letters, the title, the next time of the program and the channel number. ESQ shows only one box. It tries the `first` box, and then the other box. If ESQ does not find a title, it shows the loaded logo.

#### Packet types

These are the types of the control line, from the code of ESQ. A packet has a body of a maximum of 196 characters. A machine with a RAVESC selection code ignores the types with an asterisk (*). ESQ ignores the types 6, 8, 9, 10, 14, 18, 19 and 21.

| Type | Body | What ESQ does |
|---|---|---|
| 1 | a sub-command, see below | Controls the top half of the screen. |
| 2* | two brush IDs | Sets the backgrounds of the two promo boxes. `00` keeps a background, and `11` removes it. `!!promo` sends this type. |
| 3* | any | Nothing. ESQ counts the packet only. |
| 4* | `L` or `R` | Sets the box that ESQ tries first. Another character changes the order. `!!promo` sends this type. |
| 5* | two characters | Sets the channel codes of the two boxes. ESQ uses them in the title search. |
| 7* | | Stops the read mode. |
| 11* | 11 digits | Sets the clock and the battery clock of the Amiga. The CLOCKCMD setting must be `1`. Do not use it for tests: the battery clock keeps the time. |
| 12 | `1<channels>` or `3` | The same as type 1, but only when no local avail filter and no graphic ads are active. |
| 13 | | ESQ ignores the next byte. |
| 15 | a number | Sets the level of a copper effect to 63 minus the number. The effect on the screen is not known. |
| 16* | | Starts the read mode. ESQ then stops the animation of the grid. |
| 17* | `<title>` or `<title>\x12<title>` | Sets the titles that a promo finds. `!!promo` sends this type. |
| 20* | one character | Keeps a promo type for the next type 1 or 12. If the type is in `PromoId.Dat`, ESQ shows the logo in place of the promo. |
| 22* | `9<tokens>` or `8` | `9` starts a local avail filter, and `8` stops it. With the diagnostic VIN setting, `0` to `3` control the CTRL output. |

The sub-commands of type 1:

| Body | What ESQ does |
|---|---|
| `1<channels>[\x12<channels>]` | Shows a promo. `!!promo` sends this sub-command. |
| `8<channels>[\x12<channels>]` | The same search as `1`, with a different index for the status screen. The effect on the screen is not known. |
| `3` | Removes the promo or the logo (`!!clearTopHalf`). |
| `D` | Shows the loaded logo (`!!showLogo`). |
| `4<channels>` or `6<channels>` | Finds a channel by its call letters. If ESQ finds no channel, it shows the logo. |
| `5<hex><hex><digit><digit>[<channels>]` | Moves the banner. This needs the LRBN setting `Y`. The two digits set the speed: seconds, and then tenths of a second. |
| `7<digit><digit>` | Selects a range of channels after a search with `1` or `8`. |
| `F<op><text>` or `X<op><text>` | Sends a command to the text display. `C` shows a "now showing" line, `J` shows a status line, and `T` shows a highlight frame. |
| `W0` or `W3` | Shows a weather status. Another character shows the logo. |

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

To test the control line, add `--prevue-ctrl-port 5402` and set `controlPort: 5402` in the playbook. Add `--watch _SCRIPT_CtrlCmdCount --watch _SCRIPT_CtrlCmdChecksumErrorCount` to show the packets that ESQ reads, and the packets with a bad checksum.

## Project layout

| Project | Description |
|---|---|
| `Prevue.Core` | Broadcast days, slots, group codes and text conversion. |
| `Prevue.Commands` | The commands of the data line and of the control line (`Control`), and the listings builder. Each class tells the format that ESQ reads. |
| `PrevueCommander.XmlTv` | The XMLTV reader. |
| `PrevueCommander.ChannelsDVR` | The Channels DVR client. |
| `PrevueCommander` | The program and the playbook commands. |
| `PrevueCommander.Tests` | The tests. |
