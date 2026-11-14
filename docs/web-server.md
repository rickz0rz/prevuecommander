# Web server

The web server lets you send commands while Prevue Commander runs. It has a control page for a browser, and an HTTP API for scripts. Set `servePort` in the configuration, or use `--serve`:

```sh
dotnet run --project PrevueCommander -- --serve 8095 playbook.yaml
```

Prevue Commander first sends the commands of the playbook. Then it starts the server and stays connected until you press Ctrl+C. The playbook can have no commands, for example a playbook with only the `configuration` section. The server can run together with the [watch mode](watch-mode.md) and the [automatic promos](auto-promos.md). `--validate` and `--output` do not start the server.

Open `http://localhost:8095/` in a browser. The page has these parts:

- The status of the lines, the queues, the hold, the guide and the automatic promos. The page shows the new status each 2 seconds.
- Buttons for the top half of the screen: clear, logo, ad break, weather and forecast.
- Keep the top half clear for some minutes, or until you stop it. See [Keep the top half clear](control-line.md#keep-the-top-half-clear).
- A promo form.
- A local ad editor with a preview of each ad, as ESQ shows it.
- A weather form, the clock, and a button that reads the guide again.
- A box for any playbook commands in YAML.
- The list of jobs, with their errors.

The page needs no internet connection, so it works on a Raspberry Pi without internet.

> [!WARNING]
> The server has no password. Anyone who can connect to the port can send commands to ESQ. The default address `127.0.0.1` accepts only this computer. Use `serveHost: 0.0.0.0` only on a network that you trust. On Windows, an address other than `127.0.0.1` can need administrator rights.

## Jobs

Each request that sends commands makes all its commands first. If a command has an error, the server answers with status 400 and an error text, and it sends nothing. Otherwise, the server adds a job to a queue and answers at once with status 202 and the job.

The server has two queues:

- The control queue has the jobs with only control line commands, for example a promo.
- The data queue has the jobs with data line commands, for example local ads.

Each queue sends one job at a time, in order. Thus a promo does not wait for a long file transfer on the data line. A job always sends all its commands together. No other command of the same line can come between them, also from the watch mode or the automatic promos.

A job has one of these states: `queued`, `sending`, `done`, `failed` or `cancelled`. You can cancel a queued job. A job that started always finishes, because ESQ cannot use a record that stops in the middle. If a line cannot connect, the job fails, and the next job tries to connect again.

The server puts the data line commands of a request in a session. The session has the first `!!address` and the last `!!configuration` of the playbook, and `!!save` if the playbook has one. Without these commands in the playbook, the session uses the address `*` and the default status packet. `POST /prevue/commands` is different: it sends the commands as you write them.

## HTTP API

The requests that Esquire also has use the same paths and bodies, so a script works with both programs. A body is JSON, except for `POST /prevue/commands`.

| Request | Body | What it does |
|---|---|---|
| `GET /` | | The control page. |
| `GET /status` | | The status: the lines, the queues, the hold, the guide, the automatic promos and the last 10 jobs. |
| `GET /prevue/ctrl` | | The state of the control line, with the fields of Esquire. `queued` is the bytes that wait, and `seconds` is the time to send them. `sent` is the bytes sent. |
| `GET /jobs` | | The last 50 jobs, the newest first. |
| `GET /jobs/<id>` | | One job. |
| `DELETE /jobs/<id>` | | Cancels a queued job. |
| `POST /prevue/ctrl/promo` | `{"title": "Seinfeld", "brush": "AT"}` | Shows a promo. See below. |
| `POST /prevue/ctrl/clear` | | Removes what the top half shows (`!!clearTopHalf`). |
| `POST /prevue/ctrl/logo` | | Shows the loaded logo (`!!showLogo`). |
| `POST /prevue/ctrl/ad` | | Starts an ad break: a type 12 packet. See [Show a local ad](local-ads.md#show-a-local-ad). |
| `POST /prevue/ctrl/weather` | `{"forecast": false}` | Shows the current weather (`W0`). With `"forecast": true`, shows the 3-day forecast (`W3`). |
| `POST /prevue/ctrl/packets` | `[{"type": 17, "body": "Seinfeld"}]` | Sends control packets, as `!!controlPacket`. In JSON, `\u0012` is the separator of the two boxes. |
| `POST /prevue/ctrl/hold` | `{"minutes": 30, "intervalSeconds": 150}` | Keeps the top half clear. It sends a clear at once and then at each interval. Without `minutes`, the hold has no end. `intervalSeconds` is 5 to 170. The default is 150. |
| `DELETE /prevue/ctrl/hold` | | Stops the hold. The logo rotation starts again. |
| `POST /prevue/data/local-ads` | `{"ads": ["%CENTER%Joe's Pizza"], "dryRun": false}` | Replaces the local ads of ESQ, as `!!localAds`. The answer has the preview and the warnings of each ad. With `"dryRun": true`, the server only gives the preview and sends nothing. |
| `POST /prevue/data/weather` | `{"status": {...}, "text": {...}, "forecast": {...}, "show": true, "pauseSeconds": 5}` | Sends the weather status, the weather text and the 3-day forecast, with the settings of `!!weatherStatus`, `!!weatherText` and `!!weatherForecast`. Each part is optional, but one part is necessary. With `show`, the server then waits `pauseSeconds` and shows the weather. With a forecast, it shows the forecast (`W3`). Without a forecast, it shows the current weather (`W0`). |
| `POST /prevue/data/clock` | `{"date": "2020-11-01T16:00"}` | Sets the clock of ESQ, as `!!clock`. Without `date`, it sends the current time. The server first sends the DST window of the year of the clock, as `!!configureDst`. |
| `POST /prevue/data/weather-gov` | `{"zip": "48201", "show": "forecast", "pauseSeconds": 5}` | Gets the weather of weather.gov and sends it, as `!!weatherGov`. The body can have all the settings of `!!weatherGov`. `show` is `none` (the default), `current` or `forecast`. With `current` or `forecast`, the server then waits `pauseSeconds` and shows the weather. |
| `POST /prevue/weather/refresh` | | Reads the weather of the `!!weatherGov` commands of the playbook again, and sends it. The playbook must have a `!!weatherGov` command. |
| `POST /prevue/guide/refresh` | | Reads the guide of the imports of the playbook again, and sends the changes, as the [watch mode](watch-mode.md) does. The answer has the number of listings commands that it sent. The playbook must have a guide data import. |
| `POST /prevue/commands` | YAML | Sends playbook commands. The body is a YAML list of commands, or a playbook with `commands:`. The server uses only the commands of a playbook. |

The body of a promo is as the settings of `!!promo`. It has a box on the right, a box on the left, or both boxes:

```json
{
  "right": {"title": "Bob's Burgers", "channels": "*", "brush": "AT"},
  "left": {"title": "Seinfeld", "channels": "KTIV*", "brush": "DT"},
  "first": "left",
  "style": "box"
}
```

`title`, `channels` and `brush` at the top of the body are for the right box, for example `{"title": "Seinfeld"}`.

Examples with `curl`:

```sh
curl -X POST localhost:8095/prevue/ctrl/promo -d '{"title": "Seinfeld", "brush": "AT"}'
curl -X POST localhost:8095/prevue/ctrl/clear -d ''
curl -X POST localhost:8095/prevue/ctrl/hold -d '{"minutes": 30}'
curl -X DELETE localhost:8095/prevue/ctrl/hold
curl -X POST localhost:8095/prevue/data/local-ads -d '{"ads": ["%CENTER%Visit Joe'"'"'s Pizza"]}'
curl -X POST localhost:8095/prevue/ctrl/ad -d ''
curl -X POST localhost:8095/prevue/commands --data-binary $'- !!showLogo {}\n'
curl -X POST localhost:8095/prevue/data/weather-gov -d '{"zip": "48201", "show": "current"}'
curl localhost:8095/status
```

A POST needs a `Content-Length` header. Thus use `-d ''` for a request without a body. HttpListener answers "411 Length Required" to a POST without the header, and the server then does nothing.

A test with Esquire sent each of these requests. ESQ showed two local ads, a promo, the weather and a clear. Then a hold of 4 minutes kept the top half clear.
