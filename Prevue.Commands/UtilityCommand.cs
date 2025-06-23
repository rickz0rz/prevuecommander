namespace Prevue.Commands;

public class UtilityCommand : BaseCommand
{
    private readonly string _asciiString;
    private readonly byte _family;
    private readonly string _member;
    private readonly string _subMember;

    public UtilityCommand(byte family, string member = "", string subMember = "", string asciiString = "")
        : base('x')
    {
        _family = family;
        _member = member;
        _subMember = subMember;
        _asciiString = asciiString;
    }

    public override string ToString()
    {
        return
            $"{nameof(UtilityCommand)}: Family = {_family}, Member = \"{_member}\", SubMember = \"{_subMember}\", ASCII String: \"{_asciiString}\"";
    }

    protected override byte[] GetMessageBytes()
    {
        var bytes = new List<byte>
        {
            _family
        };

        if (!string.IsNullOrWhiteSpace(_member))
        {
            bytes.AddRange(_member.ToCharArray().Select(c => (byte)c));

            if (!string.IsNullOrWhiteSpace(_subMember))
            {
                bytes.AddRange(_subMember.ToCharArray().Select(c => (byte)c));

                if (!string.IsNullOrWhiteSpace(_asciiString))
                    bytes.AddRange(_asciiString.ToCharArray().Select(c => (byte)c));
            }
        }

        return bytes.ToArray();
    }
}