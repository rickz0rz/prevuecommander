namespace Prevue.Commands;

/// <summary>
/// A pause before the next command. It is not an ESQ command and sends no bytes. ESQ reads the packets of the control
/// line only when its display is not busy, so put a pause between two promos.
/// </summary>
public sealed class PauseCommand : BaseCommand
{
    public PauseCommand(TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(duration), "A pause cannot be negative.");
        Duration = duration;
    }

    public TimeSpan Duration { get; }

    public override SerialLine Line => SerialLine.None;

    protected override byte[] GetPayload() => [];

    public override byte[] Render() => [];

    public override string ToString() => $"{nameof(PauseCommand)} {Duration.TotalSeconds:0.#} s";
}
