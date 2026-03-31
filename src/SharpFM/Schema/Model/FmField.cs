using System;
using System.Xml.Linq;

namespace SharpFM.Schema.Model;

public class FmField
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public FieldDataType DataType { get; set; } = FieldDataType.Text;
    public FieldKind Kind { get; set; } = FieldKind.Normal;
    public int Repetitions { get; set; } = 1;
    public string? Comment { get; set; }

    // Validation
    public bool NotEmpty { get; set; }
    public bool Unique { get; set; }
    public bool Existing { get; set; }
    public string? MaxDataLength { get; set; }
    public string? ValidationCalculation { get; set; }
    public string? ErrorMessage { get; set; }
    public string? RangeMin { get; set; }
    public string? RangeMax { get; set; }

    // Auto-enter
    public AutoEnterType? AutoEnter { get; set; }
    public bool AllowEditing { get; set; }
    public string? AutoEnterValue { get; set; }

    // Calculated fields
    public string? Calculation { get; set; }
    public bool AlwaysEvaluate { get; set; }
    public string? CalculationContext { get; set; }

    // Summary fields
    public SummaryOperation? SummaryOp { get; set; }
    public string? SummaryTargetField { get; set; }

    // Storage
    public bool IsGlobal { get; set; }
    public FieldIndexing Indexing { get; set; } = FieldIndexing.None;

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
