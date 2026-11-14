# Prevue Commander

## What is this?

Prevue Commander sends data to a Prevue Guide system. The system can be an emulator, such as Esquire or FS-UAE, or a real Amiga on a serial port of this computer.

ESQ, the Prevue Guide program, reads two serial lines:

- The **data line** gets the data that the satellite data feed sent. Prevue Commander sends the channel lineup, the listings, local text ads, the configuration, the clock, the daylight saving time window, pictures and other files on this line.
- The **control line** is a 110 baud line. The commands on this line show promos of programs and logos in the top half of the screen, and move the grid.

You write the commands in a YAML file, the playbook. Prevue Commander reads the playbook and sends its commands in order.

### Documents

| Document | Contents |
|---|---|
| [Web server](docs/web-server.md) | The jobs and the HTTP API of the web server. |
| [Connect from a Raspberry Pi](docs/pi.md) | The wiring and the UART setup of a Pi. |
| [The clock and daylight saving time](docs/clock.md) | How ESQ moves the clock, and the file `dst.dat`. |
| [Local ads](docs/local-ads.md) | How to show a local ad, and the graphic ads of ESQ. |
| [Weather](docs/weather.md) | The 3-day forecast, and the weather from weather.gov. |
| [Send your own IFF picture](docs/pictures.md) | The picture formats that ESQ loads, and how to make a picture smaller. |
| [Control line](docs/control-line.md) | How to keep the top half clear, and the packet types. |
| [Test with an emulator](docs/testing.md) | Tests with Esquire and FS-UAE, and the feed file tools. |

## What is Prevue Guide?

