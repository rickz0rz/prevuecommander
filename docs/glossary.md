# Glossary

| Word | Meaning |
|---|---|
| ESQ | The Prevue Guide program that runs on the Amiga. |
| Data line | The serial line that sends the guide data to ESQ. The satellite feed used 2400 baud. |
| Control line | A second, slow serial line (110 baud). It tells ESQ what to show in the top half of the screen. |
| Data line command | One record on the data line. It starts with the bytes `55 AA`, then a letter that names the command, then its data and a checksum. For example, 'A' is the address command and 'L' is a local ad. The documents write the letter in quotes, for example 'F'. The case of the letter is important: 'F' and 'f' are different commands. |
| Control packet | One record on the control line. It starts with a type number from 1 to 22. See [Packet types](control-line.md#packet-types). |
| Unit | One Prevue machine. A cable company could have many units on one satellite feed. Each unit has an address. |
| Session | A group of data line commands for the selected units: an address command, the commands, and a box off command. See [Commands](commands.md). |
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
