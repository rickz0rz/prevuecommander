# Development

## Build and test

```sh
dotnet build
dotnet test
```

GitHub Actions runs these commands for each pull request and each push to `main`. The workflow is in `.github/workflows/build.yml`.

`build-osx.sh` makes `PrevueCommander-osx`, a universal macOS program for Apple silicon and Intel. `build-win.sh` makes a Windows x64 program in `publish_win-x64`. `build-linux-arm64.sh` makes a Linux arm64 program in `publish_linux-arm64`, for example for a Raspberry Pi. These programs do not need .NET on the computer. Run them with the playbook as the argument, for example `./PrevueCommander-osx playbook.yaml`.

Do not trim the output: YamlDotNet and the XMLTV reader need the metadata that trimming removes.

To see what ESQ does with each command, test with Esquire or FS-UAE. See [Test with an emulator](testing.md).

## Project layout

| Project | Description |
|---|---|
| `Prevue.Core` | Broadcast days, slots, group codes and text conversion. |
| `Prevue.Graphics` | The PNG reader, the color reduction and the IFF writer for the pictures of ESQ. |
| `Prevue.Commands` | The commands of the data line and of the control line (`Control`), and the listings builder. Each class tells the format that ESQ reads. |
| `PrevueCommander.XmlTv` | The XMLTV reader. |
| `PrevueCommander.ChannelsDVR` | The Channels DVR client. |
| `PrevueCommander.WeatherGov` | The weather.gov client, the ZIP code table and the choice of the weather pictures. |
| `PrevueCommander` | The program, the playbook commands and the web server (`Server`, and the control page in `wwwroot`). |
| `PrevueCommander.Tests` | The tests. |
