using System.Reflection;

namespace Erlang.Tests;

// The controller owns this fresh observation graph. Only real exported Host
// carriers are closed; canonical entries and guest headers are not fabricated
// or manually destroyed. No CLR collection is used as retirement evidence.
internal static class OriginalCounters610ResultOwnership
{
    internal static IDisposable CaptureHostCarriers(Term graph)
    {
        var terms = new HashSet<Term>(ReferenceEqualityComparer.Instance);
        var carriers = new HashSet<IDisposable>(ReferenceEqualityComparer.Instance);
        var stack = new Stack<Term>();
        stack.Push(graph);
        try
        {
            while (stack.TryPop(out Term? term))
            {
                if (!terms.Add(term)) continue;
                if (term is ResourceReferenceTerm resource)
                    foreach (string name in new[] { OriginalCounters610Fields.AtomicsHost586, OriginalCounters610Fields.CountersHost596 })
                        if (typeof(ResourceReferenceTerm).GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
                            .GetValue(resource) is IDisposable carrier) carriers.Add(carrier);
                if (term is TupleTerm tuple) foreach (Term child in tuple.Items) stack.Push(child);
                else if (term is Cons cons)
                {
                    stack.Push(cons.Head);
                    stack.Push(cons.Tail);
                }
                else if (term is MapTerm map) foreach (var pair in map.Entries)
                {
                    stack.Push(pair.Key);
                    stack.Push(pair.Value);
                }
                else if (term is FunctionTerm function) foreach (Term child in function.Environment) stack.Push(child);
            }

            return new Captured(carriers.ToArray());
        }
        catch (Exception original)
        {
            try
            {
                Release(carriers);
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(original, cleanup);
            }
            throw;
        }
    }

    private static void Release(IEnumerable<IDisposable> carriers)
    {
        var failures = new List<Exception>();
        foreach (IDisposable carrier in carriers)
            try
            {
                carrier.Dispose();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        if (failures.Count != 0) throw new AggregateException(OriginalCounters610Names.Failure, failures);
    }

    private sealed class Captured(IDisposable[] carriers) : IDisposable
    {
        private IDisposable[]? owned = carriers;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref owned, null) is { } released) Release(released);
        }
    }
}
