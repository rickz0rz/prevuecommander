# ESQ settings

These are the settings of the commands `!!configuration` and `!!newLookConfiguration`. See [Configuration and time](commands.md#configuration-and-time).

## The status packet (`!!configuration`)

The settings of `!!configuration`:

| Setting | Default | Description |
|---|---|---|
| `timeZone` | `'6'` | A character from `'1'` to `'9'` or from `'A'` to `'H'`. ESQ adds (time zone - 6) hours to the clock and to the times of the grid. For example, `'7'` shows the times one hour later, and `'5'` one hour earlier. Prevue Commander sends local times, so keep `'6'` unless you want another time on the screen. |
| `scrollSpeed` | `'3'` | The speed of the grid, from `'1'` to `'8'`. ESQ uses `'4'` for another value. `'3'` is the value of the original data files. |
| `daylightSavingTime` | `true` | `true` if the area of the units uses daylight saving time. Send the dates of the change with `!!configureDst`. |
| `clockOffsetMinutes` | `0` | Minutes, from 0 to 59, that ESQ adds to the clock that it shows. |

The other values of the status packet are the values of the original data files. A playbook cannot change them.

## The configuration record (`!!newLookConfiguration`)

The settings of `!!newLookConfiguration`. Each setting is one character, except the numbers. The defaults are the values of the original data files. The names come from the diagnostics screen of ESQ. For many settings, what ESQ does is not known. Keep the default of these settings.

| Setting | Default | What ESQ does |
|---|---|---|
| `rollAndHoldTime` | `'2'` | The refresh interval in minutes. |
| `gridRollAndHold` | `'N'` | `'Y'` stops the grid for a short time after each 3 listings. |
| `militaryTime` | `'N'` | `'Y'` shows the clock in 24-hour format. |
| `cleanUpUnusedLogos` | `'Y'` | `'Y'` scans the directory `DH2:logos` before ESQ reads `LOGO.LST` again. Without that directory, ESQ stops. Use `'N'` before `reload: true` or `ReloadLogoList`. See [Files and utilities](commands.md#files-and-utilities). |
| `localTextAdSource` | `'S'` | The source of the local ads: `'N'`, `'L'`, `'R'` or `'S'`. See [Local ads](commands.md#local-ads). |
| `pcDiskSupport` | `'N'` | `'Y'` uses the volume `PC1:` for the graphics. |
| `lrbnBannerControl` | `'N'` | `'Y'` lets the control line move the grid (`!!moveGrid`). |
| `lrbnModeSelector` | `'N'` | `'L'`, `'R'`, `'B'` or `'N'`. How the control line moves the grid in one of its modes. The effect on the screen is not known. |
| `msnMode` | `'N'` | `'M'` changes the promo, logo and text commands of the control line to a clear of the top half. |
| `clockCommandInFSetting` | `'2'` | `'2'` tells ESQ to accept the clock of `!!clock`. With `'1'`, ESQ keeps its own clock. |
| `displayLength` | `'C'` | `'C'` tells ESQ to use the next byte of the record for the banner. |
| `numberOfColorPalettes` | `'8'` | ESQ always uses 8. |
| `displayMode` | `'G'` | ESQ keeps and saves this setting, but does not use it. |
| `primeTimeStartingSlot` | `29` | A number from 0 to 99. Not known. |
| `primeTimeLookAhead` | `6` | A number from 0 to 99. Not known. |
| `primeTimePromotionTimeSlotStart` | `23` | A number from 0 to 99. Not known. |
| `primeTimePromotionTimeSlotEnd` | `36` | A number from 0 to 99. Not known. |
| `lengthOfLookAheadSportsSummary` | `6` | A number from 0 to 99. Not known. |
| `minutesToDisplayPPVPostStart` | `15` | A number from 0 to 999. Not known. |
| `summaryCycleFrequency` | `'1'` | `'1'` to `'9'`. Not known. |
| `primeTimeSummary`, `sportsSummary` | `'0'`, `'1'` | A digit. Not known. |
| `laserDiscInsertionType` | `'L'` | `'L'`, `'S'` or `'V'`. Not known. |
| `gridSourceChannelOrder` (`'C'`), `mrReviewInFSetting` (`'0'`), `sbsFrequency` (`'1'`), `advueInsertion` (`'N'`), `pieFormat` (`'A'`), `language` (`'E'`), `sumBySourceRollAndHold` (`'N'`), `movieRecapRollAndHold` (`'N'`), `primeTimeSummaryRollAndHold` (`'N'`), `sportsSummaryRollAndHold` (`'N'`), `videoVueInsertion` (`'N'`), `cycleSummaryInfo` (`'Y'`), `gridSynopsis` (`'Y'`), `lockGridOnSynopsis` (`'Y'`), `weatherHoldAndRoll` (`'Y'`) | (in parentheses) | Not known. |

For example:

```yaml
- !!newLookConfiguration
  configuration:
    militaryTime: 'Y'
    cleanUpUnusedLogos: 'N'
```

A setting that is not in the playbook keeps its default. ESQ saves the configuration record to `config.dat` on its drive.
