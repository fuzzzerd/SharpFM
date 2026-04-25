using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SharpFM.Model.Scripting.Calc;

/// <summary>
/// Single source of truth for FileMaker calculation built-ins. The TextMate
/// grammar (<c>fmcalc.tmLanguage.json</c>) is regenerated from this catalog
/// by <c>SharpFM.Tools.GenerateGrammar</c>; the completion provider reads
/// from the same lists at runtime. Edit here, regenerate grammar, both stay
/// in sync.
///
/// Categorisation follows FileMaker's calculation dialog. Function lists
/// are based on the public function reference at help.claris.com and may
/// trail very recent FM releases; bumps are a one-line append.
/// </summary>
public static class FmCalcCatalog
{
    public static IReadOnlyList<FmCalcFunction> Functions { get; } = BuildFunctions();
    public static IReadOnlyList<FmCalcControlForm> ControlForms { get; } = BuildControlForms();
    public static IReadOnlyList<string> Constants { get; } = new ReadOnlyCollection<string>(
        new[] { "True", "False", "Pi" });

    /// <summary>Word operators that must scope as keyword.operator.word, not function names.</summary>
    public static IReadOnlyList<string> WordOperators { get; } = new ReadOnlyCollection<string>(
        new[] { "and", "or", "not", "xor" });

