namespace SharpFM.Core.ScriptConverter.Renderers;

public interface IMultiStepRenderer : IStepRenderer
{
    string[] StepNames { get; }
}
