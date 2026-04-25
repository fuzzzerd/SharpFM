using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SharpFM.Model.Schema;

namespace SharpFM.Schema.Editor;

public class TableEditorViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public FmTable Table { get; }
    public ObservableCollection<FmField> Fields => Table.Fields;

    private FmField? _selectedField;
    public FmField? SelectedField
    {
        get => _selectedField;
        set
        {
            _selectedField = value;
            NotifyPropertyChanged();
            (RemoveFieldCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (EditCalculationCommand as RelayCommand)?.RaiseCanExecuteChanged();
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
    public ICommand EditCalculationCommand { get; }

    public TableEditorViewModel(FmTable table)
    {
        Table = table;
        _tableName = table.Name;
        AddFieldCommand = new RelayCommand(_ => AddField());
        RemoveFieldCommand = new RelayCommand(_ => RemoveSelectedField(), _ => SelectedField != null);
        EditCalculationCommand = new RelayCommand(_ => OpenCalculationEditor(),
            _ => SelectedField?.Kind is FieldKind.Calculated or FieldKind.Summary);
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
        Table.AddField(field);
        SelectedField = field;
    }

    public void RemoveSelectedField()
    {
        if (SelectedField == null) return;
        Table.RemoveField(SelectedField);
        SelectedField = null;
    }

    public void OpenCalculationEditor()
    {
        if (SelectedField == null) return;
        var window = new CalculationEditorWindow(SelectedField, Table);
        window.ShowDialog(Avalonia.Application.Current?.ApplicationLifetime
            is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow! : null!);
    }

}
