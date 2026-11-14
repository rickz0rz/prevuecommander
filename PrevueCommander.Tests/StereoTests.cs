using Prevue.Commands;
using Prevue.Commands.Listings;
using Prevue.Commands.Model;
using PrevueCommander.XmlTv;
using Xunit;

namespace PrevueCommander.Tests;

public class StereoTests
{
    private static readonly DateTime Now = new(2026, 11, 13, 18, 10, 0);

    private static GuideChannel Channel(bool stereoProgram) =>
        new("2", "TESTA", [new GuideProgram(new DateTime(2026, 11, 13, 18, 0, 0), new DateTime(2026, 11, 13, 18, 30, 0),
            "Show |", IsStereo: stereoProgram)]);

    [Theory]
    [InlineData(false, false, 0x01)]
    [InlineData(true, false, 0x03)]
    [InlineData(false, true, 0x81)]
    [InlineData(true, true, 0x83)]
    public void LineUp_SetsBit7ForAStereoChannel(bool premium, bool stereo, byte attribute)
    {
        var bytes = new ChannelLineUpCommand(new DateOnly(2026, 11, 13),
            [new Channel("TESTA", "2", "TESTA", premium, stereo)]).Render();
        // 55 AA 'C', the group code, $12 and then the attribute.
        Assert.Equal(attribute, bytes[5]);
    }

    [Theory]
    [InlineData("Show |", "Show In Stereo |")]
    [InlineData("Show", "Show In Stereo")]
    public void AddStereo_PutsTheTagBeforeTheClosedCaptionSymbol(string text, string expected) =>
        Assert.Equal(expected, ProgramText.AddStereo(text));

    [Fact]
    public void AddStereo_ShortensTheTextBeforeTheTag()
    {
        var text = ProgramText.AddStereo(new string('a', 118) + " |", 120);
        Assert.Equal(120, text.Length);
        Assert.EndsWith(" In Stereo |", text);
    }

    [Fact]
    public void Build_WithStereo_MarksTheChannels_AndTheStereoPrograms()
    {
        var commands = ListingsBuilder.Build([Channel(true)], Now, stereo: true);

        Assert.All(commands.OfType<ChannelLineUpCommand>(), c => Assert.True(c.Channels.Single().Stereo));
        Assert.Contains(commands.OfType<ChannelProgramCommand>(), p => p.Text == "Show In Stereo |");
    }

    [Fact]
    public void Build_WithStereo_DoesNotChangeAMonoProgram()
    {
        var commands = ListingsBuilder.Build([Channel(false)], Now, stereo: true);
        Assert.All(commands.OfType<ChannelProgramCommand>(), p => Assert.Equal("Show |", p.Text));
    }

    [Fact]
    public void Build_WithoutStereo_ChangesNothing()
    {
        var commands = ListingsBuilder.Build([Channel(true)], Now);

        Assert.All(commands.OfType<ChannelLineUpCommand>(), c => Assert.False(c.Channels.Single().Stereo));
        Assert.All(commands.OfType<ChannelProgramCommand>(), p => Assert.Equal("Show |", p.Text));
    }

    [Theory]
    [InlineData("stereo", true)]
    [InlineData("dolby digital", true)]
    [InlineData("surround", true)]
    [InlineData("mono", false)]
    [InlineData("MONO", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void XmlTv_IsStereo(string? value, bool stereo) => Assert.Equal(stereo, XmlTvGuide.IsStereo(value));
}
