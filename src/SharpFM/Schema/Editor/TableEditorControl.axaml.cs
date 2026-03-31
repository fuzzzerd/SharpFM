using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SharpFM.Schema.Model;

namespace SharpFM.Schema.Editor;

public partial class TableEditorControl : UserControl
{
    public static FieldDataType[] DataTypes { get; } = Enum.GetValues<FieldDataType>();
    public static FieldKind[] FieldKinds { get; } = Enum.GetValues<FieldKind>();

    public TableEditorControl()
    {
        InitializeComponent();

        var addBtn = this.FindControl<Button>("addFieldButton");
        var removeBtn = this.FindControl<Button>("removeFieldButton");
        var calcBtn = this.FindControl<Button>("editCalcButton");

        if (addBtn != null) addBtn.Click += (_, _) => GetVm()?.AddField();
        if (removeBtn != null) removeBtn.Click += (_, _) => GetVm()?.RemoveSelectedField();
        if (calcBtn != null) calcBtn.Click += (_, _) => GetVm()?.OpenCalculationEditor();

        KeyDown += OnKeyDown;
    }

    private TableEditorViewModel? GetVm() => DataContext as TableEditorViewModel;

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete && GetVm() is { } vm)
        {
            vm.RemoveSelectedField();
            e.Handled = true;
        }
    }
}
