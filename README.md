# Prevue Commander

Prevue Commander sends data to a Prevue Guide system: the channel lineup, the listings, local ads, the weather, promos and pictures. The system can be an emulator, such as Esquire or FS-UAE, or a real Amiga on a serial port of this computer.

If you had cable TV in the 1980s or 1990s, you may remember [this channel](https://www.youtube.com/watch?v=ceukujHQvwM) in your lineup. An Amiga at the cable company ran it. The program of the Amiga is ESQ ("Esquire"), and it [was recovered](https://prevueguide.com/wiki/Prevue_(ESQ)).

ESQ reads two serial lines:

- The **data line** gets the guide data, as the satellite feed sent it.
- The **control line** is a slow line (110 baud). Its commands show promos and logos in the top half of the screen.

You write the commands in a YAML file, the playbook. Prevue Commander reads the playbook and sends its commands in order.

## Quick start

You need:

- The [.NET 11 SDK](https://dotnet.microsoft.com/download).
- A Prevue system. [Esquire](https://prevueguide.neocities.org/guides/Esquire) is the easiest: it opens the data line on TCP port 5400. For a real Amiga, see [Connect to an Amiga](docs/amiga.md).
- Guide data for listings: an XMLTV file (for example from [zap2xml](https://github.com/jef/zap2xml)), or a [Channels DVR](https://getchannels.com/dvr-server/) server.

Then:

1. Start the Prevue system, and wait until the grid shows.
2. Run `dotnet run --project PrevueCommander`. Prevue Commander writes the example playbook `playbook.yaml` and stops.
3. Edit `playbook.yaml`. Set `port` to the port of the data line. Set the `path` of the XMLTV file, or use the Channels DVR import.
4. Run `dotnet run --project PrevueCommander` again. ESQ shows the new listings and local ads.

[`playbook.yaml`](playbook.yaml) has comments for each part. To check a playbook without a connection, add `--validate`. To show promos, set `controlPort` and add `!!promo` commands. See [Commands](docs/commands.md).

To run Prevue Commander without .NET, for example on a Raspberry Pi, build a program with `build-osx.sh`, `build-win.sh` or `build-linux-arm64.sh`. See [Development](docs/development.md).

### ESQ versions

Prevue Commander was developed and tested with ESQ 9.0.4. Other versions of ESQ can support different features. For example, ESQ 7.3.13 does not support colored text in local ads. For an older version of ESQ, set `characterSet: Ascii`, because its fonts possibly do not have letters with accents.

## Keep it running

Prevue Commander stops after the commands of the playbook. These options keep it connected:

| Option | What it does |
|---|---|
| `--watch <minutes>` | Reads the guide again at this interval and sends the changes. See [Watch mode](docs/watch-mode.md). |
| `autoPromo` section | Shows a promo of a program in the listings at an interval. See [Automatic promos](docs/auto-promos.md). |
| `--serve <port>` | Starts a web server with a control page and an HTTP API. See [Web server](docs/web-server.md). |
| `--weather <minutes>` | Reads the weather of `!!weatherGov` again at this interval. See [Weather](docs/weather.md#weather-from-weathergov). |

Press Ctrl+C to stop.

## Documents

| Document | Contents |
|---|---|
| [The playbook](docs/playbook.md) | The settings of the `configuration` section, the exit codes, and the schema for an editor. |
| [Commands](docs/commands.md) | All the playbook commands and their settings. |
| [ESQ settings](docs/esq-settings.md) | The settings of `!!configuration` and `!!newLookConfiguration`. |
| [Watch mode](docs/watch-mode.md) | Send the changes of the guide at an interval. |
| [Automatic promos](docs/auto-promos.md) | Show promos at an interval. |
| [Web server](docs/web-server.md) | The control page, the jobs and the HTTP API. |
| [Connect to an Amiga](docs/amiga.md) | The wiring to the serial port of an Amiga. |
| [Connect from a Raspberry Pi](docs/pi.md) | The wiring and the UART setup of a Pi. |
| [The clock and daylight saving time](docs/clock.md) | How ESQ moves the clock, and the file `dst.dat`. |
| [Local ads](docs/local-ads.md) | How to show a local ad, and the graphic ads of ESQ. |
| [Weather](docs/weather.md) | The 3-day forecast, and the weather from weather.gov. |
| [Send your own IFF picture](docs/pictures.md) | The picture formats that ESQ loads, and how to make a picture smaller. |
| [Control line](docs/control-line.md) | How to keep the top half clear, and the packet types. |
| [Test with an emulator](docs/testing.md) | Tests with Esquire and FS-UAE, and the feed file tools. |
| [Development](docs/development.md) | Build, test, the standalone programs and the project layout. |
| [Glossary](docs/glossary.md) | The words of these documents, for example unit, session and brush. |

## Command-line options

An option replaces a setting of the playbook for one run. Thus you can use one playbook with different systems. Run `dotnet run --project PrevueCommander -- --help` to show the options.

| Option | Setting | Description |
|---|---|---|
| `--host <name>` | `hostname` | The host of the TCP bridges. |
| `--port <n>` | `port` | The TCP port of the data line. It replaces `serialPort` of the playbook. |
| `--serial-port <name>` | `serialPort` | A serial port for the data line. |
| `--baud-rate <n>` | `baudRate` | The speed of the data line. |
| `--control-port <n>` | `controlPort` | The TCP port of the control line. It replaces `controlSerialPort` of the playbook. |
| `--control-serial-port <name>` | `controlSerialPort` | A serial port for the control line. |
| `--control-baud-rate <n>` | `controlBaudRate` | The speed of the control line. |
| `--now <date>` | `now` | The time of the run, for example `1993-02-23T18:00`. |
| `--verbose` | `output` | Shows the bytes of each command. |
| `--watch <minutes>` | `watchMinutes` | After the commands, reads the guide again at this interval and sends only the changes. See [Watch mode](docs/watch-mode.md). |
| `--weather <minutes>` | `weatherMinutes` | After the commands, reads the weather of `!!weatherGov` again at this interval and sends it. See [Weather from weather.gov](docs/weather.md#weather-from-weathergov). |
| `--serve <port>` | `servePort` | After the commands, starts the web server on this port. See [Web server](docs/web-server.md). |
| `--serve-host <address>` | `serveHost` | The address of the web server. `0.0.0.0` accepts all the computers of the network. |
| `--validate` | | Makes all the commands and shows them, but does not connect. Use it to check a playbook. |
| `--output <file>` | | Writes the bytes of the data line to the file, but does not connect. See [Replay a feed file](docs/testing.md#replay-a-feed-file). |
| `--control-output <file>` | | With `--output`, writes the bytes of the control line to the file. Without it, `--output` leaves out the control line commands. |
| `-h`, `--help` | | Shows the options. |

You can write an option with a value as `--port 5401` or as `--port=5401`. For example, this command sends the playbook to a second Esquire on port 5401:

```sh
dotnet run --project PrevueCommander -- --port 5401 playbook.yaml
```