If you had cable TV in the 1980s or 1990s, you may remember [this channel](https://www.youtube.com/watch?v=ceukujHQvwM) in your lineup. An Amiga at the cable company ran it. The program of the Amiga is ESQ ("Esquire"), and it [was recovered](https://prevueguide.com/wiki/Prevue_(ESQ)).

### ESQ versions

Prevue Commander was developed and tested with ESQ 9.0.4. Other versions of ESQ can support different features. For example, ESQ 7.3.13 does not support colored text in local ads. If a command does not work as this README tells, look at the version of ESQ.

The fonts of ESQ 9.0.4 have the Latin-1 characters, for example `é`, `ñ` and `©`. Prevue Commander sends these characters by default. For an older version of ESQ, set `characterSet: Ascii`. The fonts of older versions possibly do not have these characters.

## Words in this README

| Word | Meaning |
|---|---|
| ESQ | The Prevue Guide program that runs on the Amiga. |
| Data line | The serial line that sends the guide data to ESQ. The satellite feed used 2400 baud. |
| Control line | A second, slow serial line (110 baud). It tells ESQ what to show in the top half of the screen. |
| Data line command | One record on the data line. It starts with the bytes `55 AA`, then a letter that names the command, then its data and a checksum. For example, 'A' is the address command and 'L' is a local ad. This README writes the letter in quotes, for example 'F'. The case of the letter is important: 'F' and 'f' are different commands. |
| Control packet | One record on the control line. It starts with a type number from 1 to 22. See [Packet types](docs/control-line.md#packet-types). |
| Unit | One Prevue machine. A cable company could have many units on one satellite feed. Each unit has an address. |
| Session | A group of data line commands for the selected units: an address command, the commands, and a box off command. See [Commands](#commands). |
| Status packet | The 'F' command (`!!configuration`). It has the time zone, the scroll speed and the daylight saving time setting. ESQ needs it before the listings. |
| Configuration record | The 'f' command (`!!newLookConfiguration`). It has the settings of the screen, the local ads and the logos. |
| Box off | The command that ends a session (`!!boxOff`). ESQ saves the data at the box off if the session has a save command. |
| Broadcast day | The day of the listings. It starts at 5:00 AM and has 48 half-hour slots. |
| Lineup | The list of channels in the grid, with their numbers and call letters. |
| Call letters, call sign | The name of a channel in the grid, for example `KTIVDT`. ESQ shows a maximum of 6 characters. |
| Source name | A short name of 1 to 6 letters or digits for each channel. ESQ connects the programs and the channel logos to a channel by this name. |
| Promo | A box in the top half of the screen that shows the next time of one program. |
| Brush | An IFF picture of ESQ, for example the background of a promo box. The file `BRUSH.INI` on the drive gives each brush a two-letter ID, for example `AT`. |
| Logo | A picture in the top half of the screen. ESQ shows the logos of the file `LOGO.LST` in turn. A picture of `!!graphicAd` is a logo. |
| Genlock, genlock key | A genlock mixes the picture of the Amiga with a video signal. Where the picture has color 0 (the genlock key), the video shows through. |
| Drive, volume | The disk of the Amiga that ESQ uses. AmigaDOS names a volume with a colon. For example, `DF0:` is the drive of ESQ, and `RAM:` is a disk in memory. |
| Operator menu | The menu that an operator opens with the ESC key. It has the diagnostics screen and the ad editor. ESQ reads the keys of this menu on a pin of the serial port. Prevue Commander and Esquire do not send these keys. `!!utility` with `ToggleMenu` opens the menu. |
| Baud | The speed of a serial line. A byte takes 10 bits on the line, so 2400 baud sends 240 bytes each second. |

## Requirements

- The [.NET 11 SDK](https://dotnet.microsoft.com/download).
- A Prevue system, one of these:
  - An emulator that gives the serial port of ESQ on a TCP port. [Esquire](https://prevueguide.neocities.org/guides/Esquire) opens the data line on TCP port 5400. [FS-UAE](docs/testing.md#test-with-fs-uae) can also open the serial port of the Amiga on a TCP port.
  - An Amiga that runs ESQ, connected to a serial port of this computer or to the UARTs of a Raspberry Pi. See [Connect to an Amiga](#connect-to-an-amiga).
- Guide data, if you send listings: an XMLTV file (for example from [zap2xml](https://github.com/jef/zap2xml)), or a [Channels DVR](https://getchannels.com/dvr-server/) server.

## Run it

1. Start the Prevue system, and wait until the grid shows.
2. Run `dotnet run --project PrevueCommander -- playbook.yaml`.

If you do not give a playbook, Prevue Commander uses `playbook.yaml` in the current directory. If the playbook does not exist, Prevue Commander writes an example playbook to that path and stops. Edit the playbook, and run the command again.

A playbook has a `configuration` section and a list of `commands`. [`playbook.yaml`](playbook.yaml) is the example playbook.

### Edit a playbook in an editor

[`playbook.schema.json`](playbook.schema.json) is a JSON schema of the playbook. A YAML editor can use it to complete the settings and to find unknown settings. The first line of the example playbook names the schema:

```yaml
# yaml-language-server: $schema=https://raw.githubusercontent.com/rickz0rz/prevuecommander/main/playbook.schema.json
```

JSON schema has no YAML tags. Thus the editor must also know the tags of the commands. In Visual Studio Code with the YAML extension, add the tags to `yaml.customTags` in the settings:

```json
"yaml.customTags": [
    "!!address mapping",
    "!!banner mapping",
    "!!boxOff mapping",
    "!!channelsDVRGuideDataImport mapping",
    "!!clearTopHalf mapping",
    "!!clock mapping",
    "!!configuration mapping",
    "!!configureDst mapping",
    "!!controlPacket mapping",
    "!!fileTransfer mapping",
    "!!graphicAd mapping",
    "!!localAds mapping",
    "!!logoList mapping",
    "!!moveGrid mapping",
    "!!newLookConfiguration mapping",
    "!!pause mapping",
    "!!promo mapping",
    "!!reset mapping",
    "!!save mapping",
    "!!shell mapping",
    "!!showLogo mapping",
    "!!utility mapping",
    "!!xmlTvGuideDataImport mapping"
]
```

`dotnet run --project PrevueCommander -- schema` shows the schema, and `schema --tags` shows the tags. If you change a playbook class, write the schema again with `dotnet run --project PrevueCommander -- schema > playbook.schema.json`. A test fails if the schema of the repository is not up to date.

### Command-line options

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
| `--watch <minutes>` | `watchMinutes` | After the commands, reads the guide again at this interval and sends only the changes. See [Watch mode](#watch-mode). |
| `--weather <minutes>` | `weatherMinutes` | After the commands, reads the weather of `!!weatherGov` again at this interval and sends it. See [Weather from weather.gov](docs/weather.md#weather-from-weathergov). |
| `--serve <port>` | `servePort` | After the commands, starts the web server on this port. See [Web server](#web-server). |
| `--serve-host <address>` | `serveHost` | The address of the web server. `0.0.0.0` accepts all the computers of the network. |
| `--validate` | | Makes all the commands and shows them, but does not connect. Use it to check a playbook. |
| `--output <file>` | | Writes the bytes of the data line to the file, but does not connect. See [Replay a feed file](docs/testing.md#replay-a-feed-file). |
| `--control-output <file>` | | With `--output`, writes the bytes of the control line to the file. Without it, `--output` leaves out the control line commands. |
| `-h`, `--help` | | Shows the options. |

You can write an option with a value as `--port 5401` or as `--port=5401`. For example, this command sends the playbook to a second Esquire on port 5401:

```sh
dotnet run --project PrevueCommander -- --port 5401 playbook.yaml
```

### What happens in a run

Prevue Commander makes all the commands before it connects. If a command fails, for example because it cannot read the guide data, Prevue Commander sends nothing. If all the commands are correct, it connects and sends them in the order of the list.

Before the first command, Prevue Commander shows the total bytes, the expected time and the expected end time. Each 30 seconds, it shows the bytes sent and the new expected end time. The expected time is the bytes at `baudRate` or `controlBaudRate`, plus `commandDelay` after each command, plus the pauses. For example:

```text
The run sends 5,504 bytes. It takes about 43 s. Expected end: 19:45:48.
Sent 3,812 of 5,504 bytes (69%). Expected end: 19:45:48.
```

When Prevue Commander stops, it gives one of these exit codes:

| Code | Meaning |
|---|---|
| `0` | Prevue Commander sent all the commands. |
| `1` | The playbook did not exist. Prevue Commander wrote an example playbook. |
| `2` | The playbook is not valid YAML, or it has an unknown command or setting. |
| `3` | Prevue Commander cannot connect to the TCP bridge. |
| `4` | A command or a connection has an error, for example a file that does not exist or a serial port that cannot open. |
| `5` | The command line has an unknown option or a bad value. |
| `6` | `decode` found a command with a bad checksum. |
| `130` | You stopped Prevue Commander with Ctrl+C, or a service manager stopped it with SIGTERM. This is the usual end of the watch mode. |

## Configuration

Each line goes to a TCP bridge or to a serial port. A TCP bridge is a TCP port that an emulator opens for a serial line of the Amiga.

| Setting | Default | Description |
|---|---|---|
| `hostname` | `127.0.0.1` | The host of the TCP bridges. |
| `port` | `5400` | The TCP port of the data line. |
| `serialPort` | | A serial port of this computer for the data line, for example `/dev/tty.usbserial-1410`, `/dev/ttyUSB0` or `COM3`. When it is set, Prevue Commander writes to this port and does not use `port`. |
| `baudRate` | `2400` | The speed of the data line. Prevue Commander sends a maximum of `baudRate / 10` bytes each second. ESQ has no flow control, and it loses data when its buffer is full. `0` sends at full speed. Use `0` only with a TCP bridge that keeps the bytes and sends them at the speed of the line. |
| `commandDelay` | `100` | The pause in milliseconds after each command. |
| `controlPort` | `0` | The TCP port of the control line. Esquire opens it with `--prevue-ctrl-port`. `0` means no control line. |
| `controlSerialPort` | | A serial port of this computer for the control line. When it is set, Prevue Commander writes to this port and does not use `controlPort`. |
| `controlBaudRate` | `110` | The speed of the control line. ESQ reads it at 110 baud, so a promo takes about 2 seconds. Esquire keeps the bytes and sends them at the speed of the line, so `0` also works with Esquire. |
| `output` | `Minimal` | `Minimal` shows each command. `Verbose` also shows its bytes. |
| `now` | | A local date and time that the playbook uses in place of the clock of this computer, for example `1993-02-23T18:00`. The imports send the listings of the broadcast day of this time and of the next day. `!!clock` without `date` sends this time, and `!!configureDst` without `year` uses its year. A Channels DVR server has only current listings, so use an XMLTV file for a date in the past. |
| `watchMinutes` | `0` | The minutes between two reads of the guide in the [watch mode](#watch-mode). `0` stops after the commands. |
| `weatherMinutes` | `0` | The minutes between two reads of the weather of `!!weatherGov`. See [Weather from weather.gov](docs/weather.md#weather-from-weathergov). `0` stops after the commands. |
| `servePort` | `0` | The TCP port of the [web server](#web-server). `0` starts no server. |
| `serveHost` | `127.0.0.1` | The address of the web server. `127.0.0.1` accepts only this computer. `0.0.0.0` accepts all the computers of the network. |
| `characterSet` | `Latin1` | The characters of the listings, the local ads and the promo titles. `Latin1` sends letters with accents and other Latin-1 characters, for example `Fiancé`. `Ascii` removes the accents, for example `Fiance`, for older versions of ESQ. See [ESQ versions](#esq-versions). |

Prevue Commander connects to a line only when a command needs it. A playbook with only control line commands does not connect to the data line. If a playbook has control line commands, but `controlPort` and `controlSerialPort` are not set, Prevue Commander stops before it sends a command.

## Watch mode

In the watch mode, Prevue Commander stays connected after the commands. At each interval, it reads the guide of the imports again and sends only the changes. Set `watchMinutes` in the configuration, or use `--watch`:

```sh
dotnet run --project PrevueCommander -- --watch 10 playbook.yaml
```

At each interval, Prevue Commander follows these rules:

- If the lineup of a day changed, it sends the lineup and all the programs of that day. ESQ deletes the programs of a day when its lineup changes.
- If the lineup of a day did not change, it sends only the new and changed programs.
- At 5:30 AM, ESQ changes to the next day. The new next day gets its lineup and all its programs.
- It sends the changes in a session. The session has the `!!address` and the `!!configuration` of the playbook, the changes, `!!save` if the playbook has it, and `!!boxOff`. If the guide has no changes, it sends nothing.
- It does not send the channel logos again.
- If it cannot read the guide, for example because the server does not answer, it shows the error. It tries again at the next interval.

ESQ cannot delete one program. Thus a program that is not in the new guide stays until a new lineup or the next day.

Press Ctrl+C to stop the watch mode. Prevue Commander also stops correctly when a service manager, for example systemd, sends SIGTERM. `--validate` and `--output` do not use the watch mode.

## Automatic promos

With an `autoPromo` section, Prevue Commander stays connected after the commands and sends a promo at each interval. It chooses the program from the listings that it sent, so ESQ always finds the program. The promos need the control line. The section is beside `configuration` and `commands`:

```yaml
autoPromo:
  intervalSeconds: 120
  movies: true
  brush: AT
```

| Setting | Default | Description |
|---|---|---|
| `intervalSeconds` | `120` | The seconds between two promos, 5 or more. The first promo comes directly after the commands. |
| `withinHours` | `3` | The hours after the current half hour for the start of the program, 0.5 to 24. |
| `includeNow` | `false` | `true` also chooses a program that starts in the current half hour. |
| `movies` | | `true` chooses only movies, `false` no movies. Without a value, all programs. |
| `premium` | | `true` chooses only premium channels, `false` no premium channels. Without a value, all channels. |
| `channels` | | Call letters (with `*` for any text) or channel numbers, for example `[KTIV*, '4']`. |
| `titles` | | Parts of titles, for example `[Seinfeld, News]`. Upper case and lower case are the same. |
| `order` | `Soonest` | `Soonest` chooses the next program. `Random` chooses by chance. |
| `repeat` | `10` | The number of the last promos whose titles do not show again, 0 to 100. If all the programs that fit were in these promos, the title of the oldest promo shows again. |
| `brush` | | The background of the box, for example `AT`. See [Control line](#control-line). |
| `side` | `Right` | The box of the promo: `Right` or `Left`. |
| `style` | `Box` | `Box` shows the background of the box. `Text` draws only the text. |

The promo of a movie finds the title in quotation marks, so that ESQ finds only that title. The promo uses the call letters of the channel as its channel pattern. With the [watch mode](#watch-mode), the promos use the listings of the last read of the guide. Without the watch mode, they use the listings of the commands. If no program fits, Prevue Commander shows a message and tries again at the next interval. Press Ctrl+C to stop.

## Web server

The web server lets you send commands while Prevue Commander runs. It has a control page for a browser, and an HTTP API for scripts. Set `servePort` in the configuration, or use `--serve`:

```sh
dotnet run --project PrevueCommander -- --serve 8095 playbook.yaml
```

Prevue Commander first sends the commands of the playbook. Then it starts the server and stays connected until you press Ctrl+C. The playbook can have no commands, for example a playbook with only the `configuration` section. The server can run together with the [watch mode](#watch-mode) and the [automatic promos](#automatic-promos). `--validate` and `--output` do not start the server.

Open `http://localhost:8095/` in a browser. The page has these parts:

- The status of the lines, the queues, the hold, the guide and the automatic promos. The page shows the new status each 2 seconds.
- Buttons for the top half of the screen: clear, logo, ad break, weather and forecast.
- Keep the top half clear for some minutes, or until you stop it. See [Keep the top half clear](docs/control-line.md#keep-the-top-half-clear).
- A promo form.
- A local ad editor with a preview of each ad, as ESQ shows it.
- A weather form, the clock, and a button that reads the guide again.
- A box for any playbook commands in YAML.
- The list of jobs, with their errors.

The page needs no internet connection, so it works on a Raspberry Pi without internet.

> [!WARNING]
> The server has no password. Anyone who can connect to the port can send commands to ESQ. The default address `127.0.0.1` accepts only this computer. Use `serveHost: 0.0.0.0` only on a network that you trust. On Windows, an address other than `127.0.0.1` can need administrator rights.

[Web server](docs/web-server.md) tells about the jobs and the queues, and has all the requests of the HTTP API.

## Connect to an Amiga

Prevue Commander can write to the serial ports of this computer, in place of a TCP bridge. Use a USB to RS-232 adapter with RS-232 voltages. Do not use a TTL (3.3 V or 5 V) adapter.

Each port sends 8 data bits, no parity and 1 stop bit, without handshake. The data line port sends at `baudRate`, and the control line port sends at `controlBaudRate`. Thus these settings cannot be `0` with a serial port.

The serial port of the Amiga is a DB25 connector. Connect these pins:

| Line | Computer | Amiga DB25 pin |
|---|---|---|
| Data line | TXD of the first port | 3 (RXD) |
| Control line | TXD of the second port | 5 (CTS) |
| Both | GND of each port | 7 (GND) |

ESQ reads the data line with the receiver of the serial port. The original satellite data feed used 2400 baud. ESQ reads the control line bit by bit on the CTS pin, at 110 baud. Thus the control line needs a second serial port of this computer. Connect its TXD to the CTS pin of the Amiga. If you do not use the control line, you need only one port.

ESQ does not send data back, so the RXD pins of this computer stay open.

For example:

```yaml
configuration:
  serialPort: /dev/tty.usbserial-A10
  baudRate: 2400
  controlSerialPort: /dev/tty.usbserial-B20
  controlBaudRate: 110
```

On Linux, your user must be a member of the group of the port, for example `dialout`. On macOS, use the `/dev/tty.*` or `/dev/cu.*` name of the adapter.

The wiring is from the ESQ code and the Amiga documentation. It was not tested on a real Amiga. Tests with pseudo terminals and Esquire showed that the serial port output is correct.

### Connect from a Raspberry Pi

A Raspberry Pi can run Prevue Commander next to the Amiga. Its UARTs can send the two lines. See [Connect from a Raspberry Pi](docs/pi.md).

## Commands

Each command has a YAML tag, for example `!!localAds`. Write a command without settings with `{}`, for example `!!save {}`. A setting in parentheses in the tables below shows its default value.

A session on the data line has this order:

1. `!!address` selects the units.
2. `!!configuration` sends the status packet. ESQ needs it before a channel lineup or local ads.
3. The other data line commands, for example the listings and the local ads.
4. `!!save`, if ESQ must keep the data after a restart.
5. `!!boxOff` ends the session.

The control line commands do not need a session. Send them after the listings, because a promo finds its program in the listings.

### Session

| Command | Settings | Description |
|---|---|---|
| `!!address` | `target` (`*`) | Selects the units (Prevue machines) that accept the next commands. ESQ accepts no other command before it. `*` selects all units. Each unit has an address, so a cable company could send different data to different units. |
| `!!save` | | Tells ESQ to save the received data to its drive at the next box off. Without it, the data stays only in memory. |
| `!!boxOff` | | Ends the session. Send it last. |
| `!!reset` | `target` (`*`) | Reboots the Amiga after about 10 seconds. ESQ accepts a reset only directly after an address command, so this command sends both. |

### Configuration and time

| Command | Settings | Description |
|---|---|---|
| `!!configuration` | `configuration`: see the table below | Sends the status packet ('F'). ESQ needs it before a channel lineup or a change of the local ads. |
| `!!newLookConfiguration` | `configuration`: see the table below | Sends the configuration record ('f'). ESQ saves it to `config.dat`. |
| `!!clock` | `date`, `useCurrentDate` (`false`) | Sets the clock of ESQ. Without `date`, it sends the local time of this computer. With `date`, for example `2020-11-01T16:00`, it sends that time. `useCurrentDate: true` sends the local time also when `date` is set. ESQ accepts the clock only when `clockCommandInFSetting` of `!!newLookConfiguration` is `'2'` (the default). See [The clock and daylight saving time](docs/clock.md#the-clock-and-daylight-saving-time). |
| `!!configureDst` | `year` (the current year) | Sends the daylight saving time window of the year, from the time zone of this computer. ESQ writes the window to `dst.dat` only after `!!save`. See [The clock and daylight saving time](docs/clock.md#the-clock-and-daylight-saving-time). |

The settings of `!!configuration`:

| Setting | Default | Description |
|---|---|---|
| `timeZone` | `'6'` | A character from `'1'` to `'9'` or from `'A'` to `'H'`. ESQ adds (time zone - 6) hours to the clock and to the times of the grid. For example, `'7'` shows the times one hour later, and `'5'` one hour earlier. Prevue Commander sends local times, so keep `'6'` unless you want another time on the screen. |
| `scrollSpeed` | `'3'` | The speed of the grid, from `'1'` to `'8'`. ESQ uses `'4'` for another value. `'3'` is the value of the original data files. |
| `daylightSavingTime` | `true` | `true` if the area of the units uses daylight saving time. Send the dates of the change with `!!configureDst`. |
| `clockOffsetMinutes` | `0` | Minutes, from 0 to 59, that ESQ adds to the clock that it shows. |

The other values of the status packet are the values of the original data files. A playbook cannot change them.

The settings of `!!newLookConfiguration`. Each setting is one character, except the numbers. The defaults are the values of the original data files. The names come from the diagnostics screen of ESQ. For many settings, what ESQ does is not known. Keep the default of these settings.

| Setting | Default | What ESQ does |
|---|---|---|
| `rollAndHoldTime` | `'2'` | The refresh interval in minutes. |
| `gridRollAndHold` | `'N'` | `'Y'` stops the grid for a short time after each 3 listings. |
| `militaryTime` | `'N'` | `'Y'` shows the clock in 24-hour format. |
| `cleanUpUnusedLogos` | `'Y'` | `'Y'` scans the directory `DH2:logos` before ESQ reads `LOGO.LST` again. Without that directory, ESQ stops. Use `'N'` before `reload: true` or `ReloadLogoList`. See [Files and utilities](#files-and-utilities). |
| `localTextAdSource` | `'S'` | The source of the local ads: `'N'`, `'L'`, `'R'` or `'S'`. See [Local ads](#local-ads). |
| `pcDiskSupport` | `'N'` | `'Y'` uses the volume `PC1:` for the graphics. |
| `lrbnBannerControl` | `'N'` | `'Y'` lets the control line move the grid (`!!moveGrid`). |
| `lrbnModeSelector` | `'N'` | `'L'`, `'R'`, `'B'` or `'N'`. How the control line moves the grid in one of its modes. The effect on the screen is not known. |
| `msnMode` | `'N'` | `'M'` changes the promo, logo and text commands of the control line to a clear of the top half. |
| `clockCommandInFSetting` | `'2'` | `'2'` tells ESQ to accept the clock of `!!clock`. With `'1'`, ESQ keeps its own clock. |
| `displayLength` | `'C'` | `'C'` tells ESQ to use the next byte of the record for the banner. |
| `numberOfColorPalettes` | `'8'` | ESQ always uses 8. |
| `displayMode` | `'G'` | ESQ keeps and saves this setting, but does not use it. |
| `primeTimeStartingSlot` | `29` | A number from 0 to 99. Not known. |
| `primeTimeLookAhead` | `6` | A number from 0 to 99. Not known. |
| `primeTimePromotionTimeSlotStart` | `23` | A number from 0 to 99. Not known. |
| `primeTimePromotionTimeSlotEnd` | `36` | A number from 0 to 99. Not known. |
| `lengthOfLookAheadSportsSummary` | `6` | A number from 0 to 99. Not known. |
| `minutesToDisplayPPVPostStart` | `15` | A number from 0 to 999. Not known. |
| `summaryCycleFrequency` | `'1'` | `'1'` to `'9'`. Not known. |
| `primeTimeSummary`, `sportsSummary` | `'0'`, `'1'` | A digit. Not known. |
| `laserDiscInsertionType` | `'L'` | `'L'`, `'S'` or `'V'`. Not known. |
| `gridSourceChannelOrder` (`'C'`), `mrReviewInFSetting` (`'0'`), `sbsFrequency` (`'1'`), `advueInsertion` (`'N'`), `pieFormat` (`'A'`), `language` (`'E'`), `sumBySourceRollAndHold` (`'N'`), `movieRecapRollAndHold` (`'N'`), `primeTimeSummaryRollAndHold` (`'N'`), `sportsSummaryRollAndHold` (`'N'`), `videoVueInsertion` (`'N'`), `cycleSummaryInfo` (`'Y'`), `gridSynopsis` (`'Y'`), `lockGridOnSynopsis` (`'Y'`), `weatherHoldAndRoll` (`'Y'`) | (in parentheses) | Not known. |

For example:

```yaml
- !!newLookConfiguration
  configuration:
    militaryTime: 'Y'
    cleanUpUnusedLogos: 'N'
```

A setting that is not in the playbook keeps its default. ESQ saves the configuration record to `config.dat` on its drive.

#### The clock and daylight saving time

`!!clock` also sends a daylight saving time flag. ESQ compares it with its DST window, and can move the clock by one hour. Send `!!configureDst` in the same playbook. See [The clock and daylight saving time](docs/clock.md).

### Listings

| Command | Settings | Description |
|---|---|---|
| `!!xmlTvGuideDataImport` | `xmlTvFiles`: a list of `path` and `maximumNumberOfChannels` (`0`) | Imports XMLTV files. A `path` is a file or an HTTP or HTTPS address, for example `http://192.168.0.10/guide.xml`. A relative path starts at the directory of the playbook. A file that is compressed with gzip, for example `guide.xml.gz`, is also correct. |
| `!!channelsDVRGuideDataImport` | `channelsDVRServers`: a list of `serverAddress`, `maximumNumberOfChannels` (`0`), `filterOnlyHDChannels` (`false`) and `filterMusicChoiceChannels` (`true`) | Imports the guide of Channels DVR servers, for example `http://192.168.0.10:8089`. `filterOnlyHDChannels: true` imports only the HD channels. `filterMusicChoiceChannels: true` leaves out the channels with a call sign that starts with `MC`. |

`maximumNumberOfChannels` is the maximum number of channels from one file or server. `0` means 200, the maximum of ESQ.

The two imports also have these settings:

| Setting | Default | Description |
|---|---|---|
| `sendChannelLineUp` | `true` | Sends the channel lineup. If the lineup changed, ESQ deletes the programs of the day, so all the programs follow it. |
| `channelNumberOrder` | `None` | The order of the channels in the grid: `None` (the order of the guide data), `Ascending` or `Descending`. |
| `premiumChannels` | | Channel numbers or call signs. ESQ shows their programs on a red background. |
| `excludedChannels` | | Channel numbers or call signs to leave out. |
| `showStereo` | `false` | Shows the stereo symbol for programs with stereo sound. See the rules below. |
| `channelLogos` | | Sends a logo for each channel that has a logo image. See [Channel logos](#channel-logos). |

ESQ keeps the listings of two broadcast days. A broadcast day starts at 5:00 AM and has 48 half-hour slots. ESQ changes to the next day at 5:30 AM. An import sends the lineup and the programs of the current day and of the next day.

The import follows these rules:

- ESQ keeps a maximum of 200 channels. If the files and servers give more channels, the import sends the first 200 channels and shows a warning.
- Each channel gets a source name of 1 to 6 letters or digits from its call sign, in upper case. If two channels get the same name, the second name ends with a number, for example `WJBKD2`. ESQ finds the programs and the channel logos of a channel by its source name.
- The grid shows the call sign. ESQ shows a maximum of 6 characters of it.
- A program goes into the slot in which it starts. If it does not start at :00 or :30, the text starts with the time, for example `( 8:15) News`.
- The text has the format of the listings tool of Esquire. A movie is `"Title" (year) summary (rating)`. A sports event is `Title: event title`. Programs with closed captions end with `|`, which ESQ shows as the closed caption symbol.
- With `characterSet: Latin1` (the default), Latin-1 characters stay, for example `é`. A letter with an accent outside Latin-1 loses its accent. Typographic quotation marks and dashes become ASCII. Other characters are removed.
- With `characterSet: Ascii`, all letters lose their accents.
- The text has a maximum of 120 characters.
- With `showStereo: true`, the import marks each channel of the lineup as a stereo channel. This is bit 7 of the attribute byte of the channel. It adds `In Stereo` before the closed caption symbol of each program with stereo sound. ESQ shows the stereo symbol in place of that text. If the text is then longer than 120 characters, the text before `In Stereo` becomes shorter. A Channels DVR program has stereo sound if it has the tag `Stereo`. An XMLTV program has stereo sound if it has an `<audio><stereo>` value that is not `mono`.

### Channel logos

With `channelLogos`, an import also sends channel logos. Channels DVR gives a logo image for most channels. An XMLTV file can give it in the `<icon src="...">` element of a channel. A channel without a logo image gets no channel logo.

Prevue Commander downloads each PNG image and makes a picture of 640 by 240 pixels. The image is on a light card at the left, on the navy background of Prevue. ESQ writes the call letters and the channel number on the right side, for example "WJBKDT on Channel 2.4".

The logos go to `DF0:Logos/Channels`. The name of each file is the source name of its channel, for example `WJBKD4`. ESQ shows a channel logo only when its file name is the source name of a channel in the lineup. A test showed this: the logo `WJBKD4` showed on the channel with the call sign `WJBKDT4`.

After the logos, the import sends a new `LOGO.LST`. It has the lines of `keepLines` and a line for each channel logo. A channel logo line has no comma.

| Setting | Default | Description |
|---|---|---|
| `keepLines` | | The other lines of `LOGO.LST` to keep, for example `Logos/tvgsport.uv,`. Prevue Commander cannot read the `LOGO.LST` of the drive, so give all the lines to keep. |
| `reload` | `false` | Tells ESQ to read the new `LOGO.LST` at once. This needs `cleanUpUnusedLogos: 'N'` (see [Files and utilities](#files-and-utilities)). Without `reload`, the logos show after a restart of ESQ. |
| `cacheDirectory` | | A directory for the downloaded images, so that the next run does not download them again. A relative path starts at the directory of the playbook. |
| `saveTo` | | A local directory that gets a copy of each IFF file. |

A channel logo has about 6 to 12 KB. At 2400 baud, each logo takes about 40 seconds, so the logos of 50 channels take about 30 minutes. Use `maximumNumberOfChannels` or `excludedChannels` to send fewer logos.

```yaml
- !!newLookConfiguration
  configuration:
    cleanUpUnusedLogos: 'N'
- !!channelsDVRGuideDataImport
  channelsDVRServers:
  - serverAddress: http://192.168.0.10:8089
    maximumNumberOfChannels: 20
  channelLogos:
    keepLines: [ 'Logos/tvgsport.uv,', 'Logos/Enews.uv,' ]
    reload: true
    cacheDirectory: logo-cache
```

### Local ads

| Command | Settings | Description |
|---|---|---|
| `!!localAds` | `ads`: a list of texts | Deletes the local ads of ESQ and sends the new ads. ESQ keeps a maximum of 45 ads. Send `!!configuration` before it. |

A local ad is a text ad of the cable company, for example for a local shop. ESQ shows it in the top half of the screen.

`localTextAdSource` of `!!newLookConfiguration` sets the source of the local ads:

| Value | Local ads | 'L' commands of the data line | Ad editor of the operator menu |
|---|---|---|---|
| `'N'` | Off. ESQ shows no local ads. | Ignored | Not available |
| `'L'` | Local: an operator types the ads in the operator menu of the Amiga. | Ignored | Available |
| `'R'` | From the data line. | Accepted | Not available |
| `'S'` (default) | From the data line. | Accepted | Not available |

The code of ESQ 9.0.4 does the same for `'R'` and `'S'`. The names of these two values are not known. Use `'S'` or `'R'` for the ads of `!!localAds`. ESQ also keeps the ads in the file `local.ads` on its drive.

An ad has a maximum of 6 lines of 40 characters. An alignment token is not a character. ESQ does not wrap words. When a line is full, ESQ continues the text on the next line, also in the middle of a word. The next line has no alignment, so ESQ shows it at the left. ESQ drops the text after line 6. A test with Esquire showed these limits.

`!!localAds` shows each ad as ESQ shows it, with a warning for text that ESQ breaks or drops. For example:

```text
Local ad 2:
  |ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcd|
  |efghij                                  |
  Warning: Line 1 is longer than 40 characters. ESQ continues it on line 2, also in the middle of a word.
```

An ad can have these tokens:

| Token | Description |
|---|---|
| `%COLOR%` and two colors | The background color and then the text color of the text that follows, for example `%COLOR%%BLACK%%CYAN%`. ESQ 7.3.13 does not support colored text. |
| `%TRANSPARENT%`, `%WHITE%`, `%BLACK%`, `%YELLOW%`, `%RED%`, `%CYAN%`, `%GRAY%`, `%BLUE%` | The colors 0 to 7 of the default palette. |
| `%LEFT%`, `%CENTER%`, `%RIGHT%` | The alignment of a line. A token that is not at the start of a line starts a new line. |
| `%%` | A `%` character. |

ESQ shows a local ad only at an ad break of the control line. See [Local ads](docs/local-ads.md).

### Weather

ESQ shows the weather in the top half of the screen. The control line command `!!showWeather` starts it. The weather has three parts:

- The status (`!!weatherStatus`, the 'I' command) has the city and a label.
- The text (`!!weatherText`, the 'W' command) has the lines, for example a forecast.
- The 3-day forecast (`!!weatherForecast`, the 'w' command) has a picture, a high and a low for each day.

Send them in a session on the data line. Then send `!!showWeather` on the control line. `!!showWeather {}` shows the current weather, from the status and the text. `!!showWeather` with `forecast: true` shows the 3-day forecast.

| Command | Settings | Description |
|---|---|---|
| `!!weatherStatus` | `digit` (`'1'`), `label`, `text` | Sets the weather status. ESQ shows `text` at the top, for example the city. `label` has a maximum of 10 characters. The digit `'0'` clears the weather. ESQ saves the status in `curday.dat`. |
| `!!showWeather` | `forecast` (`false`) | Shows the current weather (`W0`). With `forecast: true`, it shows the 3-day forecast (`W3`). |
| `!!weatherText` | `minutes` (`60`), `icon` (`1`), `label` (`*`), `lines`, `color` (`1`) | Sends the weather text, for example a forecast or an alert. ESQ keeps it only if `label` matches the label of the status. `*` and `?` are wildcards, and upper case and lower case are different. ESQ shows the text for `minutes` (1 to 255). `icon` 2 to 6 shows the picture `PWI1` to `PWI5` of the drive, and `1` shows no picture. `lines` has a maximum of 10 lines. `color` shows only on the diagnostics screen of the operator menu. |
| `!!weatherForecast` | `minutes` (`60`), `label` (`*`), `days` | Sends the 3-day forecast. See [3-day forecast](docs/weather.md#3-day-forecast). |
| `!!weatherGov` | `zip`, or `latitude` and `longitude`, and other settings | Gets the weather of a US location from weather.gov, and sends the status, the current weather and the 3-day forecast. See [Weather from weather.gov](docs/weather.md#weather-from-weathergov). |

The status text has a maximum of 39 bytes, with the digit, the label, a separator and the end. Prevue Commander stops before it sends a longer status, because ESQ ignores it. ESQ shows the lines of the weather text in pairs. The first line of a pair is at the left of the picture, and the second line is at the right. A text can have printable ASCII characters. It can also have the Latin-1 characters of the ESQ 9.0.4 fonts, for example `°` and `é`. A label can have printable ASCII characters only. This example shows a sun and four lines:

```yaml
- !!address
  target: '*'
- !!weatherStatus
  label: DETROIT
  text: Detroit, Michigan
- !!weatherText
  minutes: 30
  icon: 2
  label: DETROIT
  lines: [Tonight - Clear, Low 32, Saturday - Sunny, High 45]
- !!boxOff {}
- !!pause
  seconds: 5
- !!showWeather {}
```

[Weather](docs/weather.md) tells about the 3-day forecast and the weather from weather.gov.

### Files and utilities

| Command | Settings | Description |
|---|---|---|
| `!!fileTransfer` | `source`, `target`, `address` (`*`) | Sends a local file to the drive of ESQ. The target is an AmigaDOS path that starts with a volume name of 4 characters, for example `DF0:Logos/MYAD.IFF`. `DF0:` is the drive of ESQ. ESQ accepts one file for each address command, so this command first sends a box off command and an address command. |
| `!!utility` | `action` | Tells ESQ to read a file of its drive again, or to show a screen. See the table of actions below. |
| `!!shell` | `commandLine` | Runs an AmigaDOS command line on the Amiga, for example `Delete DF0:OLD.IFF`. The command line has a maximum of 78 characters. ESQ shows no output. |

ESQ first writes a received file to `RAM:`, in the same directories as the target. Then it copies the file to the target. If a directory does not exist in `RAM:` or on the target volume, the file does not arrive. Thus, for a target in a directory, `!!fileTransfer` first sends `MakeDir` commands for the directories in `RAM:` and on the target volume.

The actions of `!!utility`. Use a reload action after you send a new file with `!!fileTransfer`.

| Action | What ESQ does |
|---|---|
| `ReloadGraphicAds` | Reads the graphic ads of `gfx/g_ads.data` again. It does not read `LOGO.LST` again. |
| `ReloadLogoList` | Reads `gfx/g_ads.data` and `LOGO.LST` again, and starts at the first line of the list. See the warning below. |
| `RebuildBrushList` | Makes the list of brushes again from the table in the program. |
| `ReloadBrushes` | Reads `BRUSH.INI` again: the brushes for the promo boxes. |
| `ReloadGradient` | Reads `gradient.ini` again. |
| `ReloadBanner` | Reads `BANNER.INI` and the banner picture again. |
| `ReloadDefaults` | Reads `default.ini` again. |
| `ReloadQTable` | Reads `qtable.ini` again. |
| `ReloadSourceConfiguration` | Reads `SourceCfg.ini` again. |
| `ToggleMenu` | Opens or closes the operator menu. |
| `ShowDiagnostics` | Shows the diagnostics screen. |
| `ShowVersion` | Shows the version screen. |

> [!IMPORTANT]
> If `cleanUpUnusedLogos` is `'Y'` (the default), `ReloadLogoList` stops ESQ. Before ESQ reads the list, it scans the directory `DH2:logos`. Without that directory, ESQ stops. Tests in Esquire and FS-UAE gave the same result. Send `cleanUpUnusedLogos: 'N'` in `!!newLookConfiguration` before you use `ReloadLogoList`.

ESQ always reads `LOGO.LST` when it starts, so a restart with `!!reset` also loads a new list. A restart takes about 50 seconds in FS-UAE.

ESQ has no command for a title text. To change the banner, send a new banner file with `!!fileTransfer`, and then send `!!utility` with `ReloadBanner`.

### Pictures

`!!graphicAd` and `!!banner` convert PNG files to the IFF pictures of ESQ, and send them with file transfers. The PNG file can have any size. `!!logoList` sends the list of logos that ESQ shows in turn in the top half of the screen.

| Command | Settings | Description |
|---|---|---|
| `!!graphicAd` | `source`, `name`, `resolution` (`Low`), `fit` (`Contain`), `background` (`#000044`), `keyBackground` (`true`), `saveTo`, `address` (`*`) | Sends a picture for the top half of the screen to `DF0:Logos/Ads/<name>`. The default name is the first 8 letters and digits of the file name, in upper case. |
| `!!logoList` | `lines`, `reload` (`false`), `address` (`*`) | Sends a new `LOGO.LST` with these lines. With `reload`, ESQ reads the new list at once. |
| `!!banner` | `source`, `fit` (`Contain`), `background` (`#000033`), `height` (`160`), `reload` (`true`), `saveTo`, `address` (`*`) | Sends the banner between the rows of the grid to `DF0:Banner/ADBANNER.UV`, and a `BANNER.INI` with its name. With `reload`, ESQ shows the new banner at once. |

The settings of the pictures:

- `resolution`: `Low` is 320 by 240 pixels with 32 colors, for photos and color gradients. `High` is 640 by 240 pixels with 16 colors, for small text and thin lines. ESQ shows the top 220 rows.
- `fit`: `Contain` shows all the image, and the background fills the rest. `Cover` fills all the picture and cuts the edges of the image. `Stretch` fills all the picture and changes the shape of the image.
- `background`: the color around the image and behind its transparent pixels.
- `keyBackground`: `true` makes the background the genlock key (color 0). With a genlock, the video then shows through the background.
- `saveTo`: a local directory that gets a copy of the IFF file.

The pixels of the Amiga are not square, so Prevue Commander gives the image the shape that it has on a TV. For a full ad, use an image with a ratio of 2.46 to 1, for example 1080 by 440 pixels. For a full banner, use a ratio of 3.4 to 1, for example 1088 by 320 pixels. The banner uses only 4 colors of its own, so use few colors.

ESQ shows an ad only when `LOGO.LST` has its line, for example `Logos/Ads/PIZZA,`. Prevue Commander cannot read the `LOGO.LST` of the drive, so `!!logoList` must give all the lines to keep. With `reload: true`, `!!logoList` tells ESQ to read the new list at once. This needs `cleanUpUnusedLogos: 'N'` (see [Files and utilities](#files-and-utilities)). For example:

```yaml
- !!newLookConfiguration
  configuration:
    cleanUpUnusedLogos: 'N'
- !!graphicAd
  source: ads/pizza.png
- !!logoList
  lines:
  - Logos/Ads/PIZZA,
  - Logos/tvgsport.uv,
  - Logos/Enews.uv,
  reload: true
- !!banner
  source: ads/banner.png
- !!boxOff {}
```

The ad shows in turn with the other logos, and `!!showLogo` shows the next logo at once. Without `reload`, the new list applies after a restart of ESQ, for example with `!!reset`.

To send an IFF picture that you made yourself, see [Send your own IFF picture](docs/pictures.md).

### Control line

The control line commands go to `controlPort` or `controlSerialPort`. ESQ reads them only when its display is not busy. Put a `!!pause` between two promos, so that each promo stays on the screen.

| Command | Settings | Description |
|---|---|---|
| `!!promo` | `right`, `left`: a box with `title`, `channels` (`*`) and `brush`. `first` (`Right`). `style` (`Box`) | Shows a promo in the top half of the screen. With `style: Text`, ESQ draws only the text, over what the top half shows at that time. |
| `!!moveGrid` | `position` (`A0`), `seconds` (`1`), `channels` | Moves the top of the grid up or down. `position` is two hex digits: a larger value is lower on the screen. `seconds` is the time of the move, from 0 to 9.9. This needs `lrbnBannerControl: 'Y'` in `!!newLookConfiguration`. Without it, ESQ shows the loaded logo. `!!clearTopHalf` moves the grid back. |
| `!!clearTopHalf` | | Removes the promo, the logo or the local ad, and moves the grid back. The top half shows the genlock video. The next logo of the rotation comes about 3 minutes later. See [Keep the top half clear](docs/control-line.md#keep-the-top-half-clear). |
| `!!showLogo` | | Shows the loaded logo of `LOGO.LST`. ESQ then loads the next logo. |
| `!!showWeather` | | Sends `W0`: shows the current weather. See [Weather](#weather). |
| `!!controlPacket` | `type` (1 to 22), `body` | Sends one packet, for the types that have no command. See [Packet types](docs/control-line.md#packet-types). In a double-quoted YAML text, `\x12` is the byte that separates the text of the two boxes. |
| `!!pause` | `seconds` (`1`) | Waits before the next command. It does not send data. |

A promo has a box on the right, a box on the left, or both boxes. ESQ finds the program in its listings, so send the listings first. A box has these settings:

- `title` is the title to find. A part of the title is sufficient, and upper case and lower case are the same. Put the title in quotes to find only the exact title. Prevue Commander converts the title with `characterSet`, as it converts the listings. Thus `Fiancé` finds the program in both modes.
- `channels` is a pattern for the channels to search, for example `KTIV*`. The ESQ code calls it a pattern for the call letters. The tests used only `*`. ESQ matches channel logos by source name, so the pattern possibly also matches source names.
- `brush` is the ID of the background in `BRUSH.INI` on the drive, for example `AT` (orange "around town") or `DT` (blue TV Guide). Without it, the box keeps its background.

The box shows the call letters, the title, the next time of the program and the channel number. The box shows the title as the promo gives it, not as the listings give it. For example, `title: family or fiancé` shows "family or fiancé". ESQ shows only one box. It tries the `first` box, and then the other box. If ESQ does not find a title, it shows the loaded logo.

[Control line](docs/control-line.md) tells how to keep the top half clear, and has all the packet types.

## Build and test

```sh
dotnet build
dotnet test
```

GitHub Actions runs these commands for each pull request and each push to `main`. The workflow is in `.github/workflows/build.yml`.

`build-osx.sh` makes `PrevueCommander-osx`, a universal macOS program for Apple silicon and Intel. `build-win.sh` makes a Windows x64 program in `publish_win-x64`. `build-linux-arm64.sh` makes a Linux arm64 program in `publish_linux-arm64`, for example for a Raspberry Pi. These programs do not need .NET on the computer. Run them with the playbook as the argument, for example `./PrevueCommander-osx playbook.yaml`.

Do not trim the output: YamlDotNet and the XMLTV reader need the metadata that trimming removes.

To see what ESQ does with each command, test with Esquire or FS-UAE. See [Test with an emulator](docs/testing.md).

## Project layout

| Project | Description |
|---|---|
| `Prevue.Core` | Broadcast days, slots, group codes and text conversion. |
| `Prevue.Graphics` | The PNG reader, the color reduction and the IFF writer for the pictures of ESQ. |
| `Prevue.Commands` | The commands of the data line and of the control line (`Control`), and the listings builder. Each class tells the format that ESQ reads. |
| `PrevueCommander.XmlTv` | The XMLTV reader. |
| `PrevueCommander.ChannelsDVR` | The Channels DVR client. |
| `PrevueCommander.WeatherGov` | The weather.gov client, the ZIP code table and the choice of the weather pictures. |
| `PrevueCommander` | The program, the playbook commands and the web server (`Server`, and the control page in `wwwroot`). |
| `PrevueCommander.Tests` | The tests. |
