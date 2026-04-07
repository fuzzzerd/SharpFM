namespace SharpFM.Model.Schema;

public enum FieldDataType
{
    Text,
    Number,
    Date,
    Time,
    TimeStamp,
    Binary
}

public enum FieldKind
{
    Normal,
    Calculated,
    Summary
}

public enum AutoEnterType
{
    Serial,
    UUID,
    CreationDate,
    ModificationDate,
    CreationTime,
    ModificationTime,
    ConstantData,
    Calculation,
    Lookup
}

public enum SummaryOperation
{
    Sum,
    Average,
    Count,
    Minimum,
    Maximum,
    StdDeviation
}

public enum FieldIndexing
{
    None,
    Minimal,
    All
}
