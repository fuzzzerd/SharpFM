using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SharpFM.Schema.Model;

namespace SharpFM.Schema.Editor;

public class TableEditorViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public FmTable Table { get; }
    public ObservableCollection<FmField> Fields { get; }

    private FmField? _selectedField;
    public FmField? SelectedField
    {
        get => _selectedField;
        set
        {
            _selectedField = value;
            NotifyPropertyChanged();
        }
    }

    private string _tableName;
    public string TableName
    {
        get => _tableName;
        set
        {
            _tableName = value;
            Table.Name = value;
            NotifyPropertyChanged();
        }
    }

    public ICommand AddFieldCommand { get; }
    public ICommand RemoveFieldCommand { get; }

    public TableEditorViewModel(FmTable table)
    {
        Table = table;
        _tableName = table.Name;
        Fields = new ObservableCollection<FmField>(table.Fields);
        AddFieldCommand = new RelayCommand(_ => AddField());
        RemoveFieldCommand = new RelayCommand(_ => RemoveSelectedField(), _ => SelectedField != null);
    }

    public void AddField()
    {
        var field = new FmField
        {
            Id = Fields.Count + 1,
            Name = "NewField",
            DataType = FieldDataType.Text,
            Kind = FieldKind.Normal
        };
        Fields.Add(field);
        Table.AddField(field);
        SelectedField = field;
    }

    public void RemoveSelectedField()
    {
        if (SelectedField == null) return;
        var field = SelectedField;
        Fields.Remove(field);
        Table.RemoveField(field);
        SelectedField = null;
    }

    /// <summary>
    /// Sync the ObservableCollection back to the model (in case of reordering etc.)
    /// </summary>
    public void SyncToModel()
    {
        Table.Fields.Clear();
        foreach (var f in Fields)
            Table.Fields.Add(f);
    }
}
