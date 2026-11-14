# Weather

## 3-day forecast

| Command | Settings | Description |
|---|---|---|
| `!!weatherForecast` | `minutes` (`60`), `label` (`*`), `days` | Sends the 3-day forecast. ESQ shows it for `minutes` (1 to 255). `days` has a maximum of 4 days. Each day has `icon`, `high`, `low` and an optional `date`. |

The settings of a day:

- `icon` 2 to 6 shows the picture `PWI1` to `PWI5` of the drive. Without `icon`, the day has no picture.
- `high` and `low` are -99 to 999. Without a value, ESQ shows `???`.
- `date` is a day, for example `2026-11-15`. Without dates, the first day is tomorrow, and each next day is one day later.

ESQ shows three panels: tomorrow, the day after, and the day after that. Each panel has the name of the day, the picture, and the high and the low, for example `82/52`. ESQ uses its own clock for the names of the days, so set the clock of ESQ correctly. ESQ keeps four days but shows only three. ESQ ignores a day that is not one of the next four days. A panel without a day shows "No Forecast Weather Data Available".

ESQ shows the panels only when two things are true:

- The forecast time (`minutes`) has not ended.
- The digit of `!!weatherStatus` is not `'0'`. Send `!!weatherStatus` before the forecast.

Keep `label: '*'`. ESQ compares the label with a forecast label that the 'i' command sets. Prevue Commander does not send the 'i' command, so the forecast label is empty. Only `*` matches an empty label.

This example shows a sun, clouds and rain for the next three days:

```yaml
- !!address
  target: '*'
- !!weatherStatus
  label: DETROIT
  text: Detroit, Michigan
- !!weatherForecast
  minutes: 30
  days:
  - {icon: 2, high: 81, low: 51}
  - {icon: 3, high: 82, low: 52}
  - {icon: 4, high: 83, low: 53}
- !!boxOff {}
- !!pause
  seconds: 5
- !!showWeather
  forecast: true
```

## Weather from weather.gov

`!!weatherGov` gets the weather of a US location from [weather.gov](https://www.weather.gov/documentation/services-web-api), the free API of the National Weather Service. It needs no key. weather.gov has data only for the USA. The command sends three parts:

- The status: the city, for example `Philadelphia`.
- The current weather of the nearest weather station, as a weather text. `!!showWeather {}` shows it.
- The forecast of the next 3 days. `!!showWeather` with `forecast: true` shows it.

```yaml
- !!address
  target: '*'
- !!weatherGov
  zip: '48201'
- !!boxOff {}
- !!pause
  seconds: 5
- !!showWeather {}
```

| Setting | Default | Description |
|---|---|---|
| `zip` | | A US ZIP code, for example `'48201'`. Put it in quotes, because a ZIP code can start with `0`. |
| `latitude`, `longitude` | | The location, for example `42.33` and `-83.05`. Use them in place of `zip`. |
| `label` | `LOCAL` | The label of the weather status, 0 to 10 characters. |
| `status` | `{city}` | The status text at the top of the weather. It can have the placeholders of `currentLines`. |
| `current` | `true` | Sends the current weather. |
| `forecast` | `true` | Sends the 3-day forecast. |
| `minutes` | `90` | The minutes that ESQ shows the weather, 1 to 255. Each new read starts the minutes again. |
| `units` | `F` | `F` for degrees Fahrenheit and miles per hour. `C` for degrees Celsius and kilometers per hour. |
| `currentLines` | See below | The lines of the current weather, a maximum of 10. |
| `contact` | | A contact, for example an e-mail address. weather.gov asks each program to identify itself, so that it can contact the owner if the program causes problems. Without `contact`, Prevue Commander sends only its name and the address of this project. |

ESQ shows the lines of the current weather in pairs. The first line of a pair is at the left of the picture. The second line is at the right. The default lines copy the "current conditions" screen of Prevue Weather:

```yaml
  currentLines:
  - '{condition}'
  - Temp {temperature}{degree}{unit}
  - Wind {wind}
  - Pressure {pressure}
  - Humidity {humidity}%
  - DewPoint {dewPoint}{degree}{unit}
  - Visibility {visibility}
  - ''
```

ESQ then shows this screen:

```
               Philadelphia
      Cloudy                  Temp 41°F
  Wind NW 11 mph   (picture)  Pressure 29.81 in.
   Humidity 56%               DewPoint 27°F
  Visibility 10 M
```

The last line is empty, so the right column has only three lines. Put a line that starts with `{` in quotes. A line can have these placeholders:

| Placeholder | Value |
|---|---|
| `{city}`, `{state}` | The city and the state of the location, for example `Detroit` and `MI`. |
| `{condition}` | The weather of the station, for example `Cloudy` or `Light Rain`. |
| `{temperature}` | The temperature of the station. |
| `{feelsLike}` | The wind chill or the heat index. Without one, the temperature. |
| `{dewPoint}` | The dew point. |
| `{pressure}` | The barometric pressure, for example `29.81 in.`, or `1009 hPa` with `units: C`. |
| `{visibility}` | The visibility, for example `10 M` (miles), or `16 km` with `units: C`. |
| `{degree}` | The degree sign `°`. With `characterSet: Ascii`, ESQ gets nothing for it. |
| `{humidity}` | The relative humidity in percent. |
| `{wind}` | The wind direction and speed, for example `W 5 mph`, or `Calm`. |
| `{unit}` | `F` or `C`. |
| `{time}` | The time of the observation, for example `6:10 AM`. |
| `{next}` | The name of the next forecast period, for example `Today` or `Tonight`. |
| `{nextForecast}` | The weather of the next period, for example `Partly Sunny`. |
| `{nextTemperature}` | The temperature of the next period. |
| `{nextHighLow}` | `High` for a day period, `Low` for a night period. |

A missing value shows `--`. Prevue Commander removes the characters that ESQ cannot show. With `characterSet: Ascii`, it also removes `°` and the accents. If the status is too long, ESQ gets only its start.

Prevue Commander chooses the picture from the condition code of weather.gov:

| Picture | Conditions |
|---|---|
| `PWI1`, the sun | Clear, a few clouds, hot, cold |
| `PWI2`, clouds | Overcast, fog, haze, smoke, dust |
| `PWI3`, rain | Rain, showers, thunderstorms, freezing rain, sleet, tropical storms |
| `PWI4`, the sun with clouds | Partly cloudy, mostly cloudy |
| `PWI5`, snow | Snow, rain and snow, a blizzard |

The drive has only these five pictures. ESQ shows the sun for a clear night too.

Prevue Commander finds the location of a ZIP code in a table in the program. The table has the center of each ZIP Code Tabulation Area of the 2024 Gazetteer of the U.S. Census Bureau. Some ZIP codes, for example ZIP codes for post office boxes only, are not in the table. For them, use `latitude` and `longitude`.

To send new weather at an interval, set `weatherMinutes` in the configuration, or use `--weather`. weather.gov updates its forecast a few times each day, so an interval of 30 or 60 minutes is sufficient. If weather.gov does not answer, Prevue Commander shows the error, and ESQ keeps the last weather. The next interval tries again.

```sh
dotnet run --project PrevueCommander -- --weather 30 playbook.yaml
```
