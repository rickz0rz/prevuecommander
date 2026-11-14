# Watch mode

In the watch mode, Prevue Commander stays connected after the commands. At each interval, it reads the guide of the imports again and sends only the changes. Set `watchMinutes` in the configuration, or use `--watch`:

```sh
dotnet run --project PrevueCommander -- --watch 10 playbook.yaml
```

At each interval, Prevue Commander follows these rules:

- If the lineup of a day changed, it sends the lineup and all the programs of that day. ESQ deletes the programs of a day when its lineup changes.
- If the lineup of a day did not change, it sends only the new and changed programs.
- At 5:30 AM, ESQ changes to the next day. The new next day gets its lineup and all its programs.
- It sends the changes in a session. The session has the `!!address` and the `!!configuration` of the playbook, the changes, `!!save` if the playbook has it, and `!!boxOff`. If the guide has no changes, it sends nothing.
- It does not send the channel logos again.
- If it cannot read the guide, for example because the server does not answer, it shows the error. It tries again at the next interval.

ESQ cannot delete one program. Thus a program that is not in the new guide stays until a new lineup or the next day.

Press Ctrl+C to stop the watch mode. Prevue Commander also stops correctly when a service manager, for example systemd, sends SIGTERM. `--validate` and `--output` do not use the watch mode.
