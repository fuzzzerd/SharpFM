namespace SharpFM.Model.Scripting.Registry;

/// <summary>
/// Factory contract for typed script-step POCOs. A class that implements
/// this interface participates in <see cref="StepRegistry"/> discovery:
/// the registry reflects for implementors at first access and reads the
/// static <see cref="Metadata"/> property from each one.
///
/// <para>
/// Uses a static-abstract member so the registration surface is enforced
/// at compile time — a POCO claiming <see cref="IStepFactory"/> without a
/// static <c>Metadata</c> property fails to build. Contrast with a runtime
/// convention (reflect for a field named "Metadata") which would fail
/// silently when the member is missing or misnamed.
/// </para>
/// </summary>
public interface IStepFactory
{
    static abstract StepMetadata Metadata { get; }
}
