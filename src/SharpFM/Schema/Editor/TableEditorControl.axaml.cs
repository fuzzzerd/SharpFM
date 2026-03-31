using System;
using System.Windows.Input;
using Avalonia.Controls;
using SharpFM.Schema.Model;

namespace SharpFM.Schema.Editor;

public partial class TableEditorControl : UserControl
{
    public static FieldDataType[] DataTypes { get; } = Enum.GetValues<FieldDataType>();
    public static FieldKind[] FieldKinds { get; } = Enum.GetValues<FieldKind>();

    public TableEditorControl()
    {
        InitializeComponent();
    }
}
