# Control line

## Keep the top half clear

ESQ shows the logos of `LOGO.LST` in turn, with a timer. It counts the seconds, and after 180 seconds it shows the next logo. The logo stays until the next logo, a promo, an ad or a clear.

Each `!!clearTopHalf` removes what the top half shows, and the next logo comes about 3 minutes later. Thus, to keep the genlock video in the top half, send `!!clearTopHalf` again at intervals of less than 3 minutes. The [web server](web-server.md) does this for you with `POST /prevue/ctrl/hold`, and the control page has a button for it. Without the web server, use a playbook. For example, this playbook keeps the top half clear for 10 minutes:

```yaml
configuration:
  controlPort: 5402
commands:
- !!clearTopHalf {}
- !!pause
  seconds: 150
- !!clearTopHalf {}
- !!pause
  seconds: 150
- !!clearTopHalf {}
- !!pause
  seconds: 150
- !!clearTopHalf {}
- !!pause
  seconds: 150
- !!clearTopHalf {}
```

When you stop the clears, the logo rotation starts again. A test with Esquire showed this: with a clear each 150 seconds, no logo showed in 8 minutes. Without the clears, the first logo showed about 3 minutes after the start of ESQ. When the top half has nothing to show, a clear does not change the screen.

The clear does not stop a promo or an ad break that comes later. Do not send `!!promo` or a type 12 packet while you keep the top half clear.

To stop the logos for a long time, `!!logoList` can send a `LOGO.LST` without lines. ESQ then has no logos to show. But this changes the file on the drive, and you must send all the lines again to start the logos. Tests with Esquire showed that an empty `LOGO.LST` stops the logos. The tests did not send the empty file with `!!logoList`.

## Packet types

These are the types of the control line, from the code of ESQ. Use `!!controlPacket` to send a type that has no command. A packet has a body of a maximum of 196 characters. ESQ ignores the types 6, 8, 9, 10, 14, 18, 19 and 21. A unit with the selection code `RAVESC` in its configuration ignores the types with an asterisk (*). The purpose of that mode is not known.

Some types have results that are not known. The satellite feed sent these types, and a playbook does not usually need them.

| Type | Body | What ESQ does |
|---|---|---|
| 1 | a sub-command, see below | Controls the top half of the screen. |
| 2* | two brush IDs | Sets the backgrounds of the two promo boxes. `00` keeps a background, and `11` removes it. `!!promo` sends this type. |
| 3* | any | Nothing. ESQ counts the packet only. |
| 4* | `L` or `R` | Sets the box that ESQ tries first. Another character changes the order. `!!promo` sends this type. |
| 5* | two characters | Sets the channel codes of the two boxes. ESQ uses them in the title search. |
| 7* | | Stops the read mode (see type 16). |
| 11* | 11 digits | Sets the clock and the battery clock of the Amiga. The `CLOCKCMD` setting of the configuration file of ESQ must be `1`. Do not use it for tests: the battery clock keeps the time after a restart. |
| 12 | any, or `1<channels>` or `3` | Shows the next local ad, if ESQ has local ads. See [Show a local ad](local-ads.md#show-a-local-ad). Without local ads, it is the same as type 1, but only when no local avail filter and no graphic ads are active. |
| 13 | | ESQ ignores the next byte. |
| 15 | a number | Sets the level of a copper effect to 63 minus the number. The copper is the display processor of the Amiga, which can change colors on each line of the screen. The effect on the screen is not known. |
| 16* | | Starts the read mode. ESQ then stops the animation of the grid. |
| 17* | `<title>` or `<title>\x12<title>` | Sets the titles that a promo finds. `!!promo` sends this type. |
| 20* | one character | Keeps a promo type for the next type 1 or 12. If the type is in the file `PromoId.Dat` of the drive, ESQ shows the logo in place of the promo. |
| 22* | `9<tokens>` or `8` | `9` starts a local avail filter, and `8` stops it. The filter controls what ESQ shows in the top half for the next type 12 packets. The details of the tokens are not known. With the VIN setting of the diagnostics screen, `0` to `3` control the CTRL output. |

The sub-commands of type 1. The results come from tests with Esquire.

| Body | What ESQ does |
|---|---|
| `1<channels>[\x12<channels>]` | Shows a promo in a box (`!!promo`). If the program plays now, the box shows "Now showing" and the next time. |
| `8<channels>[\x12<channels>]` | Shows the text of the promo without the box background, over what the top half shows (`!!promo` with `style: Text`). |
| `3` | Removes the promo, the logo or the local ad (`!!clearTopHalf`). |
| `D` | Shows the loaded logo (`!!showLogo`). |
| `4<channels>` or `6<channels>` | Finds a channel by its call letters. If ESQ finds the channel, it clears the top half. If not, it shows the logo. |
| `5<hex><hex><digit><digit>[<channels>]` | Moves the top of the grid up or down. The two hex digits set the position: `A0` is lower than `80`. The two digits set the speed: seconds, and then tenths of a second. This needs `lrbnBannerControl: 'Y'` in `!!newLookConfiguration`. Without it, ESQ shows the logo. `!!clearTopHalf` moves the grid back. `!!moveGrid` sends this sub-command. |
| `7<digit><digit>` | Selects a range of channels after a search with `1` or `8`. In the test, it removed the promo and showed nothing. |
| `F<op><text>` or `X<op><text>` | Sends a command to the text display of the top half. `<op>` is one letter: `C` for a "now showing" line, `J` for a status line, `T` for a highlight frame. In the test, nothing showed. The text display possibly needs a state that the test did not make. |
| `W0` | Shows the current weather of `!!weatherStatus` and `!!weatherText`. Without weather data, it shows "No Current Weather Data Available". `!!showWeather` sends it. |
| `W3` | Shows the 3-day forecast of `!!weatherForecast`. Without a forecast, it shows "No Forecast Weather Data Available". `!!showWeather` with `forecast: true` sends it. |
| `W` and another character | Shows the logo. |
