namespace Erlang;

internal sealed class AllocatorQueryRequest
{
    internal Term Allocators
    {
        get;
    }
    internal bool Single
    {
        get;
    }

    private AllocatorQueryRequest(Term allocators, bool single)
    {
        Allocators = allocators;
        Single = single;
    }

    internal static AllocatorQueryRequest Create(Term value, IReadOnlySet<string> knownAllocators)
    {
        ArgumentNullException.ThrowIfNull(knownAllocators);
        bool single = value is Atom;
        if (!single && value is not Cons and not Nil)
            throw new ErlangException(ErlangErrorReasons.FunctionClause);

        Term allocators = single ? Term.List(value) : value;
        if (allocators is Nil)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        var known = new HashSet<string>(knownAllocators, StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        Term remaining = allocators;
        while (remaining is Cons cell)
        {
            if (cell.Head is not Atom allocator || !known.Contains(allocator.Name) || !seen.Add(allocator.Name))
                throw new ErlangException(ErlangErrorReasons.BadArgument);

            remaining = cell.Tail;
        }
        if (remaining is not Nil)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return new(allocators, single);
    }
}
