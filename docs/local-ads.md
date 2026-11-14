# Local ads

## Show a local ad

ESQ has no timer for the local ads. ESQ shows local ads only when the control line sends a type 12 packet. On a real system, the satellite feed sent this packet at the times of the commercial breaks. Prevue Commander has no command for this packet, so send it with `!!controlPacket`:

```yaml
- !!controlPacket
  type: 12
```

One type 12 packet starts an ad break of about 30 seconds:

1. ESQ shows the next ad at once.
2. After 16 seconds, ESQ shows the ad after it.
3. After 16 more seconds, the break ends. The second ad stays on the screen.

ESQ shows the ads in the order of their numbers. After the last ad, the first ad comes again. During a break, ESQ ignores another type 12 packet. Thus send type 12 at intervals of 35 seconds or more. A test with Esquire showed this behavior. The code of ESQ sets the 16 seconds. A local avail filter can change this time.

An ad stays on the screen until one of these events:

- `!!clearTopHalf` removes it at once.
- The next ad break shows other ads.
- A promo replaces it.
- The logo rotation shows the next logo, about 3 minutes later. See [Keep the top half clear](control-line.md#keep-the-top-half-clear).

The packet shows a local ad only when all these conditions are true:

- ESQ has local ads. Send `!!localAds` first.
- `localTextAdSource` is `'R'` or `'S'`. With `'L'`, the ads that an operator typed also show.
- The graphic ad mode of the operator menu is `N`. This is the default. With another mode, ESQ shows graphic ads in place of the local ads. See [Graphic ads of ESQ](#graphic-ads-of-esq).
- No local avail filter is active. A type 22 packet starts this filter. See [Packet types](control-line.md#packet-types).

Otherwise, type 12 does the same as type 1. The key `T` of the operator menu also shows a local ad. But ESQ reads the keys of the operator on a different pin of the serial port. Esquire does not send these keys.

## Graphic ads of ESQ

ESQ can also show graphic ads from the file `gfx/g_ads.data` of its drive. These graphic ads are not the pictures of `!!graphicAd`. A picture of `!!graphicAd` is a logo, and the logo rotation shows it.

The graphic ads of `gfx/g_ads.data` have no timer, as the local ads. ESQ shows the next graphic ad only at these times:

- At a type 12 packet, if ESQ has graphic ads and the graphic ad mode is not `N`.
- When an operator presses the key `A` in the operator menu.

Only the operator menu sets the graphic ad mode. Prevue Commander cannot set it. The drive of Esquire does not have `gfx/g_ads.data`, and the format of this file is not known. Thus these rules come from the code of ESQ only. They were not tested.