    private static IReadOnlyList<FmCalcFunction> BuildFunctions()
    {
        var list = new List<FmCalcFunction>();

        void Add(string n, FunctionCategory c, string sig, string desc) =>
            list.Add(new FmCalcFunction(n, c, sig, desc));

        // Text
        Add("Char", FunctionCategory.Text, "Char(number)", "Returns the character for a Unicode code point.");
        Add("Code", FunctionCategory.Text, "Code(text)", "Returns the Unicode code points for the characters in text.");
        Add("Exact", FunctionCategory.Text, "Exact(originalText; comparisonText)", "Returns true if the two values match exactly.");
        Add("Filter", FunctionCategory.Text, "Filter(textToFilter; filterText)", "Returns characters from textToFilter that also appear in filterText.");
        Add("FilterValues", FunctionCategory.Text, "FilterValues(textToFilter; filterValues)", "Returns values from textToFilter that match a value list.");
        Add("GetAsCSS", FunctionCategory.Text, "GetAsCSS(text)", "Returns text marked up with CSS style attributes.");
        Add("GetAsDate", FunctionCategory.Text, "GetAsDate(text)", "Converts text to a date.");
        Add("GetAsNumber", FunctionCategory.Text, "GetAsNumber(text)", "Returns only the numeric characters from text.");
        Add("GetAsSVG", FunctionCategory.Text, "GetAsSVG(text)", "Returns text marked up as SVG.");
        Add("GetAsText", FunctionCategory.Text, "GetAsText(data)", "Converts a value to text.");
        Add("GetAsTime", FunctionCategory.Text, "GetAsTime(text)", "Converts text to a time.");
        Add("GetAsTimestamp", FunctionCategory.Text, "GetAsTimestamp(text)", "Converts text to a timestamp.");
        Add("GetAsURLEncoded", FunctionCategory.Text, "GetAsURLEncoded(text)", "Returns the URL-encoded form of text.");
        Add("Hiragana", FunctionCategory.Text, "Hiragana(text)", "Converts katakana to hiragana.");
        Add("KanaHankaku", FunctionCategory.Text, "KanaHankaku(text)", "Converts full-width katakana to half-width.");
        Add("KanaZenkaku", FunctionCategory.Text, "KanaZenkaku(text)", "Converts half-width katakana to full-width.");
        Add("KanjiNumeral", FunctionCategory.Text, "KanjiNumeral(text)", "Converts Arabic numerals to kanji.");
        Add("KatakanaToRoman", FunctionCategory.Text, "KatakanaToRoman(text)", "Converts katakana to roman characters.");
        Add("Left", FunctionCategory.Text, "Left(text; numberOfCharacters)", "Returns the leftmost characters of text.");
        Add("LeftValues", FunctionCategory.Text, "LeftValues(text; numberOfValues)", "Returns the leftmost values from a return-delimited list.");
        Add("LeftWords", FunctionCategory.Text, "LeftWords(text; numberOfWords)", "Returns the leftmost words from text.");
        Add("Length", FunctionCategory.Text, "Length(text)", "Returns the number of characters in text.");
        Add("Lower", FunctionCategory.Text, "Lower(text)", "Returns text in lowercase.");
        Add("Middle", FunctionCategory.Text, "Middle(text; start; numberOfCharacters)", "Returns characters from text starting at start.");
        Add("MiddleValues", FunctionCategory.Text, "MiddleValues(text; startingValue; numberOfValues)", "Returns values from a list starting at startingValue.");
        Add("MiddleWords", FunctionCategory.Text, "MiddleWords(text; startingWord; numberOfWords)", "Returns words from text starting at startingWord.");
        Add("NumToJText", FunctionCategory.Text, "NumToJText(number; separator; characterType)", "Converts Arabic numerals to Japanese text.");
        Add("PatternCount", FunctionCategory.Text, "PatternCount(text; searchString)", "Returns how many times searchString appears in text.");
        Add("Position", FunctionCategory.Text, "Position(text; searchString; start; occurrence)", "Returns the starting position of searchString in text.");
        Add("Proper", FunctionCategory.Text, "Proper(text)", "Returns text with the first letter of each word capitalized.");
        Add("Quote", FunctionCategory.Text, "Quote(text)", "Returns text wrapped in quotes with internal quotes escaped.");
        Add("Replace", FunctionCategory.Text, "Replace(text; start; numberOfCharacters; replacementText)", "Replaces a range of characters in text.");
        Add("Right", FunctionCategory.Text, "Right(text; numberOfCharacters)", "Returns the rightmost characters of text.");
        Add("RightValues", FunctionCategory.Text, "RightValues(text; numberOfValues)", "Returns the rightmost values from a return-delimited list.");
        Add("RightWords", FunctionCategory.Text, "RightWords(text; numberOfWords)", "Returns the rightmost words from text.");
        Add("RomanHankaku", FunctionCategory.Text, "RomanHankaku(text)", "Converts full-width roman to half-width.");
        Add("RomanZenkaku", FunctionCategory.Text, "RomanZenkaku(text)", "Converts half-width roman to full-width.");
        Add("SerialIncrement", FunctionCategory.Text, "SerialIncrement(text; incrementBy)", "Returns text with its trailing digits incremented.");
        Add("SortValues", FunctionCategory.Text, "SortValues(values; dataType; locale)", "Returns a sorted list of values.");
        Add("Substitute", FunctionCategory.Text, "Substitute(text; searchString; replaceString)", "Replaces every occurrence of searchString in text.");
        Add("Trim", FunctionCategory.Text, "Trim(text)", "Removes leading and trailing spaces from text.");
        Add("TrimAll", FunctionCategory.Text, "TrimAll(text; trimSpaces; trimType)", "Removes spaces with finer control than Trim.");
        Add("UniqueValues", FunctionCategory.Text, "UniqueValues(values; fieldType; locale)", "Returns the unique values from a list.");
        Add("Upper", FunctionCategory.Text, "Upper(text)", "Returns text in uppercase.");
        Add("ValueCount", FunctionCategory.Text, "ValueCount(text)", "Returns the number of values in a return-delimited list.");
        Add("VerifyID", FunctionCategory.Text, "VerifyID(id)", "Returns whether an ID has a valid checksum.");
        Add("WordCount", FunctionCategory.Text, "WordCount(text)", "Returns the number of words in text.");

        // Text formatting
        Add("RGB", FunctionCategory.TextFormatting, "RGB(red; green; blue)", "Returns a numeric color value.");
        Add("TextColor", FunctionCategory.TextFormatting, "TextColor(text; rgb)", "Returns text with the given color applied.");
        Add("TextColorRemove", FunctionCategory.TextFormatting, "TextColorRemove(text; rgb)", "Removes color from text.");
        Add("TextFont", FunctionCategory.TextFormatting, "TextFont(text; fontName; fontScript)", "Returns text with the given font applied.");
        Add("TextFontRemove", FunctionCategory.TextFormatting, "TextFontRemove(text; fontName; fontScript)", "Removes font from text.");
        Add("TextFormatRemove", FunctionCategory.TextFormatting, "TextFormatRemove(text)", "Removes all formatting from text.");
        Add("TextSize", FunctionCategory.TextFormatting, "TextSize(text; size)", "Returns text at the given size.");
        Add("TextSizeRemove", FunctionCategory.TextFormatting, "TextSizeRemove(text; size)", "Removes size from text.");
        Add("TextStyleAdd", FunctionCategory.TextFormatting, "TextStyleAdd(text; style)", "Returns text with the given style applied.");
        Add("TextStyleRemove", FunctionCategory.TextFormatting, "TextStyleRemove(text; style)", "Removes style from text.");

        // Number
        Add("Abs", FunctionCategory.Number, "Abs(number)", "Returns the absolute value of number.");
        Add("Ceiling", FunctionCategory.Number, "Ceiling(number)", "Rounds number up to the next integer.");
        Add("Combination", FunctionCategory.Number, "Combination(setSize; numberOfChoices)", "Returns the number of combinations.");
        Add("Div", FunctionCategory.Number, "Div(number; divisor)", "Returns the integer part of number divided by divisor.");
        Add("Exp", FunctionCategory.Number, "Exp(number)", "Returns e raised to the power of number.");
        Add("Factorial", FunctionCategory.Number, "Factorial(number; numberOfFactors)", "Returns the factorial of number.");
        Add("Floor", FunctionCategory.Number, "Floor(number)", "Rounds number down to the previous integer.");
        Add("Int", FunctionCategory.Number, "Int(number)", "Returns the integer part of number.");
        Add("Lg", FunctionCategory.Number, "Lg(number)", "Returns the base-2 logarithm of number.");
        Add("Ln", FunctionCategory.Number, "Ln(number)", "Returns the natural logarithm of number.");
        Add("Log", FunctionCategory.Number, "Log(number)", "Returns the base-10 logarithm of number.");
        Add("Mod", FunctionCategory.Number, "Mod(number; divisor)", "Returns the remainder of number divided by divisor.");
        Add("Random", FunctionCategory.Number, "Random", "Returns a random number between 0 and 1.");
        Add("Round", FunctionCategory.Number, "Round(number; precision)", "Rounds number to precision decimal places.");
        Add("SetPrecision", FunctionCategory.Number, "SetPrecision(expression; precision)", "Returns expression evaluated with extended precision.");
        Add("Sign", FunctionCategory.Number, "Sign(number)", "Returns -1, 0, or 1 depending on the sign of number.");
        Add("Sqrt", FunctionCategory.Number, "Sqrt(number)", "Returns the square root of number.");
        Add("Truncate", FunctionCategory.Number, "Truncate(number; precision)", "Truncates number to precision decimal places.");

        // Date
        Add("Date", FunctionCategory.Date, "Date(month; day; year)", "Returns a date value.");
        Add("Day", FunctionCategory.Date, "Day(date)", "Returns the day of the month from date.");
        Add("DayName", FunctionCategory.Date, "DayName(date)", "Returns the weekday name for date.");
        Add("DayNameJ", FunctionCategory.Date, "DayNameJ(date)", "Returns the Japanese weekday name for date.");
        Add("DayOfWeek", FunctionCategory.Date, "DayOfWeek(date)", "Returns the day of the week (1-7) for date.");
        Add("DayOfYear", FunctionCategory.Date, "DayOfYear(date)", "Returns the day of the year (1-366) for date.");
        Add("Month", FunctionCategory.Date, "Month(date)", "Returns the month number for date.");
        Add("MonthName", FunctionCategory.Date, "MonthName(date)", "Returns the month name for date.");
        Add("MonthNameJ", FunctionCategory.Date, "MonthNameJ(date)", "Returns the Japanese month name for date.");
        Add("WeekOfYear", FunctionCategory.Date, "WeekOfYear(date)", "Returns the week number of the year for date.");
        Add("WeekOfYearFiscal", FunctionCategory.Date, "WeekOfYearFiscal(date; startingDay)", "Returns the fiscal week of year for date.");
        Add("Year", FunctionCategory.Date, "Year(date)", "Returns the year for date.");
        Add("YearName", FunctionCategory.Date, "YearName(date; format)", "Returns the Japanese era year name for date.");

        // Time
        Add("Hour", FunctionCategory.Time, "Hour(time)", "Returns the hour for time.");
        Add("Minute", FunctionCategory.Time, "Minute(time)", "Returns the minute for time.");
        Add("Seconds", FunctionCategory.Time, "Seconds(time)", "Returns the seconds for time.");
        Add("Time", FunctionCategory.Time, "Time(hours; minutes; seconds)", "Returns a time value.");
        Add("Timestamp", FunctionCategory.Time, "Timestamp(date; time)", "Returns a timestamp value.");

        // Aggregate
        Add("Average", FunctionCategory.Aggregate, "Average(field {; field...})", "Returns the average of non-blank values.");
        Add("Count", FunctionCategory.Aggregate, "Count(field {; field...})", "Returns the count of non-blank values.");
        Add("List", FunctionCategory.Aggregate, "List(field {; field...})", "Returns a return-delimited list of non-blank values.");
        Add("Max", FunctionCategory.Aggregate, "Max(field {; field...})", "Returns the maximum of non-blank values.");
        Add("Min", FunctionCategory.Aggregate, "Min(field {; field...})", "Returns the minimum of non-blank values.");
        Add("StDev", FunctionCategory.Aggregate, "StDev(field {; field...})", "Returns the sample standard deviation.");
        Add("StDevP", FunctionCategory.Aggregate, "StDevP(field {; field...})", "Returns the population standard deviation.");
        Add("Sum", FunctionCategory.Aggregate, "Sum(field {; field...})", "Returns the sum of non-blank values.");
        Add("Variance", FunctionCategory.Aggregate, "Variance(field {; field...})", "Returns the sample variance.");
        Add("VarianceP", FunctionCategory.Aggregate, "VarianceP(field {; field...})", "Returns the population variance.");

        // Summary / repeating
        Add("GetSummary", FunctionCategory.Summary, "GetSummary(summaryField; breakField)", "Returns a summary value broken on breakField.");
        Add("GetNthRecord", FunctionCategory.Summary, "GetNthRecord(field; recordNumber)", "Returns field's value from the Nth record.");
        Add("Last", FunctionCategory.Summary, "Last(field)", "Returns the last non-blank value in a related set.");
        Add("GetRepetition", FunctionCategory.Summary, "GetRepetition(repeatingField; number)", "Returns the Nth repetition of a repeating field.");
        Add("Extend", FunctionCategory.Summary, "Extend(nonRepeatingField)", "Allows a non-repeating field to apply to all repetitions.");

        // Financial
        Add("FV", FunctionCategory.Financial, "FV(payment; interestRate; periods)", "Returns future value.");
        Add("NPV", FunctionCategory.Financial, "NPV(payment; interestRate)", "Returns net present value.");
        Add("PMT", FunctionCategory.Financial, "PMT(principal; interestRate; term)", "Returns payment amount.");
        Add("PV", FunctionCategory.Financial, "PV(payment; interestRate; periods)", "Returns present value.");

        // Trigonometric
        Add("Acos", FunctionCategory.Trigonometric, "Acos(number)", "Returns the arc cosine of number.");
        Add("Asin", FunctionCategory.Trigonometric, "Asin(number)", "Returns the arc sine of number.");
        Add("Atan", FunctionCategory.Trigonometric, "Atan(number)", "Returns the arc tangent of number.");
        Add("Cos", FunctionCategory.Trigonometric, "Cos(angleInRadians)", "Returns the cosine of angle.");
        Add("Degrees", FunctionCategory.Trigonometric, "Degrees(angleInRadians)", "Converts radians to degrees.");
        Add("Radians", FunctionCategory.Trigonometric, "Radians(angleInDegrees)", "Converts degrees to radians.");
        Add("Sin", FunctionCategory.Trigonometric, "Sin(angleInRadians)", "Returns the sine of angle.");
        Add("Tan", FunctionCategory.Trigonometric, "Tan(angleInRadians)", "Returns the tangent of angle.");

        // Logical
        Add("Evaluate", FunctionCategory.Logical, "Evaluate(expression {; [fields]})", "Evaluates an expression provided as text.");
        Add("EvaluationError", FunctionCategory.Logical, "EvaluationError(expression)", "Returns the error number from an evaluation.");
        Add("GetAsBoolean", FunctionCategory.Logical, "GetAsBoolean(data)", "Returns 0 if data is empty/zero, otherwise 1.");
        Add("GetField", FunctionCategory.Logical, "GetField(fieldName)", "Returns the value of the field whose name is fieldName.");
        Add("GetFieldName", FunctionCategory.Logical, "GetFieldName(field)", "Returns the fully qualified name of field.");
        Add("IsEmpty", FunctionCategory.Logical, "IsEmpty(expression)", "Returns true if expression is empty.");
        Add("IsValid", FunctionCategory.Logical, "IsValid(field)", "Returns true if field is valid and references a real field.");
        Add("IsValidExpression", FunctionCategory.Logical, "IsValidExpression(expression)", "Returns true if expression is syntactically valid.");
        Add("Lookup", FunctionCategory.Logical, "Lookup(sourceField {; failExpression})", "Returns a looked-up value through a relationship.");
        Add("LookupNext", FunctionCategory.Logical, "LookupNext(sourceField; lower-/higher-flag)", "Returns the next looked-up value.");
        Add("Self", FunctionCategory.Logical, "Self", "Refers to the object whose property is being evaluated.");
        Add("SetField", FunctionCategory.Logical, "SetField(fieldName; value)", "Sets the field whose name is fieldName.");

        // Get
        Add("Get", FunctionCategory.Get, "Get(parameter)", "Returns information about the FileMaker environment.");

        // Container
        Add("Base64Decode", FunctionCategory.Container, "Base64Decode(text {; fileNameWithExtension})", "Decodes base64 to container or text.");
        Add("Base64Encode", FunctionCategory.Container, "Base64Encode(data)", "Encodes data as base64.");
        Add("Base64EncodeRFC", FunctionCategory.Container, "Base64EncodeRFC(rfcNumber; data)", "Encodes data as base64 per the given RFC.");
        Add("CryptAuthCode", FunctionCategory.Container, "CryptAuthCode(data; algorithm; key)", "Returns an HMAC authentication code.");
        Add("CryptDecrypt", FunctionCategory.Container, "CryptDecrypt(data; key)", "Decrypts data with key.");
        Add("CryptDecryptBase64", FunctionCategory.Container, "CryptDecryptBase64(text; key)", "Decrypts base64-encoded data with key.");
        Add("CryptDigest", FunctionCategory.Container, "CryptDigest(data; algorithm)", "Returns a cryptographic digest of data.");
        Add("CryptEncrypt", FunctionCategory.Container, "CryptEncrypt(data; key)", "Encrypts data with key.");
        Add("CryptEncryptBase64", FunctionCategory.Container, "CryptEncryptBase64(data; key)", "Encrypts data with key and returns base64.");
        Add("CryptGenerateSignature", FunctionCategory.Container, "CryptGenerateSignature(data; algorithm; privateRSAKey; password)", "Generates an RSA signature.");
        Add("CryptVerifySignature", FunctionCategory.Container, "CryptVerifySignature(data; algorithm; publicRSAKey; signature)", "Verifies an RSA signature.");
        Add("GetContainerAttribute", FunctionCategory.Container, "GetContainerAttribute(field; attribute)", "Returns metadata about a container value.");
        Add("GetHeight", FunctionCategory.Container, "GetHeight(field)", "Returns the height of an image container.");
        Add("GetThumbnail", FunctionCategory.Container, "GetThumbnail(field; fitToWidth; fitToHeight)", "Returns a thumbnail of a container.");
        Add("GetWidth", FunctionCategory.Container, "GetWidth(field)", "Returns the width of an image container.");
        Add("HexDecode", FunctionCategory.Container, "HexDecode(text {; fileNameWithExtension})", "Decodes hex to container or text.");
        Add("HexEncode", FunctionCategory.Container, "HexEncode(data)", "Encodes data as hex.");
        Add("VerifyContainer", FunctionCategory.Container, "VerifyContainer(field)", "Returns whether a container's checksum verifies.");

        // JSON
        Add("JSONDeleteElement", FunctionCategory.Json, "JSONDeleteElement(json; keyOrIndexOrPath)", "Deletes an element at the given path.");
        Add("JSONFormatElements", FunctionCategory.Json, "JSONFormatElements(json)", "Returns json formatted with indentation.");
        Add("JSONGetElement", FunctionCategory.Json, "JSONGetElement(json; keyOrIndexOrPath)", "Returns an element at the given path.");
        Add("JSONListKeys", FunctionCategory.Json, "JSONListKeys(json; keyOrIndexOrPath)", "Returns the keys of an object element.");
        Add("JSONListValues", FunctionCategory.Json, "JSONListValues(json; keyOrIndexOrPath)", "Returns the values of an object/array element.");
        Add("JSONSetElement", FunctionCategory.Json, "JSONSetElement(json; keyOrIndexOrPath; value; type)", "Sets an element at the given path.");

        // SQL
        Add("ExecuteSQL", FunctionCategory.Sql, "ExecuteSQL(sql; fieldSeparator; rowSeparator {; arguments...})", "Executes an SQL query against the open database.");

        // External
        Add("GetSensor", FunctionCategory.External, "GetSensor(sensorType {; options})", "Returns a value from a device sensor (FileMaker Go).");
        Add("GetLiveRemoteCallResult", FunctionCategory.External, "GetLiveRemoteCallResult(callID)", "Returns the result of a live remote call.");
        Add("GetLiveRemoteCallStatus", FunctionCategory.External, "GetLiveRemoteCallStatus(callID)", "Returns the status of a live remote call.");

        // Design
        Add("DatabaseNames", FunctionCategory.Design, "DatabaseNames", "Returns names of open databases.");
        Add("FieldBounds", FunctionCategory.Design, "FieldBounds(fileName; layoutName; fieldName)", "Returns layout coordinates of a field.");
        Add("FieldComment", FunctionCategory.Design, "FieldComment(fileName; fieldName)", "Returns the comment for a field.");
        Add("FieldIDs", FunctionCategory.Design, "FieldIDs(fileName; layoutName)", "Returns field IDs.");
        Add("FieldNames", FunctionCategory.Design, "FieldNames(fileName; layoutName)", "Returns field names.");
        Add("FieldRepetitions", FunctionCategory.Design, "FieldRepetitions(fileName; layoutName; fieldName)", "Returns repetitions for a field.");
        Add("FieldStyle", FunctionCategory.Design, "FieldStyle(fileName; layoutName; fieldName)", "Returns the style applied to a field.");
        Add("FieldType", FunctionCategory.Design, "FieldType(fileName; fieldName)", "Returns the type of a field.");
        Add("GetNextSerialValue", FunctionCategory.Design, "GetNextSerialValue(fileName; fieldName)", "Returns the next auto-enter serial value.");
        Add("LayoutIDs", FunctionCategory.Design, "LayoutIDs(fileName)", "Returns layout IDs.");
        Add("LayoutNames", FunctionCategory.Design, "LayoutNames(fileName)", "Returns layout names.");
        Add("LayoutObjectNames", FunctionCategory.Design, "LayoutObjectNames(fileName; layoutName)", "Returns named objects on a layout.");
        Add("RelationInfo", FunctionCategory.Design, "RelationInfo(fileName; tableOccurrenceName)", "Returns information about relationships.");
        Add("ScriptIDs", FunctionCategory.Design, "ScriptIDs(fileName)", "Returns script IDs.");
        Add("ScriptNames", FunctionCategory.Design, "ScriptNames(fileName)", "Returns script names.");
        Add("TableIDs", FunctionCategory.Design, "TableIDs(fileName)", "Returns table occurrence IDs.");
        Add("TableNames", FunctionCategory.Design, "TableNames(fileName)", "Returns table occurrence names.");
        Add("ValueListIDs", FunctionCategory.Design, "ValueListIDs(fileName)", "Returns value list IDs.");
        Add("ValueListItems", FunctionCategory.Design, "ValueListItems(fileName; valueListName)", "Returns the items of a value list.");
        Add("ValueListNames", FunctionCategory.Design, "ValueListNames(fileName)", "Returns value list names.");
        Add("WindowNames", FunctionCategory.Design, "WindowNames({fileName})", "Returns names of open windows.");

        return new ReadOnlyCollection<FmCalcFunction>(list);
    }

