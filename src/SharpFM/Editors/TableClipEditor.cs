using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using SharpFM.Schema.Editor;
using SharpFM.Schema.Model;

namespace SharpFM.Editors;

/// <summary>
/// Editor for table/field clips (Mac-XMTB, Mac-XMFD). The FmTable model is the source of truth.
/// The DataGrid binds to the ViewModel which projects from the model.
/// Save syncs ViewModel edits back to the model.
/// </summary>
public class TableClipEditor : IClipEditor
{
    public event EventHandler? BecameDirty;
    public event EventHandler? Saved;

    /// <summary>The TableEditorViewModel bound to the DataGrid.</summary>
    public TableEditorViewModel ViewModel { get; private set; }

    public bool IsDirty { get; private set; }
    public bool IsPartial => false;

    public TableClipEditor(string? xml)
    {
        var table = FmTable.FromXml(xml ?? "");
        ViewModel = new TableEditorViewModel(table);

        SubscribeToViewModel(ViewModel);
    }

    public bool Save()
    {
        ViewModel.SyncToModel();
        IsDirty = false;
        Saved?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public string ToXml()
    {
        return ViewModel.Table.ToXml();
    }

    public void FromXml(string xml)
    {
        var incoming = FmTable.FromXml(xml);
        PatchViewModel(incoming);
        IsDirty = false;
    }

    private void MarkDirty()
    {
        if (!IsDirty)
        {
            IsDirty = true;
            BecameDirty?.Invoke(this, EventArgs.Empty);
        }
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
        if (e.NewItems is not null)
            foreach (FmField field in e.NewItems)
                field.PropertyChanged += OnFieldPropertyChanged;

        if (e.OldItems is not null)
            foreach (FmField field in e.OldItems)
                field.PropertyChanged -= OnFieldPropertyChanged;

        MarkDirty();
    }

    private void OnFieldPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        MarkDirty();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TableEditorViewModel.TableName))
            MarkDirty();
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
                PatchField(existing, inField);

                var currentIdx = current.Fields.IndexOf(existing);
                if (currentIdx != i && currentIdx >= 0 && i < current.Fields.Count)
                    current.Fields.Move(currentIdx, i);
            }
            else
            {
                inField.PropertyChanged += OnFieldPropertyChanged;
                if (i < current.Fields.Count)
                    current.Fields.Insert(i, inField);
                else
                    current.Fields.Add(inField);
            }
        }

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
