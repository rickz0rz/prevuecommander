using Prevue.Core;

namespace Prevue.Commands;

public class TitleCommand : BaseCommand
{
    private readonly string _text;

    public TitleCommand(string text) : base('T')
    {
        _text = text;
    }

    public override string ToString()
    {
        return $"{nameof(TitleCommand)}: Title = \"{_text}\"";
    }

    protected override byte[] GetMessageBytes()
    {
        return Helpers.ConvertStringToBytes(_text, Helpers.GuideFontTokenMapper);
    }
}