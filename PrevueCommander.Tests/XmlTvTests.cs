using System.Collections.Generic;
using PrevueCommander.XmlTv.Model;
using Xunit;

namespace PrevueCommander.Tests;

public class XmlTvTests
{
    [Theory]
    [InlineData("6", "6")]
    [InlineData("6 WTVS", "6")]
    [InlineData("WTVS 6", "6")]
    [InlineData("6.1", "6.1")]
    [InlineData("6.1 WTVS", "6.1")]
    [InlineData("6-1", "6-1")]
    [InlineData("6-1 WTVS", "6-1")]
    public void TestChannelNumberExtraction(string input, string expected)
    {
        var channel = new Channel
        {
            Displayname = new List<string>
            {
                input
            }
        };
        Assert.Equal(expected, channel.ChannelNumber);
    }

    [Theory]
    [InlineData("6 WTVS", "WTVS")]
    [InlineData("6.1 WTVS", "WTVS")]
    [InlineData("6 WTVS TX42822:-", "WTVS")]
    [InlineData("WTVS", "WTVS")]
    public void TestCallSignExtraction(string input, string expected)
    {
        var channel = new Channel
        {
            Displayname = new List<string>
            {
                input
            }
        };

        Assert.Equal(expected, channel.CallSign);
    }
}
