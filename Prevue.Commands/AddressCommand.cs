namespace Prevue.Commands;

/// <summary>
/// The 'A' command: ESQ accepts the next commands if the selection code matches its own. The pattern can use '*'
/// (the rest of the code) and '?' (one character), so "*" selects every unit. ESQ accepts other commands only after
/// a matching 'A' command, until the next box off command.
/// </summary>
public class AddressCommand : BaseCommand
{
    /// <summary>ESQ counts a line error for a selection that is longer than 16 bytes.</summary>
    public const int MaximumLength = 16;

    public AddressCommand(string target) : base('A')
    {
        if (string.IsNullOrEmpty(target) || target.Length > MaximumLength)
            throw new ArgumentException($"The address must have 1 to {MaximumLength} characters.", nameof(target));
        Target = target;
    }

    public string Target { get; }

    public override string ToString() => $"{nameof(AddressCommand)}: Target = {Target}";

    protected override byte[] GetPayload() => Ascii(Target);
}
