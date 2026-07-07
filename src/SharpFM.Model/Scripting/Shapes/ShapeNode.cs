using System;
using System.Collections.Generic;

namespace SharpFM.Model.Scripting.Shapes;

/// <summary>
/// How a shape node's value participates in the human-readable display line
/// SharpFM renders for a step. FileMaker Pro's own script display is lossy —
/// it hides values the XML carries (options, repetitions, targets) behind its
/// UI. Because SharpFM is an XML-faithful editor it must surface those hidden
/// values with an invented but <em>consistent</em> syntax so they stay visible
/// and editable.
/// </summary>
public enum DisplayMode
{
    /// <summary>Rendered the way FileMaker Pro shows it.</summary>
    Native,

    /// <summary>
    /// FileMaker Pro's UI hides this value, but the XML carries it. SharpFM
    /// surfaces it through one uniform convention (a trailing
    /// <c>[HrLabel: value]</c> segment) applied identically across every step
    /// that needs it.
    /// </summary>
    Augmented,

    /// <summary>Never shown in the display line (round-tripped in XML only).</summary>
    Hidden,
}

/// <summary>
/// One ordered entry in a step's declarative <c>Shape</c> — the single source
/// of truth for how a child (or step-level attribute) serializes to XML,
/// parses back, validates, and renders in the display line. A step's
/// <c>Shape</c> is an ordered <see cref="IReadOnlyList{ShapeNode}"/>; element
/// order in the emitted XML matches shape order exactly, which is what fixes
/// the canonical element-order requirements FileMaker's paste handler enforces.
///
/// <para>
/// The base carries the display/agent-facing metadata (<see cref="HrLabel"/>,
/// <see cref="Required"/>, <see cref="ValidValues"/>, <see cref="DisplayValues"/>,
/// <see cref="DefaultValue"/>, <see cref="Description"/>) so each shape entry is
/// the single description of both its wire emission and its human-readable
/// role. Concrete records add the per-kind emission rule.
/// </para>
///
/// <para>
/// <see cref="PocoProperty"/> binds the node to a property on the step POCO by
/// name (resolved by the renderer/parser via cached reflection). When omitted
/// it defaults to the node's element/attribute name, which is the common case
/// (element <c>Name</c> ↔ property <c>Name</c>).
/// </para>
/// </summary>
public abstract record ShapeNode
{
    /// <summary>POCO property this node reads/writes. Defaults to the node's element/attribute name.</summary>
    public string? PocoProperty { get; init; }

    /// <summary>Human-readable label for display text and completion.</summary>
    public string? HrLabel { get; init; }

    /// <summary>True when the child must be present in valid canonical XML.</summary>
    public bool Required { get; init; }

    /// <summary>
    /// True when the child is emitted only if its bound value is populated /
    /// non-default. FileMaker omits most optional children from the canonical
    /// form of an unconfigured step and emits them once configured; this flag
    /// is what keeps SharpFM from emitting empty placeholder elements FileMaker
    /// does not write.
    /// </summary>
    public bool Optional { get; init; }

    /// <summary>Closed set of permissible wire values for enum / boolean nodes.</summary>
    public IReadOnlyList<string>? ValidValues { get; init; }

    /// <summary>
    /// Display-text forms of the permissible values when they differ from the
    /// wire forms (e.g. View As shows "View as Form" for the wire value
    /// "Form"). Display consumers fall back to <see cref="ValidValues"/> when
    /// unset.
    /// </summary>
    public IReadOnlyList<string>? DisplayValues { get; init; }

    /// <summary>Default value assumed when the child is absent.</summary>
    public string? DefaultValue { get; init; }

    /// <summary>How this node participates in the display line.</summary>
    public DisplayMode Display { get; init; } = DisplayMode.Native;

    /// <summary>Human-facing explanation of the value — tooltip / hover source.</summary>
    public string? Description { get; init; }
}

/// <summary>Step-level extra attribute, e.g. <c>&lt;Step Source="MBSP" index="2" …&gt;</c> (MBS plugin).</summary>
public sealed record AttributeNode(string AttrName) : ShapeNode;

