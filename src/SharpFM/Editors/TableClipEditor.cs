using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia.Threading;
using SharpFM.Schema.Editor;
using SharpFM.Schema.Model;

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
    public TableEditorViewModel ViewModel { get; private set; }

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
        ViewModel.SyncToModel();
        return ViewModel.Table.ToXml();
    }

    public void FromXml(string xml)
    {
        var incoming = FmTable.FromXml(xml);
        PatchViewModel(incoming);
    }

    private void SubscribeToViewModel(TableEditorViewModel vm)
    {
        vm.Fields.CollectionChanged += OnCollectionChanged;
        vm.PropertyChanged += OnViewModelPropertyChanged;

        foreach (var field in vm.Fields)
            field.PropertyChanged += OnFieldPropertyChanged;
    }

    private void UnsubscribeFromViewModel(TableEditorViewModel vm)
    {
        vm.Fields.CollectionChanged -= OnCollectionChanged;
        vm.PropertyChanged -= OnViewModelPropertyChanged;

        foreach (var field in vm.Fields)
            field.PropertyChanged -= OnFieldPropertyChanged;
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Subscribe to new fields, unsubscribe from removed ones
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

    /// <summary>
    /// Diff and patch the existing ViewModel fields from incoming XML.
    /// Preserves UI state (selection, scroll) when possible.
    /// Falls back to full rebuild if the table identity changed.
    /// </summary>
    private void PatchViewModel(FmTable incoming)
    {
        var current = ViewModel;

        // If the table name/identity changed entirely, full rebuild
        if (current.Table.Name != incoming.Name && current.Table.Id != incoming.Id)
        {
            UnsubscribeFromViewModel(current);
            var table = incoming;
            ViewModel = new TableEditorViewModel(table);
            SubscribeToViewModel(ViewModel);
            return;
        }

        // Update table name
        if (current.TableName != incoming.Name)
            current.TableName = incoming.Name;

        // If any fields lack unique IDs (e.g., newly added fields with Id=0),
        // fall back to a full rebuild since we can't reliably diff by ID.
        var hasUniqueIds = incoming.Fields.Select(f => f.Id).Distinct().Count() == incoming.Fields.Count
                        && current.Fields.Select(f => f.Id).Distinct().Count() == current.Fields.Count;

        if (!hasUniqueIds)
        {
            UnsubscribeFromViewModel(current);
            ViewModel = new TableEditorViewModel(incoming);
            SubscribeToViewModel(ViewModel);
            return;
        }

        // Build lookup of incoming fields by Id
        var incomingById = incoming.Fields.ToDictionary(f => f.Id);
        var currentById = current.Fields.ToDictionary(f => f.Id);

        // Remove fields not in incoming
        var toRemove = current.Fields.Where(f => !incomingById.ContainsKey(f.Id)).ToList();
        foreach (var field in toRemove)
        {
            field.PropertyChanged -= OnFieldPropertyChanged;
            current.Fields.Remove(field);
        }

        // Update existing fields and add new ones
        for (int i = 0; i < incoming.Fields.Count; i++)
        {
            var inField = incoming.Fields[i];

            if (currentById.TryGetValue(inField.Id, out var existing))
            {
                // Update properties on the existing field in-place
                PatchField(existing, inField);

                // Move to correct position if needed
                var currentIdx = current.Fields.IndexOf(existing);
                if (currentIdx != i && currentIdx >= 0 && i < current.Fields.Count)
                    current.Fields.Move(currentIdx, i);
            }
            else
            {
                // New field — insert at the correct position
                inField.PropertyChanged += OnFieldPropertyChanged;
                if (i < current.Fields.Count)
                    current.Fields.Insert(i, inField);
                else
                    current.Fields.Add(inField);
            }
        }

        // Sync the underlying model
        current.SyncToModel();
    }

    private static void PatchField(FmField target, FmField source)
    {
        if (target.Name != source.Name) target.Name = source.Name;
        if (target.DataType != source.DataType) target.DataType = source.DataType;
        if (target.Kind != source.Kind) target.Kind = source.Kind;
        if (target.Repetitions != source.Repetitions) target.Repetitions = source.Repetitions;
        if (target.Comment != source.Comment) target.Comment = source.Comment;
        if (target.NotEmpty != source.NotEmpty) target.NotEmpty = source.NotEmpty;
        if (target.Unique != source.Unique) target.Unique = source.Unique;
        if (target.Existing != source.Existing) target.Existing = source.Existing;
        if (target.MaxDataLength != source.MaxDataLength) target.MaxDataLength = source.MaxDataLength;
        if (target.ValidationCalculation != source.ValidationCalculation) target.ValidationCalculation = source.ValidationCalculation;
        if (target.ErrorMessage != source.ErrorMessage) target.ErrorMessage = source.ErrorMessage;
        if (target.RangeMin != source.RangeMin) target.RangeMin = source.RangeMin;
        if (target.RangeMax != source.RangeMax) target.RangeMax = source.RangeMax;
        if (target.AutoEnter != source.AutoEnter) target.AutoEnter = source.AutoEnter;
        if (target.AllowEditing != source.AllowEditing) target.AllowEditing = source.AllowEditing;
        if (target.AutoEnterValue != source.AutoEnterValue) target.AutoEnterValue = source.AutoEnterValue;
        if (target.Calculation != source.Calculation) target.Calculation = source.Calculation;
        if (target.AlwaysEvaluate != source.AlwaysEvaluate) target.AlwaysEvaluate = source.AlwaysEvaluate;
        if (target.CalculationContext != source.CalculationContext) target.CalculationContext = source.CalculationContext;
        if (target.SummaryOp != source.SummaryOp) target.SummaryOp = source.SummaryOp;
        if (target.SummaryTargetField != source.SummaryTargetField) target.SummaryTargetField = source.SummaryTargetField;
        if (target.IsGlobal != source.IsGlobal) target.IsGlobal = source.IsGlobal;
        if (target.Indexing != source.Indexing) target.Indexing = source.Indexing;
    }
}
