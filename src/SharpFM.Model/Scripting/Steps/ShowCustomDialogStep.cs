using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Typed domain representation of FileMaker's "Show Custom Dialog" step.
/// <para>
/// FM Pro's native display hides button configuration, input-field labels,
/// and input-field password flags — all round-trip through XML but are
/// invisible in FM Pro's script editor. SharpFM extends the display line
/// with <c>; Buttons: [...]</c> and <c>; Inputs: [...]</c> trailing blocks
/// (style-guide form 3) so every POCO field survives display-text edits.
/// </para>
/// <para>
/// FM Pro always emits three <c>&lt;Button&gt;</c> slots and, when
/// <c>&lt;InputFields&gt;</c> is present, three <c>&lt;InputField&gt;</c> slots.
/// We preserve whatever count is in the source so round-trip is exact even
/// for hand-crafted fixtures that deviate from FM Pro's shape.
/// </para>
/// </summary>
public sealed class ShowCustomDialogStep : ScriptStep
{
    public Calculation Title { get; set; }
    public Calculation Message { get; set; }
    public IReadOnlyList<ShowCustomDialogButton> Buttons { get; set; }
    public IReadOnlyList<ShowCustomDialogInputField>? InputFields { get; set; }

    public ShowCustomDialogStep(
        bool enabled,
        Calculation title,
        Calculation message,
        IReadOnlyList<ShowCustomDialogButton> buttons,
        IReadOnlyList<ShowCustomDialogInputField>? inputFields = null)
        : base(StepCatalogLoader.ByName["Show Custom Dialog"], enabled)
    {
        Title = title;
        Message = message;
        Buttons = buttons;
        InputFields = inputFields;
    }

    [SuppressMessage("Usage", "CA2255:The 'ModuleInitializer' attribute should not be used in libraries",
        Justification = "Register typed step factories on assembly load.")]
    [ModuleInitializer]
    internal static void Register()
    {
        StepXmlFactory.Register("Show Custom Dialog", FromXml);
        StepDisplayFactory.Register("Show Custom Dialog", FromDisplayParams);
    }

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";

        var titleCalc = step.Element("Title")?.Element("Calculation");
        var title = titleCalc is not null ? Calculation.FromXml(titleCalc) : new Calculation("");

        var messageCalc = step.Element("Message")?.Element("Calculation");
        var message = messageCalc is not null ? Calculation.FromXml(messageCalc) : new Calculation("");

        var buttons = step.Element("Buttons")?.Elements("Button")
            .Select(ShowCustomDialogButton.FromXml).ToList()
            ?? new List<ShowCustomDialogButton>();

        var inputsEl = step.Element("InputFields");
        List<ShowCustomDialogInputField>? inputs = inputsEl is not null
            ? inputsEl.Elements("InputField").Select(ShowCustomDialogInputField.FromXml).ToList()
            : null;