    private static IReadOnlyList<FmCalcControlForm> BuildControlForms()
    {
        return new ReadOnlyCollection<FmCalcControlForm>(new[]
        {
            new FmCalcControlForm(
                "Let",
                "Let([var = expr; ...]; result)",
                "Binds variables and evaluates result with them in scope.",
                "Let ( [ ${1:var} = ${2:value} ] ; ${3:result} )"),
            new FmCalcControlForm(
                "Case",
                "Case(test1; result1 {; test2; result2 ...} {; defaultResult})",
                "Returns result for the first true test; otherwise default.",
                "Case ( ${1:test} ; ${2:result} ; ${3:default} )"),
            new FmCalcControlForm(
                "If",
                "If(test; resultIfTrue; resultIfFalse)",
                "Branches between two results based on test.",
                "If ( ${1:test} ; ${2:trueResult} ; ${3:falseResult} )"),
            new FmCalcControlForm(
                "While",
                "While([initialVars]; condition; [updateVars]; result)",
                "Iterates while condition is true and returns result.",
                "While ( [ ${1:counter} = 0 ] ; ${2:condition} ; [ ${3:counter} = counter + 1 ] ; ${4:result} )"),
            new FmCalcControlForm(
                "Choose",
                "Choose(test; result0 {; result1 ...})",
                "Returns the Nth result based on test (0-indexed).",
                "Choose ( ${1:test} ; ${2:result0} ; ${3:result1} )"),
        });
    }
}
