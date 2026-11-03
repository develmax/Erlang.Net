using System.Runtime.CompilerServices;

namespace Erlang;

public sealed partial class ProcessRuntime
{
    private readonly ConditionalWeakTable<Term, DebugSharingLiteralMarker425> sharingLiterals425 = new();

    internal void MarkPreparedLiterals425(IReadOnlyList<Term> literals)
    {
        // Called only for real linked literal roots, after structural validation.
        // Equal runtime-created values are never classified by value equality.
        var pending = new Stack<Term>(literals);
        var seen = new HashSet<Term>(ReferenceEqualityComparer.Instance);
        while (pending.TryPop(out Term? term))
        {
            if (DebugSharingWordLayout425.Immediate(term) || !seen.Add(term)
                || term is FunctionTerm global && Functions.IsExternalEntry(global)) continue;
            sharingLiterals425.GetValue(term, static _ => new());
            foreach (Term child in DebugSharingGraph425.Children(term)) pending.Push(child);
        }
    }

    internal bool IsSharingLiteral425(Term value)
        // export.c allocates/registers the canonical export lambda in actual
        // global literal storage. This exact owned table slot is global even
        // when created dynamically, independently of BEAM literal references.
        => IsEmptyTuple803(value) || value is FunctionTerm function && Functions.IsExternalEntry(function)
            || sharingLiterals425.TryGetValue(value, out _);
}
