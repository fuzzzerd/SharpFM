using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Input;
using SharpFM.Model.Schema;

namespace SharpFM.Schema.Editor;

[ExcludeFromCodeCoverage]
public partial class TableEditorControl : UserControl
{
    public static FieldDataType[] DataTypes { get; } = Enum.GetValues<FieldDataType>();
    public static FieldKind[] FieldKinds { get; } = Enum.GetValues<FieldKind>();

    public TableEditorControl()
    {
        InitializeComponent();
        KeyDown += OnKeyDown;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete && DataContext is TableEditorViewModel vm)
        {
            vm.RemoveSelectedField();
            e.Handled = true;
        }
    }
}
