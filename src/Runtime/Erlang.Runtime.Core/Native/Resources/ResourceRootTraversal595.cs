using System.Diagnostics.CodeAnalysis;

namespace Erlang;

// Root discovery only. Preserve physical map descendants while avoiding copy
// frames/child-array snapshots for immutable ordinary graph traversal.
internal sealed class ResourceRootTraversal595
{
    private readonly HashSet<Term> seen = new(ReferenceEqualityComparer.Instance);
    private readonly Stack<Term> pending = new();

    internal ResourceRootTraversal595(Term source) => Enqueue(source);

    internal bool TryPop([NotNullWhen(true)] out Term? value) => pending.TryPop(out value);

    internal void EnqueueChildren(Term value)
    {
        switch (value)
        {
            case BitString { ResourceSliceOrigin577: { } origin }:
                Enqueue(origin);
                break;
            case Cons list:
                Enqueue(list.Head);
                Enqueue(list.Tail);
                break;
            case TupleTerm tuple:
                foreach (Term child in tuple.Items)
                    Enqueue(child);
                break;
            case HamtBitmapTerm429 node:
                foreach (Term child in node.Children)
                    Enqueue(child);
                break;
            case MapTerm:
                // Keep canonical/arithmetic/hidden physical rows, structural
                // terms and flat keys exactly as the existing map-root view.
                foreach (Term child in NativeMapOwnedRoots510.Children(value))
                    Enqueue(child);
                break;
            case NativeRecordTerm record:
                foreach (Term child in record.Values)
                    Enqueue(child);
                break;
            case FunctionTerm function:
                foreach (Term child in function.Environment)
                    Enqueue(child);
                break;
        }
    }

    private void Enqueue(Term value)
    {
        if (seen.Add(value))
            pending.Push(value);
    }
}
