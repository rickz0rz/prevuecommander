# Automatic promos

With an `autoPromo` section, Prevue Commander stays connected after the commands and sends a promo at each interval. It chooses the program from the listings that it sent, so ESQ always finds the program. The promos need the control line. The section is beside `configuration` and `commands`:

```yaml
autoPromo:
  intervalSeconds: 120
  movies: true
  brush: AT
```

| Setting | Default | Description |
|---|---|---|
| `intervalSeconds` | `120` | The seconds between two promos, 5 or more. The first promo comes directly after the commands. |
| `withinHours` | `3` | The hours after the current half hour for the start of the program, 0.5 to 24. |
| `includeNow` | `false` | `true` also chooses a program that starts in the current half hour. |
| `movies` | | `true` chooses only movies, `false` no movies. Without a value, all programs. |
| `premium` | | `true` chooses only premium channels, `false` no premium channels. Without a value, all channels. |
| `channels` | | Call letters (with `*` for any text) or channel numbers, for example `[KTIV*, '4']`. |
| `titles` | | Parts of titles, for example `[Seinfeld, News]`. Upper case and lower case are the same. |
| `order` | `Soonest` | `Soonest` chooses the next program. `Random` chooses by chance. |
| `repeat` | `10` | The number of the last promos whose titles do not show again, 0 to 100. If all the programs that fit were in these promos, the title of the oldest promo shows again. |
| `brush` | | The background of the box, for example `AT`. See [Control line](commands.md#control-line). |
| `side` | `Right` | The box of the promo: `Right` or `Left`. |
| `style` | `Box` | `Box` shows the background of the box. `Text` draws only the text. |

The promo of a movie finds the title in quotation marks, so that ESQ finds only that title. The promo uses the call letters of the channel as its channel pattern. With the [watch mode](watch-mode.md), the promos use the listings of the last read of the guide. Without the watch mode, they use the listings of the commands. If no program fits, Prevue Commander shows a message and tries again at the next interval. Press Ctrl+C to stop.
