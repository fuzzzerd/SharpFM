using System;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using AvaloniaEdit;
using SharpFM.Core;

namespace SharpFM.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var editor = this.FindControl<TextEditor>("avaloniaEditor") ?? throw new Exception("no control");
        editor.Document = new AvaloniaEdit.Document.TextDocument("Hello world");
    }
}