namespace Prevue.Commands;

public class AddressCommand : BaseCommand
{
    private readonly string _target;
    
    public AddressCommand(string target) : base((byte)'A')
    {
        _target = target;
    }

    protected override byte[] GetMessageBytes()
    {
        return _target.ToCharArray().Select(c => (byte)c).ToArray();
    }
}