# Send your own IFF picture

`!!fileTransfer` sends an IFF picture that you made yourself, for example to `DF0:Logos/Ads/MYAD`. ESQ loads a picture for the top half of the screen only in one of these formats:

An IFF picture keeps its colors in bit planes: 5 planes give 32 colors, and 4 planes give 16 colors. CAMG is the part of the file that names the display mode of the Amiga.

| Format | Width | Planes | Colors | Aspect | CAMG |
|---|---|---|---|---|---|
| Low resolution | 320 | a maximum of 5 | a maximum of 32 | 44:26 | `0x11004` |
| High resolution | 640 | a maximum of 4 | a maximum of 16 | 22:26 | `0x19004` |

- ESQ shows the top 220 rows. Use 240 rows, as the logos of the drive do.
- A test with Esquire showed a picture of 320 by 240 pixels with 5 planes and with 4 planes.
- Color 0 is the background, the border around the picture, and the genlock key.
- A picture with too many planes stops ESQ with "Report Error Code ER009", the file name, the width and the colors. A test with Esquire showed this for a picture with 320 by 240 pixels and 64 colors (6 planes). ESQ then shows nothing in the top half until an operator presses ESC two times.

A file transfer of a large picture takes a long time. The time in seconds is about the bytes of the file × 10 / the baud rate. Thus a file of 30,000 bytes takes about 2 minutes at 2400 baud. To make a picture smaller:

- Do not dither. ESQ uses ByteRun1 compression, which packs only runs of the same byte.
- Use large areas of one color. Make the color of the largest area color 0.
- Use the minimum number of colors. Each plane less makes the picture about 20% smaller.
- Try different orders of the palette. The order changes the runs in each plane.

For example, a picture with text on a green background was 30,356 bytes with 32 colors. With 10 colors that were selected by hand, no dither, and a better order of the palette, it was 15,666 bytes.
