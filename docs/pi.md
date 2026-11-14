# Connect from a Raspberry Pi

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

## Pi 4 and Pi 5

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

## Pi 3

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

Connect the TX of the PL011 UART (GPIO14, pin 8) to T1IN of the MAX3232, and T1OUT to pin 5 of the Amiga. Connect the USB adapter to pin 3, as in [Connect to an Amiga](../README.md#connect-to-an-amiga).
