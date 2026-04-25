using System;
using System.Linq;
using System.Text.Json.Nodes;

namespace SharpFM.Model.Scripting.Calc;

/// <summary>
/// Builds the <c>source.fmcalc</c> TextMate grammar JSON from
/// <see cref="FmCalcCatalog"/>. Called at runtime by the registry — there
/// is no committed grammar file. The catalog is the single source of truth;
/// the completion provider and the grammar both read from it.
/// </summary>
public static class FmCalcGrammarBuilder
{
    public static string Build()
    {
        var root = new JsonObject
        {
            ["name"] = "FileMaker Calculation",
            ["scopeName"] = "source.fmcalc",
            ["fileTypes"] = new JsonArray("fmcalc"),
            ["patterns"] = new JsonArray(Include("#expression")),
            ["repository"] = BuildRepository(),
        };

        var options = new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };
        return root.ToJsonString(options);
    }

    private static JsonObject BuildRepository() => new()
    {
        ["expression"] = new JsonObject
        {
            ["patterns"] = new JsonArray(
                Include("#comment-block"),
                Include("#comment-line"),
                Include("#string"),
                Include("#number"),
                Include("#constant"),
                Include("#control-form"),
                Include("#operator-word"),
                Include("#variable"),
                Include("#field-reference"),
                Include("#builtin-function"),
                Include("#custom-function"),
                Include("#operator-symbol"),
                Include("#punctuation")),
        },
        ["comment-block"] = new JsonObject
        {
            ["name"] = "comment.block.fmcalc",
            ["begin"] = @"/\*",
            ["beginCaptures"] = NamedCapture("0", "punctuation.definition.comment.fmcalc"),
            ["end"] = @"\*/",
            ["endCaptures"] = NamedCapture("0", "punctuation.definition.comment.fmcalc"),
        },
        ["comment-line"] = new JsonObject
        {
            ["name"] = "comment.line.double-slash.fmcalc",
            ["begin"] = "//",
            ["beginCaptures"] = NamedCapture("0", "punctuation.definition.comment.fmcalc"),
            ["end"] = "$",
        },
        ["string"] = new JsonObject
        {
            ["name"] = "string.quoted.double.fmcalc",
            ["begin"] = "\"",
            ["beginCaptures"] = NamedCapture("0", "punctuation.definition.string.begin.fmcalc"),
            ["end"] = "\"",
            ["endCaptures"] = NamedCapture("0", "punctuation.definition.string.end.fmcalc"),
            ["patterns"] = new JsonArray(new JsonObject
            {
                ["name"] = "constant.character.escape.fmcalc",
                ["match"] = @"\\(""|\\|n|r|t)",
            }),
        },
        ["number"] = new JsonObject
        {
            ["name"] = "constant.numeric.fmcalc",
            ["match"] = @"\b\d+(\.\d+)?([eE][+-]?\d+)?\b",
        },
        ["constant"] = new JsonObject
        {
            ["name"] = "constant.language.fmcalc",
            ["match"] = $@"\b({string.Join("|", FmCalcCatalog.Constants)})\b",
        },
        ["control-form"] = new JsonObject
        {
            ["name"] = "keyword.control.fmcalc",
            ["match"] = $@"\b({string.Join("|", FmCalcCatalog.ControlForms.Select(c => c.Name))})(?=\s*\()",
        },
        ["operator-word"] = new JsonObject
        {
            ["name"] = "keyword.operator.word.fmcalc",
            ["match"] = $@"\b({string.Join("|", FmCalcCatalog.WordOperators)})\b",
        },
        ["variable"] = new JsonObject
        {
            ["name"] = "variable.other.fmcalc",
            ["match"] = @"\${1,2}[A-Za-z_][A-Za-z0-9_.]*",
        },
        ["field-reference"] = new JsonObject
        {
            ["match"] = @"\b([A-Za-z_][A-Za-z0-9_ ]*?)(::)([A-Za-z_][A-Za-z0-9_]*)",
            ["captures"] = new JsonObject
            {
                ["1"] = ScopeNode("entity.name.type.fmcalc"),
                ["2"] = ScopeNode("punctuation.separator.field.fmcalc"),
                ["3"] = ScopeNode("variable.other.member.fmcalc"),
            },
        },
        ["builtin-function"] = BuildBuiltinFunctions(),
        ["custom-function"] = new JsonObject
        {
            ["match"] = @"\b([A-Za-z_][A-Za-z0-9_]*)\s*(?=\()",
            ["captures"] = new JsonObject
            {
                ["1"] = ScopeNode("entity.name.function.fmcalc"),
            },
        },
        ["operator-symbol"] = new JsonObject
        {
            ["name"] = "keyword.operator.fmcalc",
            ["match"] = @"(\^|\*|/|\+|-|&|=|≠|<>|≤|<=|≥|>=|<|>)",
        },
        ["punctuation"] = new JsonObject
        {
            ["patterns"] = new JsonArray(
                new JsonObject { ["match"] = @"\(", ["name"] = "punctuation.section.parens.begin.fmcalc" },
                new JsonObject { ["match"] = @"\)", ["name"] = "punctuation.section.parens.end.fmcalc" },
                new JsonObject { ["match"] = ";", ["name"] = "punctuation.separator.fmcalc" },
                new JsonObject { ["match"] = ",", ["name"] = "punctuation.separator.fmcalc" },
                new JsonObject { ["match"] = @"\[", ["name"] = "punctuation.section.brackets.begin.fmcalc" },
                new JsonObject { ["match"] = @"\]", ["name"] = "punctuation.section.brackets.end.fmcalc" }),
        },
    };

    private static JsonObject BuildBuiltinFunctions()
    {
        var patterns = new JsonArray();

        // One alternation per category so each can carry its own
        // support.function.<category>.fmcalc scope. Within a category,
        // sort length-desc then alphabetical — the standard TextMate idiom
        // that ensures longer alternatives win when one name prefixes another.
        var groups = FmCalcCatalog.Functions
            .GroupBy(f => f.Category)
            .OrderBy(g => (int)g.Key);

        foreach (var group in groups)
        {
            var names = group
                .Select(f => f.Name)
                .OrderByDescending(n => n.Length)
                .ThenBy(n => n, StringComparer.Ordinal)
                .ToList();

            patterns.Add(new JsonObject
            {
                ["match"] = $@"\b({string.Join("|", names)})\s*(?=\()",
                ["captures"] = new JsonObject
                {
                    ["1"] = ScopeNode($"support.function.{ScopeSegment(group.Key)}.fmcalc"),
                },
            });
        }

        return new JsonObject { ["patterns"] = patterns };
    }

    /// <summary>
    /// Map a category enum to its TextMate scope segment. Hyphenated for
    /// multi-word categories, matching VS Code grammar conventions.
    /// </summary>
    public static string ScopeSegment(FunctionCategory category) => category switch
    {
        FunctionCategory.Text => "text",
        FunctionCategory.TextFormatting => "text-formatting",
        FunctionCategory.Number => "number",
        FunctionCategory.Date => "date",
        FunctionCategory.Time => "time",
        FunctionCategory.Aggregate => "aggregate",
        FunctionCategory.Summary => "summary",
        FunctionCategory.Financial => "financial",
        FunctionCategory.Trigonometric => "trigonometric",
        FunctionCategory.Logical => "logical",
        FunctionCategory.Get => "get",
        FunctionCategory.Container => "container",
        FunctionCategory.Json => "json",
        FunctionCategory.Sql => "sql",
        FunctionCategory.External => "external",
        FunctionCategory.Design => "design",
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, null),
    };

    private static JsonObject Include(string reference) => new() { ["include"] = reference };
    private static JsonObject ScopeNode(string name) => new() { ["name"] = name };

    private static JsonObject NamedCapture(string group, string scope) => new()
    {
        [group] = ScopeNode(scope),
    };
}
