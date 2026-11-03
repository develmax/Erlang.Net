using static Erlang.Tests.SpawnWorker546Names;

namespace Erlang.Tests;

internal static class SpawnWorker546FunctionScope
{
    internal static void RequireExportFunctionsOnly(Term term)
    {
        var pending = new Stack<Term>();
        pending.Push(term);
        while (pending.TryPop(out Term? current))
        {
            switch (current)
            {
                case FunctionTerm function:
                    if (function.LocalIdentity is not null || function.ExternalIdentity is null
                        || function.Environment.Count != 0 || function.Arity != function.ExternalIdentity.Arity)
                        throw new InvalidDataException(Failure);
                    break;
                case TupleTerm tuple:
                    foreach (Term value in tuple.Items)
                        pending.Push(value);
                    break;
                case Cons cell:
                    pending.Push(cell.Head);
                    pending.Push(cell.Tail);
                    break;
                case MapTerm map:
                    foreach (var entry in map.Entries)
                    {
                        pending.Push(entry.Key);
                        pending.Push(entry.Value);
                    }
                    break;
            }
        }
    }
}
