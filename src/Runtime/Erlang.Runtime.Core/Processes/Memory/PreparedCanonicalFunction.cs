namespace Erlang;

internal sealed record PreparedCanonicalFunction(FunctionTerm Function, int Arity, Term[] Environment)
{
    internal IReadOnlySet<Term> StableGlobals { get; init; } = new HashSet<Term>(ReferenceEqualityComparer.Instance);

    internal static IReadOnlySet<Term> CaptureStableGlobals(ProcessRuntime runtime, IEnumerable<Term> environment)
    {
        var result = new HashSet<Term>(ReferenceEqualityComparer.Instance);
        var seen = new HashSet<Term>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<Term>(environment);
        while (pending.TryPop(out Term? value))
        {
            if (!seen.Add(value)) continue;
            if (value is FunctionTerm function && runtime.Functions.IsExternalEntry(function)) result.Add(value);
            foreach (Term child in NativeMapOwnedRoots510.Children(value)) pending.Push(child);
        }

        return result;
    }
}