        return new ShowCustomDialogStep(enabled, title, message, buttons, inputs);
    }

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", 87),
            new XAttribute("name", "Show Custom Dialog"));

        step.Add(new XElement("Title", Title.ToXml()));
        step.Add(new XElement("Message", Message.ToXml()));

        var buttonsEl = new XElement("Buttons");
        foreach (var button in Buttons)
            buttonsEl.Add(button.ToXml());
        step.Add(buttonsEl);

        if (InputFields is not null)
        {
            var inputsEl = new XElement("InputFields");
            foreach (var input in InputFields)
                inputsEl.Add(input.ToXml());
            step.Add(inputsEl);
        }

        return step;
    }

    public override string ToDisplayLine()
    {
        var parts = new List<string>
        {
            $"Title: {Title.Text}",
            $"Message: {Message.Text}",
        };

        if (InputFields is not null)
            parts.Add($"Inputs: [ {string.Join(" ; ", InputFields.Select(FormatInputSlot))} ]");

        // Suppress the Buttons block when it exactly matches FM Pro's
        // default 3-slot shape (OK/commit + two empty/nocommit). This is
        // lossless because the default shape round-trips identically
        // through FromDisplayParams's default construction below.
        if (!IsDefaultButtonShape(Buttons))
            parts.Add($"Buttons: [ {string.Join(" ; ", Buttons.Select(FormatButtonSlot))} ]");

        return $"Show Custom Dialog [ {string.Join(" ; ", parts)} ]";
    }

    private static bool IsDefaultButtonShape(IReadOnlyList<ShowCustomDialogButton> buttons)
    {
        if (buttons.Count != 3) return false;
        if (buttons[0].Label?.Text != "\"OK\"" || !buttons[0].CommitState) return false;
        if (buttons[1].Label is not null || buttons[1].CommitState) return false;
        if (buttons[2].Label is not null || buttons[2].CommitState) return false;
        return true;
    }

    private static List<ShowCustomDialogButton> DefaultButtons() => new()
    {
        new ShowCustomDialogButton(new Calculation("\"OK\""), true),
        new ShowCustomDialogButton(null, false),
        new ShowCustomDialogButton(null, false),
    };

    private static string FormatButtonSlot(ShowCustomDialogButton button)
    {
        // The Calculation.Text already contains FM calc source (quotes and all
        // for string literals), so don't wrap it. Empty slot renders as "".
        var labelText = button.Label?.Text ?? "\"\"";
        var commit = button.CommitState ? "commit" : "nocommit";
        return $"{labelText} {commit}";
    }

    private static string FormatInputSlot(ShowCustomDialogInputField input)
    {
        var labelText = input.Label?.Text ?? "\"\"";
        var password = input.UsePasswordCharacter ? "password" : "plain";
        var target = input.Target.ToDisplayString();
        if (input.Repetition is { } rep)
            target += $"[{rep}]";
        return $"{labelText} {password} {target}";
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var title = new Calculation("");
        var message = new Calculation("");
        List<ShowCustomDialogButton> buttons = new();
        List<ShowCustomDialogInputField>? inputs = null;

        foreach (var raw in hrParams)
        {
            var token = raw.Trim();

            if (token.StartsWith("Title:", StringComparison.OrdinalIgnoreCase))
                title = new Calculation(token.Substring("Title:".Length).Trim());
            else if (token.StartsWith("Message:", StringComparison.OrdinalIgnoreCase))
                message = new Calculation(token.Substring("Message:".Length).Trim());
            else if (token.StartsWith("Buttons:", StringComparison.OrdinalIgnoreCase))
                buttons = ParseButtonBlock(token.Substring("Buttons:".Length).Trim());
            else if (token.StartsWith("Inputs:", StringComparison.OrdinalIgnoreCase))
                inputs = ParseInputBlock(token.Substring("Inputs:".Length).Trim());
        }

        // No Buttons block in the display ⇒ FM Pro default 3-slot shape.
        // This is the round-trip pair to IsDefaultButtonShape in
        // ToDisplayLine; together they make the default shape invisible
        // in display text while fully round-tripping through XML.
        if (buttons.Count == 0)
            buttons = DefaultButtons();

        return new ShowCustomDialogStep(enabled, title, message, buttons, inputs);
    }

    private static List<ShowCustomDialogButton> ParseButtonBlock(string block)
    {
        var slots = SplitBracketBlock(block);
        var result = new List<ShowCustomDialogButton>();
        foreach (var slot in slots)
        {
            var (labelCalc, keyword) = SplitTrailingKeyword(slot);
            var commit = keyword.Equals("commit", StringComparison.OrdinalIgnoreCase);
            Calculation? calc = LabelTextToCalc(labelCalc);
            result.Add(new ShowCustomDialogButton(calc, commit));
        }
        return result;
    }

    private static List<ShowCustomDialogInputField> ParseInputBlock(string block)
    {
        var slots = SplitBracketBlock(block);
        var result = new List<ShowCustomDialogInputField>();
        foreach (var slot in slots)
            result.Add(ParseInputSlot(slot));
        return result;
    }

    private static readonly Regex TargetWithRep = new(
        @"^(?<target>.*?)\[(?<rep>\d+)\]$",
        RegexOptions.Compiled);

    private static ShowCustomDialogInputField ParseInputSlot(string slot)
    {
        // Input slot is "<calc> <plain|password> <target>". Take last two
        // whitespace-separated tokens as keyword + target; everything
        // before is the label calc. This lets calcs contain spaces.
        var trimmed = slot.Trim();
        if (string.IsNullOrEmpty(trimmed)) return ShowCustomDialogInputField.EmptySlot();

        int lastSpace = trimmed.LastIndexOf(' ');
        if (lastSpace < 0) return ShowCustomDialogInputField.EmptySlot();

        var targetText = trimmed.Substring(lastSpace + 1).Trim();
        var rest = trimmed.Substring(0, lastSpace).Trim();

        int kwSpace = rest.LastIndexOf(' ');
        if (kwSpace < 0) return ShowCustomDialogInputField.EmptySlot();

        var keyword = rest.Substring(kwSpace + 1).Trim();
        var labelText = rest.Substring(0, kwSpace).Trim();

        var password = keyword.Equals("password", StringComparison.OrdinalIgnoreCase);

        int? rep = null;
        var repMatch = TargetWithRep.Match(targetText);
        if (repMatch.Success)
        {
            rep = int.Parse(repMatch.Groups["rep"].Value);
            targetText = repMatch.Groups["target"].Value.Trim();
        }

        FieldRef target;
        if (targetText.StartsWith("$"))
            target = FieldRef.ForVariable(targetText);
        else
        {
            var sep = targetText.IndexOf("::");
            if (sep > 0)
                target = FieldRef.ForField(targetText.Substring(0, sep), 0, targetText.Substring(sep + 2));
            else
                target = FieldRef.ForField("", 0, "");
        }

        Calculation? label = LabelTextToCalc(labelText);

        return new ShowCustomDialogInputField(target, label, password, rep);
    }

    // Strip leading [ / trailing ], then split the interior on top-level ; only.
    private static List<string> SplitBracketBlock(string block)
    {
        var trimmed = block.Trim();
        if (trimmed.StartsWith("[")) trimmed = trimmed.Substring(1);
        if (trimmed.EndsWith("]")) trimmed = trimmed.Substring(0, trimmed.Length - 1);
        trimmed = trimmed.Trim();

        var result = new List<string>();
        if (string.IsNullOrEmpty(trimmed)) return result;

        var buf = new StringBuilder();
        int depth = 0;
        bool inQuote = false;

        for (int i = 0; i < trimmed.Length; i++)
        {
            var c = trimmed[i];
            if (c == '\\' && inQuote && i + 1 < trimmed.Length)
            {
                buf.Append(c);
                buf.Append(trimmed[i + 1]);
                i++;
                continue;
            }
            if (c == '"') inQuote = !inQuote;
            else if (!inQuote)
            {
                if (c == '[') depth++;
                else if (c == ']') depth--;
                else if (c == ';' && depth == 0)
                {
                    result.Add(buf.ToString().Trim());
                    buf.Clear();
                    continue;
                }
            }
            buf.Append(c);
        }

        if (buf.Length > 0) result.Add(buf.ToString().Trim());

        return result;
    }

    // Slot format is "<calcText> <keyword>". Keyword is the final
    // whitespace-separated token. CalcText is everything before.
    private static (string CalcText, string Keyword) SplitTrailingKeyword(string slot)
    {
        var trimmed = slot.Trim();
        int lastSpace = trimmed.LastIndexOf(' ');
        if (lastSpace < 0) return ("", trimmed);
        return (trimmed.Substring(0, lastSpace).Trim(), trimmed.Substring(lastSpace + 1).Trim());
    }

    // "" in the display text means empty-label (POCO has Label == null);
    // ToXml then omits the <Calculation> element. Any other calc text
    // becomes a literal Calculation value.
    private static Calculation? LabelTextToCalc(string text)
    {
        if (string.IsNullOrEmpty(text) || text == "\"\"") return null;
        return new Calculation(text);
    }
}
