using Avalonia.Controls;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;

namespace SharpFM.Plugin.XmlViewer;

public partial class XmlViewerPanel : UserControl
{
    private TextMate.Installation? _textMateInstallation;

    public XmlViewerPanel()
    {
        InitializeComponent();

        var editor = this.FindControl<TextEditor>("xmlEditor");
        if (editor is null) return;

        var registryOptions = new RegistryOptions((ThemeName)(int)ThemeName.DarkPlus);
        _textMateInstallation = editor.InstallTextMate(registryOptions);
        var xmlLang = registryOptions.GetLanguageByExtension(".xml");
        _textMateInstallation.SetGrammar(registryOptions.GetScopeByLanguageId(xmlLang.Id));
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _textMateInstallation?.Dispose();
    }
}
