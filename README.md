# Prevue Commander

## What is this?

Prevue Commander sends data to a Prevue Guide system. The system can be an emulator, such as Esquire or FS-UAE, or a real Amiga on a serial port of this computer.

ESQ, the Prevue Guide program, reads two serial lines:

- The **data line** gets the data that the satellite data feed sent. Prevue Commander sends the channel lineup, the listings, local text ads, the configuration, the clock, the daylight saving time window, pictures and other files on this line.
- The **control line** is a 110 baud line. The commands on this line show promos of programs and logos in the top half of the screen, and move the grid.

You write the commands in a YAML file, the playbook. Prevue Commander reads the playbook and sends its commands in order.

## What is Prevue Guide?

If you had cable TV in the 1980s or 1990s, you may remember [this channel](https://www.youtube.com/watch?v=ceukujHQvwM) in your lineup. An Amiga at the cable company ran it. The program of the Amiga is ESQ ("Esquire"), and it [was recovered](https://prevueguide.com/wiki/Prevue_(ESQ)).

### ESQ versions

Prevue Commander was developed and tested with ESQ 9.0.14. Other versions of ESQ can support different features. For example, ESQ 7.3.13 does not support colored text in local ads. If a command does not work as this README tells, look at the version of ESQ.

The fonts of ESQ 9.0.14 have the Latin-1 characters, for example `é`, `ñ` and `©`. Prevue Commander sends these characters by default. For an older version of ESQ, set `characterSet: Ascii`. The fonts of older versions possibly do not have these characters.

## Requirements

- The [.NET 11 SDK](https://dotnet.microsoft.com/download).
- A Prevue system, one of these:
  - An emulator that gives the serial port of ESQ on a TCP port. [Esquire](https://prevueguide.neocities.org/guides/Esquire) opens the data line on TCP port 5400. [FS-UAE](#test-with-fs-uae) can also open the serial port of the Amiga on a TCP port.
  - An Amiga that runs ESQ, connected to a serial port of this computer or to the UARTs of a Raspberry Pi. See [Connect to an Amiga](#connect-to-an-amiga).
- Guide data, if you send listings: an XMLTV file (for example from [zap2xml](https://github.com/jef/zap2xml)), or a [Channels DVR](https://getchannels.com/dvr-server/) server.

## Run it

1. Start the Prevue system, and wait until the grid shows.
2. Run `dotnet run --project PrevueCommander -- playbook.yaml`.

If you do not give a playbook, Prevue Commander uses `playbook.yaml` in the current directory. If the playbook does not exist, Prevue Commander writes an example playbook to that path and stops. Edit the playbook, and run the command again.

A playbook has a `configuration` section and a list of `commands`. [`playbook.yaml`](playbook.yaml) is the example playbook.

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
| `--validate` | | Makes all the commands and shows them, but does not connect. Use it to check a playbook. |
| `--output <file>` | | Writes the bytes of the data line to the file, but does not connect. See [Replay a feed file](#replay-a-feed-file). |
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
| `130` | You stopped Prevue Commander with Ctrl+C. |

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
| `characterSet` | `Latin1` | The characters of the listings, the local ads and the promo titles. `Latin1` sends letters with accents and other Latin-1 characters, for example `Fiancé`. `Ascii` removes the accents, for example `Fiance`, for older versions of ESQ. See [ESQ versions](#esq-versions). |

Prevue Commander connects to a line only when a command needs it. A playbook with only control line commands does not connect to the data line. If a playbook has control line commands, but `controlPort` and `controlSerialPort` are not set, Prevue Commander stops before it sends a command.

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

A Raspberry Pi can run Prevue Commander next to the Amiga. The UARTs of the Pi can send the two lines. This setup was not tested. The information comes from the Raspberry Pi documentation.

To build Prevue Commander for a Pi with a 64-bit Raspberry Pi OS, run `./build-linux-arm64.sh` on a computer with the .NET 11 SDK. Then copy `publish_linux-arm64/PrevueCommander` to the Pi. The Pi does not need .NET.

> [!WARNING]
> The GPIO pins of the Pi use 3.3 V logic. The serial port of the Amiga uses RS-232 levels, and the signal is inverted. Do not connect a GPIO pin directly to the Amiga. Use a MAX3232 level shifter board between the Pi and the Amiga.

A MAX3232 has two drivers, one for each line. Connect these pins:

| Line | Pi | MAX3232 | Amiga DB25 pin |
|---|---|---|---|
| Data line | TX of the first UART | T1IN, then T1OUT | 3 (RXD) |
| Control line | TX of the second UART | T2IN, then T2OUT | 5 (CTS) |
| Power | 3.3 V and GND | VCC and GND | |
| Ground | | GND | 7 (GND) |

Do not connect the RX pins of the Pi. ESQ does not send data back.

Set up the UARTs:

1. Run `sudo raspi-config`.
2. Select **Interface Options**, and then **Serial Port**.
3. Disable the login shell on the serial port.
4. Enable the serial port hardware.
5. Add the overlay lines for your model to `/boot/firmware/config.txt`, as the sections below show.
6. Restart the Pi.
7. Run `ls /dev/ttyAMA*` to find the names of the UARTs.

Use the PL011 UARTs (`/dev/ttyAMA*`). Do not use the mini UART (`/dev/ttyS0`). Its speed changes with the clock of the processor, and it cannot send 110 baud.

#### Pi 4 and Pi 5

These models have more than one PL011 UART on the GPIO header. Use one UART for each line. For example, on a Pi 4, add these lines to `config.txt`:

```
dtoverlay=disable-bt
dtoverlay=uart3
```

`disable-bt` moves UART0 from Bluetooth to GPIO14 (pin 8). `uart3` adds UART3 with TX on GPIO4 (pin 7). The Pi 5 has its UARTs in the RP1 chip, and its overlays are different. Read the Raspberry Pi documentation for your model.

```yaml
configuration:
  serialPort: /dev/ttyAMA0
  baudRate: 2400
  controlSerialPort: /dev/ttyAMA3
  controlBaudRate: 110
```

The names of the UARTs can be different on your Pi. Use the names that `ls /dev/ttyAMA*` shows.

#### Pi 3

The Pi 3 has only one PL011 UART and the mini UART, and both use GPIO14 for TX. Thus the GPIO header gives only one serial line. Use the PL011 UART for the control line, and a USB to RS-232 adapter for the data line. Some USB adapters cannot send 110 baud, for example the CP2102 starts at 300 baud. With this split, the adapter sends only 2400 baud.

Add this line to `config.txt`, and then run `sudo systemctl disable hciuart`:

```
dtoverlay=disable-bt
```

```yaml
configuration:
  serialPort: /dev/ttyUSB0
  baudRate: 2400
  controlSerialPort: /dev/ttyAMA0
  controlBaudRate: 110
```

Connect the TX of the PL011 UART (GPIO14, pin 8) to T1IN of the MAX3232, and T1OUT to pin 5 of the Amiga. Connect the USB adapter to pin 3, as in [Connect to an Amiga](#connect-to-an-amiga).

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
| `!!address` | `target` (`*`) | Selects the units that accept the next commands. ESQ accepts no other command before it. `*` selects all units. |
| `!!save` | | Tells ESQ to save the received data to its drive at the next box off. Without it, the data stays only in memory. |
| `!!boxOff` | | Ends the session. Send it last. |
| `!!reset` | `target` (`*`) | Reboots the Amiga after about 10 seconds. ESQ accepts a reset only directly after an address command, so this command sends both. |

### Configuration and time

| Command | Settings | Description |
|---|---|---|
| `!!configuration` | `configuration`: `timeZone` (`'6'`), `scrollSpeed` (`'3'`), `daylightSavingTime` (`true`), `clockOffsetMinutes` (`0`) | The status packet ('F'). ESQ needs it before a channel lineup or a change of the local ads. |
| `!!newLookConfiguration` | `configuration`: the fields of [`NewLookConfiguration`](Prevue.Commands/Model/NewLookConfiguration.cs) | The configuration record ('f'). ESQ saves it to `config.dat`. The defaults are the values of the original data files. `lrbnBannerControl: 'Y'` lets the control line move the grid. |
| `!!clock` | `date`, `useCurrentDate` (`false`) | Sets the clock of ESQ. Without `date`, it sends the local time of this computer. With `date`, for example `2020-11-01T16:00`, it sends that time. `useCurrentDate: true` sends the local time also when `date` is set. ESQ accepts the clock only when `clockCommandInFSetting` of the 'f' configuration is `'2'` (the default). |
| `!!configureDst` | `year` (the current year) | Sends the daylight saving time window of the year, from the time zone of this computer. |

ESQ adds (time zone - 6) hours to the clock and to the times of the grid. With the time zone `'6'`, ESQ shows the times as they are sent. Prevue Commander sends local times, so keep `'6'` unless you want another time on the screen.

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

ESQ shows the local ads only when `localTextAdSource` of the 'f' configuration is `'R'` or `'S'` (the default is `'S'`). An ad has a maximum of 6 lines. A line has 41 places. An alignment token at the start of a line uses one place, so that line has 40 characters. ESQ does not wrap words. When a line is full, ESQ continues the text on the next line, also in the middle of a word. ESQ drops the text after line 6.

`!!localAds` shows each ad as ESQ shows it, with a warning for text that ESQ breaks or drops. For example:

```text
Local ad 2:
  |ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcd |
  |efghij                                   |
  Warning: Line 1 is longer than 40 characters. ESQ continues it on line 2, also in the middle of a word.
```

An ad can have these tokens:

| Token | Description |
|---|---|
| `%COLOR%` and two colors | The background color and then the text color of the text that follows, for example `%COLOR%%BLACK%%CYAN%`. ESQ 7.3.13 does not support colored text. |
| `%TRANSPARENT%`, `%WHITE%`, `%BLACK%`, `%YELLOW%`, `%RED%`, `%CYAN%`, `%GRAY%`, `%BLUE%` | The colors 0 to 7 of the default palette. |
| `%LEFT%`, `%CENTER%`, `%RIGHT%` | The alignment of a line. A token that is not at the start of a line starts a new line. |
| `%%` | A `%` character. |

### Files and utilities

| Command | Settings | Description |
|---|---|---|
| `!!fileTransfer` | `source`, `target`, `address` (`*`) | Sends a local file to the drive of ESQ. The target must start with a volume of 4 characters, for example `DF0:Logos/MYAD.IFF`. ESQ accepts one file for each address command, so this command first sends a box off command and an address command. |
| `!!utility` | `action` | Tells ESQ to reload files or to show a screen. The actions are `ReloadGraphicAds`, `ReloadLogoList`, `RebuildBrushList`, `ReloadBrushes`, `ReloadGradient`, `ReloadBanner`, `ReloadDefaults`, `ReloadQTable`, `ReloadSourceConfiguration`, `ToggleMenu`, `ShowDiagnostics` and `ShowVersion`. |
| `!!shell` | `commandLine` | Runs an AmigaDOS command line on the Amiga, for example `Delete DF0:OLD.IFF`. The command line has a maximum of 78 characters. ESQ shows no output. |

ESQ first writes a received file to `RAM:`, in the same directories as the target. Then it copies the file to the target. If a directory does not exist in `RAM:` or on the target volume, the file does not arrive. Thus, for a target in a directory, `!!fileTransfer` first sends `MakeDir` commands for the directories in `RAM:` and on the target volume.

`ReloadGraphicAds` reloads only `gfx/g_ads.data`. `ReloadLogoList` also reads `LOGO.LST` again and starts at its first line.

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
- `keyBackground`: `true` makes the background the genlock key (color 0). The genlock video then shows through it.
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

### Control line

The control line commands go to `controlPort` or `controlSerialPort`. ESQ reads them only when its display is not busy. Put a `!!pause` between two promos, so that each promo stays on the screen.

| Command | Settings | Description |
|---|---|---|
| `!!promo` | `right`, `left`: a box with `title`, `channels` (`*`) and `brush`. `first` (`Right`). `style` (`Box`) | Shows a promo in the top half of the screen. With `style: Text`, ESQ draws only the text, over what the top half shows at that time. |
| `!!moveGrid` | `position` (`A0`), `seconds` (`1`), `channels` | Moves the top of the grid up or down. `position` is two hex digits: a larger value is lower on the screen. `seconds` is the time of the move, from 0 to 9.9. This needs `lrbnBannerControl: 'Y'` in `!!newLookConfiguration`. Without it, ESQ shows the loaded logo. `!!clearTopHalf` moves the grid back. |
| `!!clearTopHalf` | | Removes the promo or the logo, and moves the grid back. The top half shows the genlock video. |
| `!!showLogo` | | Shows the loaded logo of `LOGO.LST`. ESQ then loads the next logo. |
| `!!controlPacket` | `type` (1 to 22), `body` | Sends one packet, for the types that have no command. In a double-quoted YAML text, `\x12` is the separator of the two boxes. |
| `!!pause` | `seconds` (`1`) | Waits before the next command. It does not send data. |

A promo has a box on the right, a box on the left, or both boxes. ESQ finds the program in its listings, so send the listings first. A box has these settings:

- `title` is the title to find. A part of the title is sufficient, and upper case and lower case are the same. Put the title in quotes to find only the exact title. Prevue Commander converts the title with `characterSet`, as it converts the listings. Thus `Fiancé` finds the program in both modes.
- `channels` is a pattern for the channels to search, for example `KTIV*`. The ESQ code calls it a pattern for the call letters. The tests used only `*`. ESQ matches channel logos by source name, so the pattern possibly also matches source names.
- `brush` is the ID of the background in `BRUSH.INI` on the drive, for example `AT` (orange "around town") or `DT` (blue TV Guide). Without it, the box keeps its background.

The box shows the call letters, the title, the next time of the program and the channel number. The box shows the title as the promo gives it, not as the listings give it. For example, `title: family or fiancé` shows "family or fiancé". ESQ shows only one box. It tries the `first` box, and then the other box. If ESQ does not find a title, it shows the loaded logo.

#### Packet types

These are the types of the control line, from the code of ESQ. Use `!!controlPacket` to send a type that has no command. A packet has a body of a maximum of 196 characters. A machine with a RAVESC selection code ignores the types with an asterisk (*). ESQ ignores the types 6, 8, 9, 10, 14, 18, 19 and 21.

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

The sub-commands of type 1. The results come from tests with Esquire.

| Body | What ESQ does |
|---|---|
| `1<channels>[\x12<channels>]` | Shows a promo in a box (`!!promo`). If the program plays now, the box shows "Now showing" and the next time. |
| `8<channels>[\x12<channels>]` | Shows the text of the promo without the box background, over what the top half shows (`!!promo` with `style: Text`). |
| `3` | Removes the promo or the logo (`!!clearTopHalf`). |
| `D` | Shows the loaded logo (`!!showLogo`). |
| `4<channels>` or `6<channels>` | Finds a channel by its call letters. If ESQ finds the channel, it clears the top half. If not, it shows the logo. |
| `5<hex><hex><digit><digit>[<channels>]` | Moves the top of the grid up or down. The two hex digits set the position: `A0` is lower than `80`. The two digits set the speed: seconds, and then tenths of a second. This needs `lrbnBannerControl: 'Y'` in `!!newLookConfiguration`. Without it, ESQ shows the logo. `!!clearTopHalf` moves the grid back. `!!moveGrid` sends this sub-command. |
| `7<digit><digit>` | Selects a range of channels after a search with `1` or `8`. In the test, it removed the promo and showed nothing. |
| `F<op><text>` or `X<op><text>` | Sends a command to the text display: `C` for a "now showing" line, `J` for a status line, `T` for a highlight frame. In the test, nothing showed. The text display possibly needs a state that the test did not make. |
| `W0` | Shows the current weather. Without weather data, it shows "No Current Weather Data Available". |
| `W3` | Shows the weather forecast. Without weather data, it shows "No Forecast Weather Data Available". |
| `W` and another character | Shows the logo. |

## Build and test

```sh
dotnet build
dotnet test
```

GitHub Actions runs these commands for each pull request and each push to `main`. The workflow is in `.github/workflows/build.yml`.

`build-osx.sh` makes `PrevueCommander-osx`, a universal macOS program for Apple silicon and Intel. `build-win.sh` makes a Windows x64 program in `publish_win-x64`. `build-linux-arm64.sh` makes a Linux arm64 program in `publish_linux-arm64`, for example for a Raspberry Pi. These programs do not need .NET on the computer. Run them with the playbook as the argument, for example `./PrevueCommander-osx playbook.yaml`.

Do not trim the output: YamlDotNet and the XMLTV reader need the metadata that trimming removes.

## Test with Esquire

Esquire can show what ESQ does with each command. Run a second Esquire on a copy of its drive, with its own serial port, so that you do not change a running Esquire. Run this command in the Esquire directory:

```sh
Esquire build/target/ESQ --listing build/target/ESQ.lst --drive /tmp/prevue-drive --serial-port 5401 \
    --serial-speed 4 --prevue-feed-trace /tmp/feed.log --screenshot /tmp/prevue.png --seconds 120 \
    --screenshot-every 15
```

`/tmp/prevue-drive` is the copy of the drive. Run Prevue Commander with `--port 5401` when the grid shows. `/tmp/feed.log` shows each command that ESQ reads. A change of `_DATACErrs` shows a checksum error. Esquire saves a screenshot to `/tmp/prevue.png` each 15 seconds, and stops after 120 seconds.

To test the control line, add `--prevue-ctrl-port 5402` and run Prevue Commander with `--control-port 5402`. Add `--watch _SCRIPT_CtrlCmdCount --watch _SCRIPT_CtrlCmdChecksumErrorCount` to show the packets that ESQ reads, and the packets with a bad checksum.

### Replay a feed file

`--output` writes the bytes of the data line to a file. The file has no pauses, and Prevue Commander does not connect. Esquire can replay the file, so you can test a playbook again with the same bytes:

```sh
dotnet run --project PrevueCommander -- --output /tmp/feed.bin playbook.yaml
```

Then run this command in the Esquire directory:

```sh
Esquire build/target/ESQ --listing build/target/ESQ.lst --drive /tmp/prevue-drive --serial-file /tmp/feed.bin \
    --serial-start 8 --virtual-time --prevue-feed-trace /tmp/feed.log --screenshot /tmp/prevue.png --seconds 120
```

Esquire sends the file at the speed of the line, 8 seconds after the start of ESQ. `--virtual-time` makes the run the same each time.

### Decode a feed file

`decode` shows the commands of a feed file, their fields and the result of each checksum. Use it to compare the feed of Prevue Commander with a feed from another source.

```sh
dotnet run --project PrevueCommander -- decode /tmp/feed.bin
```

The file can come from `--output`, or from another capture of the data line. `--control` reads a file of the control line, for example from `--control-output`. `--verbose` also shows the bytes of each command.

This is the start of the output for the example playbook:

```text
000000  'A' address (6 bytes)
        target: "*"
000006  'F' status packet (25 bytes)
        scroll speed: "3"
        time zone: "6"
        daylight saving time: "Y"
        clock offset minutes: 0
```

The first number is the position of the command in the file, in hexadecimal. A command with a bad checksum shows `BAD CHECKSUM`. Bytes before the first `55 AA` show as `junk`.

## Test with FS-UAE

[FS-UAE](https://fs-uae.net/) emulates all the Amiga hardware, so it shows the real behavior of ESQ. Its serial port can be a TCP port. For example, this line of the FS-UAE configuration opens the serial port of the Amiga on TCP port 1234:

```
serial_port = tcp://127.0.0.1:1234
```

1. Make a copy of the hard drive directory of Prevue. The commands change files on the drive, for example `config.dat` and `LOGO.LST`.
2. Set `hard_drive_1` in the FS-UAE configuration to the copy.
3. Start FS-UAE, and wait until the grid shows.
4. Set `port: 1234` and `baudRate: 2400` in the playbook, and run Prevue Commander.

FS-UAE writes the files of the drive to the host directory, so you can examine them after a file transfer.

The control line was not tested with FS-UAE. ESQ reads the control line on the CTS pin, and the TCP serial port of FS-UAE possibly does not control that pin. Use Esquire to test the control line.

## Project layout

| Project | Description |
|---|---|
| `Prevue.Core` | Broadcast days, slots, group codes and text conversion. |
| `Prevue.Graphics` | The PNG reader, the color reduction and the IFF writer for the pictures of ESQ. |
| `Prevue.Commands` | The commands of the data line and of the control line (`Control`), and the listings builder. Each class tells the format that ESQ reads. |
| `PrevueCommander.XmlTv` | The XMLTV reader. |
| `PrevueCommander.ChannelsDVR` | The Channels DVR client. |
| `PrevueCommander` | The program and the playbook commands. |
| `PrevueCommander.Tests` | The tests. |
