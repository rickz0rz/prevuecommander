# Test with an emulator

## Test with Esquire

Esquire can show what ESQ does with each command. Run a second Esquire on a copy of its drive, with its own serial port, so that you do not change a running Esquire. Run this command in the Esquire directory:

```sh
Esquire build/target/ESQ --listing build/target/ESQ.lst --drive /tmp/prevue-drive --serial-port 5401 \
    --serial-speed 4 --prevue-feed-trace /tmp/feed.log --screenshot /tmp/prevue.png --seconds 120 \
    --screenshot-every 15
```

`/tmp/prevue-drive` is the copy of the drive. Run Prevue Commander with `--port 5401` when the grid shows. `/tmp/feed.log` shows each command that ESQ reads. A change of `_DATACErrs`, the counter of ESQ for the data line errors, shows a checksum error. Esquire saves a screenshot to `/tmp/prevue.png` each 15 seconds, and stops after 120 seconds.

To test the control line, add `--prevue-ctrl-port 5402` and run Prevue Commander with `--control-port 5402`. Add `--watch _SCRIPT_CtrlCmdCount --watch _SCRIPT_CtrlCmdChecksumErrorCount` to show the packets that ESQ reads, and the packets with a bad checksum. `--watch` shows the changes of a variable of ESQ. The names come from `ESQ.lst`, the listing of the ESQ program in the Esquire directory.

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

The first number is the position of the command in the file, in hexadecimal. Then come the letter and the name of the command. A command with a bad checksum shows `BAD CHECKSUM`. Each data line command starts with the bytes `55 AA`. Bytes before the first `55 AA` show as `junk`.

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
