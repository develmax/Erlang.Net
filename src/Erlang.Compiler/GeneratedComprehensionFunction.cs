namespace Erlang.Compiler;

public sealed class GeneratedComprehensionFunction(
    int qualifierIndex,
    string function,
    int arity,
    int coreOrdinal,
    int liftedOrdinal,
    int sourceLine = 0
)
{
    public int QualifierIndex { get; } = qualifierIndex;
    public string Function { get; } = function;
    public int Arity { get; } = arity;
    public int CoreOrdinal { get; } = coreOrdinal;
    public int LiftedOrdinal { get; } = liftedOrdinal;
    public int SourceLine { get; } = sourceLine;
    public bool DiscardConsContinuation { get; init; }
    internal object EntryContinuation { get; } = new();
    internal object RecursiveContinuation { get; } = new();
}
