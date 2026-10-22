namespace Erlang;

internal sealed class BinaryReplacePlan(
    FunctionTerm? callback,
    BitString? replacement,
    IReadOnlyList<BitString>? pieces,
    bool deferredBadFunction = false
)
{
    internal static async ValueTask<BinaryReplacePlan> Create(ProcessContext context, Term replacement, Term insert)
    {
        if (replacement is FunctionTerm function)
            return new(function, null, null);
        var binary = (BitString)replacement;
        if (insert is Atom { Name: ErlangBooleanAtoms.True })
            return new(
                null,
                binary,
                null,
                true
            );
        if (insert is Nil)
            return new(null, binary, null);
        var positions = new List<Term>();
        if (insert is Integer)
        {
            binary.PinForMatch();
            positions.Add(insert);
        }
        else
        {
            foreach (Term item in Cons.Items(insert))
            {
                await context.ReduceAsync();
                positions.Add(item);
            }
            positions = await Sort(context, positions);
        }
        var pieces = new List<BitString>();
        int previous = 0;
        foreach (Term position in positions)
        {
            await context.ReduceAsync();
            if (position is not Integer integer || integer.Value < previous || integer.Value > binary.ByteLength)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            int next = (int)integer.Value;
            pieces.Add((BitString)await context.Runtime.Modules.Call(
                context,
                BinaryModuleNames.Module,
                BinaryModuleNames.Part,
                [binary, Term.Tuple(Term.I(previous), Term.I(next - previous))]
            ));
            previous = next;
        }
        pieces.Add((BitString)await context.Runtime.Modules.Call(
            context,
            BinaryModuleNames.Module,
            BinaryModuleNames.Part,
            [binary, Term.Tuple(Term.I(previous), Term.I(binary.ByteLength - previous))]
        ));

        return new(null, null, pieces);
    }

    private static async ValueTask<List<Term>> Sort(ProcessContext context, List<Term> values)
    {
        Term[] source = values.ToArray(), target = new Term[values.Count];
        for (long width = 1; width < source.Length; width *= 2)
        {
            for (long start = 0; start < source.Length; start += width * 2)
            {
                int left = (int)start, end = (int)Math.Min(start + width * 2, source.Length);
                int middle = (int)Math.Min(start + width, source.Length), right = middle;
                for (int i = left; i < end; i++)
                {
                    await context.ReduceAsync();
                    target[i] = right >= end || left < middle && source[left].CompareTo(source[right]) <= 0
                        ? source[left++] : source[right++];
                }
            }
            (source, target) = (target, source);
        }

        return new List<Term>(source);
    }

    internal async ValueTask<Term> Apply(ProcessContext context, Term matched, bool pending)
    {
        if (callback is not null)
            return await BinaryWrapperTrace.Callback(
                context,
                callback,
                matched,
                pending
            );
        if (deferredBadFunction)
            throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadFunction), replacement!));
        if (replacement is not null)
            return replacement;
        var result = new List<Term>();
        foreach (BitString part in pieces!)
        {
            await context.ReduceAsync();
            if (result.Count != 0)
                result.Add(matched);
            result.Add(part);
        }

        return Cons.From(result);
    }
}
