using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang.Compiler;

public sealed record FunctionDefinition(string Name, int Arity, IReadOnlyList<Clause> Clauses)
{
    internal bool IsInlineFailure { get; init; }
    internal bool IsClosedListMapper { get; init; }
    internal bool IsRecursiveListMapper { get; init; }
    internal int? CapturedListLengthParameter { get; init; }
    internal bool HasProperListResult { get; init; }
    internal bool HasNilTerminatedMapperResult { get; init; }
    internal bool HasProvenRecursiveTupleInput { get; init; }
    internal bool HasProvenRecursiveTupleFilterInput { get; init; }
    internal bool IsStrictRecursiveListMapper { get; init; }
    internal int? StaticListClosureTarget { get; init; }
}
