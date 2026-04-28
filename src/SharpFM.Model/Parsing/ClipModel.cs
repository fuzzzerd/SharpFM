using SharpFM.Model.Schema;
using SharpFM.Model.Scripting;

namespace SharpFM.Model.Parsing;

/// <summary>
/// Discriminated representation of a parsed clip's body. Concrete subtypes
/// carry the typed domain object for clip kinds we model
/// (<see cref="ScriptClipModel"/>, <see cref="TableClipModel"/>) or the raw
/// XML for kinds where we don't yet have a domain model
/// (<see cref="LayoutClipModel"/>, <see cref="OpaqueClipModel"/>).
/// </summary>
public abstract record ClipModel;

/// <summary>Parsed body for <c>Mac-XMSS</c> / <c>Mac-XMSC</c> clips.</summary>
public sealed record ScriptClipModel(FmScript Script) : ClipModel;

/// <summary>Parsed body for <c>Mac-XMTB</c> / <c>Mac-XMFD</c> clips.</summary>
public sealed record TableClipModel(FmTable Table) : ClipModel;

/// <summary>Parsed body for <c>Mac-XML2</c> (Layout) clips. No domain model yet — XML round-trips verbatim.</summary>
public sealed record LayoutClipModel(string Xml) : ClipModel;

/// <summary>Fallback for clip formats with no registered strategy. Body preserved as-is.</summary>
public sealed record OpaqueClipModel(string Xml) : ClipModel;
