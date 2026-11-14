# The clock and daylight saving time

`!!clock` sends the local time and a daylight saving time flag. The flag tells ESQ that the time includes daylight saving time. ESQ compares the flag with its own DST window from `!!configureDst`. When they do not agree, ESQ moves the clock by one hour. Tests with Esquire showed these results:

| Clock that was sent | DST window of ESQ | ESQ shows |
|---|---|---|
| Winter, no flag | Any | The correct time. |
| Summer, with the flag | The window of the year | The correct time. |
| Summer, with the flag | No window of the year | One hour early. |
| Summer, no flag | The window of the year | One hour late. |

Prevue Commander sets the flag from the time zone of this computer, so send `!!configureDst` in the same playbook. The order of `!!clock` and `!!configureDst` is not important. If a playbook sends a summer clock without `!!configureDst`, Prevue Commander shows a warning. Prevue Commander cannot read the drive, so the warning also shows when the drive already has the window.

Use a time zone with the same daylight saving time rules as the area of ESQ. A computer without daylight saving time, for example in UTC, sends no flag. If ESQ has a window, it then shows a summer clock one hour late.

## The window in `dst.dat`

ESQ keeps the window in memory. A test with Esquire showed these rules for the file `dst.dat` on the drive:

- ESQ writes the window to `dst.dat` only after `!!save` and the next `!!boxOff`. Without `!!save`, ESQ loses the window when it restarts.
- ESQ reads `dst.dat` when it starts. After a restart, a summer clock without `!!configureDst` showed the correct time.
- The file has the window of one year only. ESQ accepts a window only within one year of its clock. Thus send `!!configureDst` and `!!save` each year, for example in the normal playbook.
- The watch mode and the web server do not send `!!configureDst` again. If Prevue Commander runs across a new year, send the new window yourself.

This playbook sends the window and makes ESQ keep it:

```yaml
- !!address
  target: '*'
- !!configureDst {}
- !!save {}
- !!boxOff {}
```

`dst.dat` has 60 bytes and no line breaks. It has two windows: first `g2`, then `g3`. Each window is a space, `g2:` or `g3:`, $04, the start, $13 and the end. A time is `YYYYDDDHH:MM`, where DDD is the day of the year from 0. For example, the 2026 window of Michigan starts with ` g2:` $04 `202606603:00` $13 `202630401:00`. Do not write the file yourself unless you prepare a new drive. Then copy a `dst.dat` that ESQ wrote. ESQ makes and checks the format of the window itself.

ESQ also ignores the clock when `clockCommandInFSetting` of `!!newLookConfiguration` is not `'2'`. It then counts a line error (`DATACErrs`). If the playbook does not send `!!newLookConfiguration`, ESQ uses the setting of its drive.
