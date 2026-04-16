using System;
using System.Collections.Generic;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Names of catalog-path ("RawStep") script steps that have been
/// verified to round-trip losslessly through the generic display-text
/// pipeline, and are therefore safe to edit as display text without
/// a typed POCO backing.
///
/// <para>
/// The list is empty at launch — every non-POCO step is sealed by
/// default. Entries are added only after manual verification of
/// round-trip fidelity (XML → display → XML byte-identical) for that
/// step shape.
/// </para>
///
/// <para>
/// A step name listed here must NOT also be backed by a typed POCO
/// registered with <c>StepXmlFactory</c> — POCOs get their editability
/// from being typed, this list is for the catalog-path fallback. The
/// <c>RawStepAllowListTests</c> contract test enforces the invariant.
/// </para>
/// </summary>
public static class RawStepAllowList
{
    public static readonly IReadOnlySet<string> Names =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}
