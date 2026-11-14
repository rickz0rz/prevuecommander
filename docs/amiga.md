# Connect to an Amiga

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

A Raspberry Pi can run Prevue Commander next to the Amiga. See [Connect from a Raspberry Pi](pi.md).
