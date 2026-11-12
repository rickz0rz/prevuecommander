namespace Prevue.Commands;

/// <summary>The actions of the 'x' command that are known.</summary>
public enum UtilityAction
{
    /// <summary>
    /// Reload the graphic ads of gfx/g_ads.data. ESQ does not read LOGO.LST again
    /// (<c>ED1_WaitForFlagAndClearBit0</c> calls <c>ESQIFF_ReloadExternalAssetCatalogBuffers</c> with mode 1).
    /// </summary>
    ReloadGraphicAds,

    /// <summary>
    /// Scan the logo directory if the configuration allows it, and then reload gfx/g_ads.data and LOGO.LST
    /// (<c>ED1_WaitForFlagAndClearBit1</c>, mode 0). ESQ frees the loaded logos and starts at the first line of the
    /// new list. In a test with Esquire, ESQ stopped with a bad FreeMem after this action. Test it before you use it.
    /// </summary>
    ReloadLogoList,

    /// <summary>Rebuild the brush list from the built-in table.</summary>
    RebuildBrushList,

    /// <summary>Reload BRUSH.INI.</summary>
    ReloadBrushes,

    /// <summary>Reload gradient.ini.</summary>
    ReloadGradient,

    /// <summary>Reload BANNER.INI and the banner graphic.</summary>
    ReloadBanner,

    /// <summary>Reload default.ini.</summary>
    ReloadDefaults,

    /// <summary>Reload qtable.ini.</summary>
    ReloadQTable,

    /// <summary>Reload SourceCfg.ini.</summary>
    ReloadSourceConfiguration,

    /// <summary>Open and close the ESC menu.</summary>
    ToggleMenu,

    /// <summary>Show the diagnostics screen.</summary>
    ShowDiagnostics,

    /// <summary>Show the version screen.</summary>
    ShowVersion
}

/// <summary>
/// The 'x' command: a text that starts with an ASCII digit family (<c>PARSEINI_HandleFontCommand</c>). Family "32"
/// runs an AmigaDOS command, family "33" reloads files and family "34" shows screens.
/// </summary>
public class UtilityCommand : BaseCommand
{
    /// <summary>ESQ counts a line error for a record that is longer than 80 bytes.</summary>
    private const int MaximumLength = 80;

    public UtilityCommand(UtilityAction action) : this(Code(action))
    {
        Action = action;
    }

    private UtilityCommand(string text) : base('x')
    {
        if (text.Length is 0 or > MaximumLength)
            throw new ArgumentException($"A utility command has 1 to {MaximumLength} characters.", nameof(text));
        Text = text;
    }

    /// <summary>
    /// Runs an AmigaDOS command line with dos <c>Execute</c>, for example "MakeDir RAM:Logos". ESQ waits until the
    /// command ends.
    /// </summary>
    public static UtilityCommand Shell(string commandLine)
    {
        if (commandLine.Length == 0 || commandLine.Any(c => c is < ' ' or > '~'))
            throw new ArgumentException("An AmigaDOS command line must have printable ASCII characters only.",
                nameof(commandLine));
        return new UtilityCommand(ShellFamily + commandLine);
    }

    private const string ShellFamily = "32";

    public UtilityAction? Action { get; }

    public string Text { get; }

    public override string ToString() => $"{nameof(UtilityCommand)}: {Action?.ToString() ?? "Shell"} (\"{Text}\")";

    protected override byte[] GetPayload() => Ascii(Text);

    private static string Code(UtilityAction action) => action switch
    {
        UtilityAction.ReloadGraphicAds => "334",
        UtilityAction.ReloadLogoList => "335",
        UtilityAction.RebuildBrushList => "336",
        UtilityAction.ReloadBrushes => "33a",
        UtilityAction.ReloadGradient => "33b",
        UtilityAction.ReloadBanner => "33c",
        UtilityAction.ReloadDefaults => "33d",
        UtilityAction.ReloadQTable => "33Q",
        UtilityAction.ReloadSourceConfiguration => "33s",
        UtilityAction.ToggleMenu => "340",
        UtilityAction.ShowDiagnostics => "341",
        UtilityAction.ShowVersion => "342",
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
    };
}
