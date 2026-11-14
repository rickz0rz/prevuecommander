# Commands

Each command has a YAML tag, for example `!!localAds`. Write a command without settings with `{}`, for example `!!save {}`. A setting in parentheses in the tables below shows its default value.

A session on the data line has this order:

1. `!!address` selects the units.
2. `!!configuration` sends the status packet. ESQ needs it before a channel lineup or local ads.
3. The other data line commands, for example the listings and the local ads.
4. `!!save`, if ESQ must keep the data after a restart.
5. `!!boxOff` ends the session.

The control line commands do not need a session. Send them after the listings, because a promo finds its program in the listings.

## Session

| Command | Settings | Description |
|---|---|---|
| `!!address` | `target` (`*`) | Selects the units (Prevue machines) that accept the next commands. ESQ accepts no other command before it. `*` selects all units. Each unit has an address, so a cable company could send different data to different units. |
| `!!save` | | Tells ESQ to save the received data to its drive at the next box off. Without it, the data stays only in memory. |
| `!!boxOff` | | Ends the session. Send it last. |
| `!!reset` | `target` (`*`) | Reboots the Amiga after about 10 seconds. ESQ accepts a reset only directly after an address command, so this command sends both. |

## Configuration and time

| Command | Settings | Description |
|---|---|---|
| `!!configuration` | `configuration`: see the table below | Sends the status packet ('F'). ESQ needs it before a channel lineup or a change of the local ads. |
| `!!newLookConfiguration` | `configuration`: see the table below | Sends the configuration record ('f'). ESQ saves it to `config.dat`. |
| `!!clock` | `date`, `useCurrentDate` (`false`) | Sets the clock of ESQ. Without `date`, it sends the local time of this computer. With `date`, for example `2020-11-01T16:00`, it sends that time. `useCurrentDate: true` sends the local time also when `date` is set. ESQ accepts the clock only when `clockCommandInFSetting` of `!!newLookConfiguration` is `'2'` (the default). See [The clock and daylight saving time](clock.md#the-clock-and-daylight-saving-time). |
| `!!configureDst` | `year` (the current year) | Sends the daylight saving time window of the year, from the time zone of this computer. ESQ writes the window to `dst.dat` only after `!!save`. See [The clock and daylight saving time](clock.md#the-clock-and-daylight-saving-time). |

The settings of `!!configuration` and `!!newLookConfiguration` are in [ESQ settings](esq-settings.md).

### The clock and daylight saving time

`!!clock` also sends a daylight saving time flag. ESQ compares it with its DST window, and can move the clock by one hour. Send `!!configureDst` in the same playbook. See [The clock and daylight saving time](clock.md).

## Listings

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

## Channel logos

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

## Local ads

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

ESQ shows a local ad only at an ad break of the control line. See [Local ads](local-ads.md).

## Weather

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
| `!!weatherForecast` | `minutes` (`60`), `label` (`*`), `days` | Sends the 3-day forecast. See [3-day forecast](weather.md#3-day-forecast). |
| `!!weatherGov` | `zip`, or `latitude` and `longitude`, and other settings | Gets the weather of a US location from weather.gov, and sends the status, the current weather and the 3-day forecast. See [Weather from weather.gov](weather.md#weather-from-weathergov). |

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

[Weather](weather.md) tells about the 3-day forecast and the weather from weather.gov.

## Files and utilities

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

## Pictures

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

To send an IFF picture that you made yourself, see [Send your own IFF picture](pictures.md).

## Control line

The control line commands go to `controlPort` or `controlSerialPort`. ESQ reads them only when its display is not busy. Put a `!!pause` between two promos, so that each promo stays on the screen.

| Command | Settings | Description |
|---|---|---|
| `!!promo` | `right`, `left`: a box with `title`, `channels` (`*`) and `brush`. `first` (`Right`). `style` (`Box`) | Shows a promo in the top half of the screen. With `style: Text`, ESQ draws only the text, over what the top half shows at that time. |
| `!!moveGrid` | `position` (`A0`), `seconds` (`1`), `channels` | Moves the top of the grid up or down. `position` is two hex digits: a larger value is lower on the screen. `seconds` is the time of the move, from 0 to 9.9. This needs `lrbnBannerControl: 'Y'` in `!!newLookConfiguration`. Without it, ESQ shows the loaded logo. `!!clearTopHalf` moves the grid back. |
| `!!clearTopHalf` | | Removes the promo, the logo or the local ad, and moves the grid back. The top half shows the genlock video. The next logo of the rotation comes about 3 minutes later. See [Keep the top half clear](control-line.md#keep-the-top-half-clear). |
| `!!showLogo` | | Shows the loaded logo of `LOGO.LST`. ESQ then loads the next logo. |
| `!!showWeather` | | Sends `W0`: shows the current weather. See [Weather](#weather). |
| `!!controlPacket` | `type` (1 to 22), `body` | Sends one packet, for the types that have no command. See [Packet types](control-line.md#packet-types). In a double-quoted YAML text, `\x12` is the byte that separates the text of the two boxes. |
| `!!pause` | `seconds` (`1`) | Waits before the next command. It does not send data. |

A promo has a box on the right, a box on the left, or both boxes. ESQ finds the program in its listings, so send the listings first. A box has these settings:

- `title` is the title to find. A part of the title is sufficient, and upper case and lower case are the same. Put the title in quotes to find only the exact title. Prevue Commander converts the title with `characterSet`, as it converts the listings. Thus `Fiancé` finds the program in both modes.
- `channels` is a pattern for the channels to search, for example `KTIV*`. The ESQ code calls it a pattern for the call letters. The tests used only `*`. ESQ matches channel logos by source name, so the pattern possibly also matches source names.
- `brush` is the ID of the background in `BRUSH.INI` on the drive, for example `AT` (orange "around town") or `DT` (blue TV Guide). Without it, the box keeps its background.

The box shows the call letters, the title, the next time of the program and the channel number. The box shows the title as the promo gives it, not as the listings give it. For example, `title: family or fiancé` shows "family or fiancé". ESQ shows only one box. It tries the `first` box, and then the other box. If ESQ does not find a title, it shows the loaded logo.

[Control line](control-line.md) tells how to keep the top half clear, and has all the packet types.