/// <summary>
/// A boolean child carried on an attribute: <c>&lt;El state="True|False"/&gt;</c>
/// by default, or <c>&lt;El value="True|False"/&gt;</c> when <see cref="Attr"/> is
/// <c>"value"</c> (e.g. Add Account's <c>ChgPwdOnNextLogin</c>). Bound to a
/// <see cref="bool"/> property.
/// </summary>
public sealed record BoolStateChild(string Element, string Attr = "state") : ShapeNode;

/// <summary><c>&lt;El value="X"/&gt;</c> bound to a string/enum property (FlushType, PauseTime, CallbackScriptState…).</summary>
public sealed record EnumValueChild(string Element) : ShapeNode;

/// <summary>
/// A presence-as-boolean flag element bound to a <see cref="bool"/> property:
/// the bare element <c>&lt;El/&gt;</c> is emitted when the value is true and
/// omitted when false (FileMaker's "flagElement" convention — e.g. Insert
/// Embedding in Found Set's <c>Overwrite</c>/<c>ContinueOnError</c>/<c>ShowSummary</c>).
/// </summary>
public sealed record FlagChild(string Element) : ShapeNode;

/// <summary><c>&lt;Calculation&gt;&lt;![CDATA[…]]&gt;&lt;/Calculation&gt;</c> bound to a <c>Calculation</c> property (If condition, Exit Script return).</summary>
public sealed record BareCalcChild : ShapeNode;

/// <summary>
/// <c>&lt;El&gt;&lt;Calculation&gt;…&lt;/Calculation&gt;&lt;/El&gt;</c> bound to a
/// <c>Calculation</c> property (Set Variable Value/Repetition; Show Custom
/// Dialog Title/Message). Also covers the Install OnTimer Script
/// <c>&lt;Interval&gt;&lt;Calculation&gt;</c> §7.3 trap — structurally the same
/// rule, declared as <c>NamedCalcChild("Interval")</c>.
/// </summary>
public sealed record NamedCalcChild(string Element) : ShapeNode;

/// <summary>
/// <c>&lt;El&gt;text&lt;/El&gt;</c> bound to a <see cref="string"/> property —
/// the Set Variable <c>&lt;Name&gt;$var&lt;/Name&gt;</c> §7.1/7.4 trap, plus
/// Configure Persistent Data <c>&lt;Name&gt;</c>, Flow, Text, etc. When
/// <see cref="Attr"/> is set the element also carries one attribute bound to
/// its own property (e.g. Insert PDF's
/// <c>&lt;UniversalPathList type="Embedded"&gt;path&lt;/UniversalPathList&gt;</c>).
/// </summary>
public sealed record NamedTextChild(string Element) : ShapeNode
{
    /// <summary>Attribute carried on the same element, always emitted when set.</summary>
    public string? Attr { get; init; }

    /// <summary>POCO property the attribute binds to. Defaults to <see cref="Attr"/>.</summary>
    public string? AttrProperty { get; init; }

    /// <summary>Attribute value assumed when the element or attribute is absent.</summary>
    public string? AttrDefault { get; init; }
}

/// <summary>
/// <c>&lt;Field table=… id=… name=…/&gt;</c> or <c>&lt;Field&gt;$var&lt;/Field&gt;</c>
/// bound to a <c>FieldRef</c> property. When <see cref="VariableTextMarker"/> is
/// set and the bound reference is a variable, FileMaker's bare <c>&lt;Text/&gt;</c>
/// marker element is emitted immediately before the <c>&lt;Field&gt;</c> (Insert
/// Calculated Result, Write to Data File, …).
/// </summary>
public sealed record FieldChild(string Element = "Field") : ShapeNode
{
    public bool VariableTextMarker { get; init; }
}

/// <summary><c>&lt;Script id=… name=…/&gt;</c>, <c>&lt;Layout …/&gt;</c>, <c>&lt;Table …/&gt;</c> bound to a <c>NamedRef</c> property.</summary>
public sealed record NamedRefChild(string Element) : ShapeNode;

