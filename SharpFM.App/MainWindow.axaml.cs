using System;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using SharpFM.Core;
using TextMateSharp.Grammars;

namespace SharpFM.App;

public partial class MainWindow : Window
{
    private RegistryOptions _registryOptions;
    private int _currentTheme = (int)ThemeName.DarkPlus;
    private readonly TextMate.Installation _textMateInstallation;
    private readonly TextEditor _textEditor;

    public MainWindow()
    {
        InitializeComponent();

        _textEditor = this.FindControl<TextEditor>("avaloniaEditor") ?? throw new Exception("no control");

        _registryOptions = new RegistryOptions(
                (ThemeName)_currentTheme);

        _textMateInstallation = _textEditor.InstallTextMate(_registryOptions);
        Language xmlLang = _registryOptions.GetLanguageByExtension(".xml");
        _textMateInstallation.SetGrammar(_registryOptions.GetScopeByLanguageId(xmlLang.Id));
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        _textMateInstallation.Dispose();
    }
}