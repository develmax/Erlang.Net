using System.Numerics;

namespace Erlang;

internal static class BinarySearchOptionParser
{
    internal static async ValueTask<BinarySearchOptions> Parse(ProcessContext context, Term options, int size, BinarySearchOperation operation)
    {
        int start = 0;
        int end = size;
        bool global = operation == BinarySearchOperation.Matches;
        bool trim = false;
        bool trimAll = false;
        if (options is not (Cons or Nil))
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        var work = new BinarySearchWork(context);
        while (options is Cons cell)
        {
            await work.Step();
            Term option = cell.Head;
            options = cell.Tail;
            if (operation == BinarySearchOperation.Split && option is Atom atom)
            {
                if (atom.Name == BinarySearchNames.Global)
                {
                    global = true;
                    continue;
                }
                if (atom.Name == BinarySearchNames.Trim)
                {
                    trim = true;
                    continue;
                }
                if (atom.Name == BinarySearchNames.TrimAll)
                {
                    trimAll = true;
                    continue;
                }
            }
            if (option is not TupleTerm { Items.Count: BinarySearchParameters.PairArity } pair
                || pair.Items[0] is not Atom { Name: BinarySearchNames.Scope }
                || pair.Items[1] is not TupleTerm { Items.Count: BinarySearchParameters.PairArity } range
                || range.Items[0] is not Integer position || position.Value < 0 || position.Value > ulong.MaxValue
                || range.Items[1] is not Integer length || length.Value < long.MinValue || length.Value > long.MaxValue)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            BigInteger first = position.Value;
            BigInteger count = length.Value;
            if (count < 0)
            {
                if (count == long.MinValue)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                first += count;
                count = -count;
            }
            BigInteger last = first + count;
            if (first < 0 || first > size || last > size)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            start = (int)first;
            end = (int)last;
        }
        return new(start, end, global, trim, trimAll);
    }
}