/// <summary>
/// Delegates emission/parsing to a value type's own
/// <c>XElement ToXml(string)</c> / <c>static FromXml(XElement)</c> convention
/// (<c>NewWindowStyles</c>, <c>Animation</c>, print-settings types…). The bound
/// property's type owns the element's full attribute/child shape.
/// </summary>
public sealed record ValueTypeChild(string Element) : ShapeNode;

/// <summary>
/// <c>&lt;Parameters Count="N"&gt;&lt;P&gt;…&lt;/P&gt;…&lt;/Parameters&gt;</c>
/// bound to a list property — the Perform JavaScript in Web Viewer §7.2 trap
/// (parameters must be <c>&lt;P&gt;</c>, never flat <c>&lt;Parameter&gt;</c>).
/// </summary>
public sealed record ParametersList(string Wrapper = "Parameters", string Child = "P") : ShapeNode;

/// <summary>
/// A nested wrapper element that encloses <see cref="Children"/>, e.g. Configure
/// RAG Account's <c>&lt;ConfigureRAGAccount&gt;</c> grouping. The wrapper is
/// always emitted (empty when every child is omitted); children bind to the
/// owning step's properties and their XML lookups are relative to the wrapper.
/// With <see cref="ShapeNode.Optional"/> set, the wrapper is emitted only when
/// the gate property named by <see cref="ShapeNode.PocoProperty"/> is non-null
/// (e.g. Save a Copy as XML's <c>&lt;SaXML&gt;</c> block).
/// </summary>
public sealed record WrapperChild(string Element, IReadOnlyList<ShapeNode> Children) : ShapeNode;

/// <summary>
/// One case of a <see cref="VariantBlock"/>. Emission selects the case whose
/// <see cref="WhenType"/> matches the bound property's runtime type. Parsing
/// selects the first case (in declaration order) whose
/// <see cref="MatchElement"/> is present in the step XML and — when
/// <see cref="MatchValues"/> is set — whose <c>value</c> attribute is in that
/// set. <see cref="MatchValues"/> may list legacy wire-value aliases alongside
/// the canonical value; a case with no <see cref="MatchValues"/> matches on
/// element presence alone, and a case with no <see cref="MatchElement"/> is the
/// unconditional fallback.
/// </summary>
public sealed record VariantCase(Type WhenType, IReadOnlyList<ShapeNode> Children)
{
    /// <summary>Element whose presence selects this case during parsing.</summary>
    public string? MatchElement { get; init; }

    /// <summary>Accepted <c>value</c> attribute values on <see cref="MatchElement"/> (canonical + legacy aliases).</summary>
    public IReadOnlyList<string>? MatchValues { get; init; }
}

/// <summary>
/// Discriminated-union selection. Picks the <see cref="VariantCase"/> whose
/// <see cref="VariantCase.WhenType"/> matches the bound property's runtime type
/// and emits that case's children, which bind to the variant value's own
/// properties. Used by <c>PerformScriptTarget</c> (ByReference vs
/// ByCalculation) and <c>LayoutTarget</c>.
/// </summary>
public sealed record VariantBlock(IReadOnlyList<VariantCase> Cases) : ShapeNode;

/// <summary>
/// Preserves unrecognized child elements verbatim. Bound to a property holding
/// captured <c>XElement</c>s so a partially-known step round-trips children the
/// shape does not model (e.g. Print PDF <c>&lt;PlatformData&gt;</c> blobs).
/// </summary>
public sealed record Passthrough : ShapeNode;

/// <summary>
/// A display-grammar-only slot: emits and parses no XML of its own, but
/// carries the label/values metadata for a display-line token whose wire form
/// lives inside a sibling <see cref="Passthrough"/> bag or a value type's own
/// serializer (e.g. Send Mail's To/Cc/Bcc, Enter Find Mode's stored requests).
/// Keeps the display consumers (validation, completion, param synthesis)
/// working for those tokens without duplicating the wire description.
/// </summary>
public sealed record HrOnly(string Name) : ShapeNode
{
    /// <summary>True when the token is an On/Off or presence flag.</summary>
    public bool Boolean { get; init; }
}
