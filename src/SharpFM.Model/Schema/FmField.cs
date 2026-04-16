using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace SharpFM.Model.Schema;

public class FmField : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private void Notify([CallerMemberName] string name = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private int _id;
    public int Id { get => _id; set { _id = value; Notify(); } }

    private string _name = "";
    public string Name { get => _name; set { _name = value; Notify(); } }

    private FieldDataType _dataType = FieldDataType.Text;
    public FieldDataType DataType { get => _dataType; set { _dataType = value; Notify(); } }

    private FieldKind _kind = FieldKind.Normal;
    public FieldKind Kind { get => _kind; set { _kind = value; Notify(); } }

    private int _repetitions = 1;
    public int Repetitions { get => _repetitions; set { _repetitions = value; Notify(); } }

    private string? _comment;
    public string? Comment { get => _comment; set { _comment = value; Notify(); } }

    // Validation
    private bool _notEmpty;
    public bool NotEmpty { get => _notEmpty; set { _notEmpty = value; Notify(); } }

    private bool _unique;
    public bool Unique { get => _unique; set { _unique = value; Notify(); } }

    private bool _existing;
    public bool Existing { get => _existing; set { _existing = value; Notify(); } }

    private string? _maxDataLength;
    public string? MaxDataLength { get => _maxDataLength; set { _maxDataLength = value; Notify(); } }

    private string? _validationCalculation;
    public string? ValidationCalculation { get => _validationCalculation; set { _validationCalculation = value; Notify(); } }

    private string? _errorMessage;
    public string? ErrorMessage { get => _errorMessage; set { _errorMessage = value; Notify(); } }

    private string? _rangeMin;
    public string? RangeMin { get => _rangeMin; set { _rangeMin = value; Notify(); } }

    private string? _rangeMax;
    public string? RangeMax { get => _rangeMax; set { _rangeMax = value; Notify(); } }

    // Auto-enter
    private AutoEnterType? _autoEnter;
    public AutoEnterType? AutoEnter { get => _autoEnter; set { _autoEnter = value; Notify(); } }

    private bool _allowEditing;
    public bool AllowEditing { get => _allowEditing; set { _allowEditing = value; Notify(); } }

    private string? _autoEnterValue;
    public string? AutoEnterValue { get => _autoEnterValue; set { _autoEnterValue = value; Notify(); } }

    // Calculated fields
    private string? _calculation;
    public string? Calculation { get => _calculation; set { _calculation = value; Notify(); } }

    private bool _alwaysEvaluate;
    public bool AlwaysEvaluate { get => _alwaysEvaluate; set { _alwaysEvaluate = value; Notify(); } }

    private string? _calculationContext;
    public string? CalculationContext { get => _calculationContext; set { _calculationContext = value; Notify(); } }

    // Summary fields
    private SummaryOperation? _summaryOp;
    public SummaryOperation? SummaryOp { get => _summaryOp; set { _summaryOp = value; Notify(); } }

    private string? _summaryTargetField;
    public string? SummaryTargetField { get => _summaryTargetField; set { _summaryTargetField = value; Notify(); } }

    // Storage
    private bool _isGlobal;
    public bool IsGlobal { get => _isGlobal; set { _isGlobal = value; Notify(); } }

    private FieldIndexing _indexing = FieldIndexing.None;
    public FieldIndexing Indexing { get => _indexing; set { _indexing = value; Notify(); } }

    public static FmField FromXml(XElement el)
    {
        var field = new FmField
        {
            Id = int.TryParse(el.Attribute("id")?.Value, out var id) ? id : 0,
            Name = el.Attribute("name")?.Value ?? "",
            DataType = ParseDataType(el.Attribute("dataType")?.Value),
            Kind = ParseFieldKind(el.Attribute("fieldType")?.Value),
            Repetitions = int.TryParse(el.Attribute("maxRepetition")?.Value, out var rep) ? rep : 1,
            Comment = el.Element("Comment")?.Value,
        };

        // Calculation
        var calcEl = el.Element("Calculation");
        if (calcEl != null)
        {
            field.Calculation = calcEl.Value;
            field.AlwaysEvaluate = calcEl.Attribute("alwaysEvaluate")?.Value == "True";
            field.CalculationContext = calcEl.Attribute("table")?.Value;
        }

        // Summary
        var summaryEl = el.Element("SummaryField");
        if (summaryEl != null)
        {
            field.SummaryOp = ParseSummaryOp(summaryEl.Attribute("operation")?.Value);
            field.SummaryTargetField = summaryEl.Element("SummaryField")?.Attribute("name")?.Value;
        }

        // Auto-enter
        var autoEl = el.Element("AutoEnter");
        if (autoEl != null)
        {
            field.AutoEnter = ParseAutoEnterType(autoEl.Attribute("value")?.Value);
            field.AllowEditing = autoEl.Attribute("allowEditing")?.Value == "True";

            var serialEl = autoEl.Element("Serial");
            if (serialEl != null)
                field.AutoEnterValue = serialEl.Attribute("nextSerialNumber")?.Value;

            var constEl = autoEl.Element("ConstantData");
            if (constEl != null)
                field.AutoEnterValue = constEl.Value;

            var autoCalcEl = autoEl.Element("Calculation");
            if (autoCalcEl != null)
                field.AutoEnterValue = autoCalcEl.Value;
        }

        // Validation
        var valEl = el.Element("Validation");
        if (valEl != null)
        {
            field.NotEmpty = valEl.Element("NotEmpty")?.Attribute("value")?.Value == "True";
            field.Unique = valEl.Element("Unique")?.Attribute("value")?.Value == "True";
            field.Existing = valEl.Element("Existing")?.Attribute("value")?.Value == "True";

            var maxLen = valEl.Element("MaxDataLength");
            if (maxLen != null)
                field.MaxDataLength = maxLen.Attribute("length")?.Value;

            var rangeEl = valEl.Element("Range");
            if (rangeEl != null)
            {
                field.RangeMin = rangeEl.Element("MinimumValue")?.Value;
                field.RangeMax = rangeEl.Element("MaximumValue")?.Value;
            }

            field.ValidationCalculation = valEl.Element("StrictValidation")?.Value;
            field.ErrorMessage = valEl.Element("ErrorMessage")?.Value;
        }

        // Storage
        var storageEl = el.Element("Storage");
        if (storageEl != null)
        {
            field.IsGlobal = storageEl.Attribute("global")?.Value == "True";
            field.Indexing = ParseIndexing(storageEl.Attribute("indexed")?.Value);
        }

        return field;
    }

    public XElement ToXml()
    {
        var el = new XElement("Field",
            new XAttribute("id", Id),
            new XAttribute("name", Name),
            new XAttribute("dataType", DataType.ToString()),
            new XAttribute("fieldType", Kind.ToString()));

        if (Repetitions > 1)
            el.Add(new XAttribute("maxRepetition", Repetitions));

        if (!string.IsNullOrEmpty(Comment))
            el.Add(new XElement("Comment", Comment));

        // Calculation
        if (!string.IsNullOrEmpty(Calculation))
        {
            var calcEl = XElement.Parse($"<Calculation><![CDATA[{Calculation}]]></Calculation>");
            if (AlwaysEvaluate)
                calcEl.Add(new XAttribute("alwaysEvaluate", "True"));
            if (!string.IsNullOrEmpty(CalculationContext))
                calcEl.Add(new XAttribute("table", CalculationContext));
            el.Add(calcEl);
        }

        // Summary
        if (SummaryOp.HasValue)
        {
            var summaryEl = new XElement("SummaryField",
                new XAttribute("operation", SummaryOp.Value.ToString()));
            if (!string.IsNullOrEmpty(SummaryTargetField))
                summaryEl.Add(new XElement("SummaryField", new XAttribute("name", SummaryTargetField)));
            el.Add(summaryEl);
        }

        // Auto-enter
        if (AutoEnter.HasValue)
        {
            var autoEl = new XElement("AutoEnter",
                new XAttribute("value", AutoEnterTypeToString(AutoEnter.Value)));
            if (AllowEditing)
                autoEl.Add(new XAttribute("allowEditing", "True"));

            switch (AutoEnter.Value)
            {
                case AutoEnterType.Serial:
                    autoEl.Add(new XElement("Serial",
                        new XAttribute("nextSerialNumber", AutoEnterValue ?? "1")));
                    break;
                case AutoEnterType.ConstantData:
                    autoEl.Add(XElement.Parse($"<ConstantData><![CDATA[{AutoEnterValue ?? ""}]]></ConstantData>"));
                    break;
                case AutoEnterType.Calculation:
                    autoEl.Add(XElement.Parse($"<Calculation><![CDATA[{AutoEnterValue ?? ""}]]></Calculation>"));
                    break;
            }

            el.Add(autoEl);
        }

        // Validation
        if (NotEmpty || Unique || Existing || MaxDataLength != null ||
            ValidationCalculation != null || ErrorMessage != null ||
            RangeMin != null || RangeMax != null)
        {
            var valEl = new XElement("Validation");
            if (NotEmpty) valEl.Add(new XElement("NotEmpty", new XAttribute("value", "True")));
            if (Unique) valEl.Add(new XElement("Unique", new XAttribute("value", "True")));
            if (Existing) valEl.Add(new XElement("Existing", new XAttribute("value", "True")));

            if (MaxDataLength != null)
                valEl.Add(new XElement("MaxDataLength", new XAttribute("length", MaxDataLength)));

            if (RangeMin != null || RangeMax != null)
            {
                var rangeEl = new XElement("Range");
                if (RangeMin != null)
                    rangeEl.Add(XElement.Parse($"<MinimumValue><![CDATA[{RangeMin}]]></MinimumValue>"));
                if (RangeMax != null)
                    rangeEl.Add(XElement.Parse($"<MaximumValue><![CDATA[{RangeMax}]]></MaximumValue>"));
                valEl.Add(rangeEl);
            }

            if (ValidationCalculation != null)
                valEl.Add(XElement.Parse($"<StrictValidation><![CDATA[{ValidationCalculation}]]></StrictValidation>"));
            if (ErrorMessage != null)
                valEl.Add(XElement.Parse($"<ErrorMessage><![CDATA[{ErrorMessage}]]></ErrorMessage>"));

            el.Add(valEl);
        }

        // Storage
        if (IsGlobal || Indexing != FieldIndexing.None)
        {
            var storageEl = new XElement("Storage");
            if (IsGlobal) storageEl.Add(new XAttribute("global", "True"));
            if (Indexing != FieldIndexing.None)
                storageEl.Add(new XAttribute("indexed", Indexing.ToString()));
            el.Add(storageEl);
        }

        return el;
    }

    // --- Parsing helpers ---

    private static FieldDataType ParseDataType(string? value) => value switch
    {
        "Text" => FieldDataType.Text,
        "Number" => FieldDataType.Number,
        "Date" => FieldDataType.Date,
        "Time" => FieldDataType.Time,
        "TimeStamp" => FieldDataType.TimeStamp,
        "Binary" => FieldDataType.Binary,
        _ => FieldDataType.Text
    };

    private static FieldKind ParseFieldKind(string? value) => value switch
    {
        "Normal" => FieldKind.Normal,
        "Calculated" => FieldKind.Calculated,
        "Summary" => FieldKind.Summary,
        _ => FieldKind.Normal
    };

    private static SummaryOperation? ParseSummaryOp(string? value) => value switch
    {
        "Sum" => SummaryOperation.Sum,
        "Average" => SummaryOperation.Average,
        "Count" => SummaryOperation.Count,
        "Minimum" => SummaryOperation.Minimum,
        "Maximum" => SummaryOperation.Maximum,
        "StdDeviation" => SummaryOperation.StdDeviation,
        _ => null
    };

    private static AutoEnterType? ParseAutoEnterType(string? value) => value switch
    {
        "SerialNumber" or "Serial" => AutoEnterType.Serial,
        "UUID" => AutoEnterType.UUID,
        "CreationDate" => AutoEnterType.CreationDate,
        "ModificationDate" => AutoEnterType.ModificationDate,
        "CreationTime" => AutoEnterType.CreationTime,
        "ModificationTime" => AutoEnterType.ModificationTime,
        "ConstantData" => AutoEnterType.ConstantData,
        "Calculation" => AutoEnterType.Calculation,
        "Lookup" => AutoEnterType.Lookup,
        _ => null
    };

    private static string AutoEnterTypeToString(AutoEnterType type) => type switch
    {
        AutoEnterType.Serial => "SerialNumber",
        AutoEnterType.UUID => "UUID",
        AutoEnterType.CreationDate => "CreationDate",
        AutoEnterType.ModificationDate => "ModificationDate",
        AutoEnterType.CreationTime => "CreationTime",
        AutoEnterType.ModificationTime => "ModificationTime",
        AutoEnterType.ConstantData => "ConstantData",
        AutoEnterType.Calculation => "Calculation",
        AutoEnterType.Lookup => "Lookup",
        _ => "ConstantData"
    };

    private static FieldIndexing ParseIndexing(string? value) => value switch
    {
        "Minimal" => FieldIndexing.Minimal,
        "All" => FieldIndexing.All,
        _ => FieldIndexing.None
    };
}
