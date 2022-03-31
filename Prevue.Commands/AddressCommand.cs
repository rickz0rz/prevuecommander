namespace Prevue.Commands;

public class AddressCommand : BaseCommand
{
    private readonly string _target;

    public AddressCommand(string target) : base('A')
    {
        _target = target;
    }

    public override string ToString()
    {
        return $"{nameof(AddressCommand)}: Target = {_target}";
    }

    protected override byte[] GetMessageBytes()
    {
        return _target.ToCharArray().Select(c => (byte)c).ToArray();
    }
}
