using Prevue.Core;

namespace Prevue.Commands;

public class TitleCommand : BaseCommand
{
    private readonly string _text;

    public TitleCommand(string text) : base((byte)'T')
    {
        _text = text;
    }

    protected override byte[] GetMessageBytes()
    {
        return Helpers.ConvertStringToBytes(_text, Helpers.GuideFontTokenMapper);
    }
}