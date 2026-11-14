# The playbook

## Edit a playbook in an editor

[`playbook.schema.json`](../playbook.schema.json) is a JSON schema of the playbook. A YAML editor can use it to complete the settings and to find unknown settings. The first line of the example playbook names the schema:

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

## What happens in a run

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
| `watchMinutes` | `0` | The minutes between two reads of the guide in the [watch mode](watch-mode.md). `0` stops after the commands. |
| `weatherMinutes` | `0` | The minutes between two reads of the weather of `!!weatherGov`. See [Weather from weather.gov](weather.md#weather-from-weathergov). `0` stops after the commands. |
| `servePort` | `0` | The TCP port of the [web server](web-server.md). `0` starts no server. |
| `serveHost` | `127.0.0.1` | The address of the web server. `127.0.0.1` accepts only this computer. `0.0.0.0` accepts all the computers of the network. |
| `characterSet` | `Latin1` | The characters of the listings, the local ads and the promo titles. `Latin1` sends letters with accents and other Latin-1 characters, for example `Fiancé`. `Ascii` removes the accents, for example `Fiance`, for older versions of ESQ. See [ESQ versions](../README.md#esq-versions). |

Prevue Commander connects to a line only when a command needs it. A playbook with only control line commands does not connect to the data line. If a playbook has control line commands, but `controlPort` and `controlSerialPort` are not set, Prevue Commander stops before it sends a command.
