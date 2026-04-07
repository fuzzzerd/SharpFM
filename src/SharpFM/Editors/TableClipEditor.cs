using System;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Threading;
using SharpFM.Schema.Editor;
using SharpFM.Model.Schema;

namespace SharpFM.Editors;

/// <summary>
/// Editor for table/field clips (Mac-XMTB, Mac-XMFD). Wraps a TableEditorViewModel
/// and tracks field collection and property changes for live sync.
/// </summary>
public class TableClipEditor : IClipEditor
{
    private readonly DispatcherTimer _debounceTimer;

    public event EventHandler? ContentChanged;

    /// <summary>The TableEditorViewModel bound to the DataGrid.</summary>
    public TableEditorViewModel ViewModel { get; }

    public bool IsPartial => false;

    public TableClipEditor(string? xml)
    {
        var table = FmTable.FromXml(xml ?? "");
        ViewModel = new TableEditorViewModel(table);

        _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _debounceTimer.Tick += (_, _) =>
        {
            _debounceTimer.Stop();
            ContentChanged?.Invoke(this, EventArgs.Empty);
        };

        SubscribeToViewModel(ViewModel);
    }

    public string ToXml()
    {
        return ViewModel.Table.ToXml();
    }

    public void FromXml(string xml)
    {
        // Not used for external updates — ReplaceEditor creates a new TableClipEditor.
        // Kept for IClipEditor interface compliance.
    }

    private void SubscribeToViewModel(TableEditorViewModel vm)
    {
        vm.Fields.CollectionChanged += OnCollectionChanged;
        vm.PropertyChanged += OnViewModelPropertyChanged;

        foreach (var field in vm.Fields)
            field.PropertyChanged += OnFieldPropertyChanged;
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
            foreach (FmField field in e.NewItems)
                field.PropertyChanged += OnFieldPropertyChanged;

        if (e.OldItems is not null)
            foreach (FmField field in e.OldItems)
                field.PropertyChanged -= OnFieldPropertyChanged;

        RestartDebounce();
    }

    private void OnFieldPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        RestartDebounce();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TableEditorViewModel.TableName))
            RestartDebounce();
    }

    private void RestartDebounce()
    {
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }
}
