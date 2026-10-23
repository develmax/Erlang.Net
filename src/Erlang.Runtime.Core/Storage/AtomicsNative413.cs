namespace Erlang;

internal static class AtomicsNative413
{
    internal static bool TryCapture(
        ProcessRuntime? owner,
        string module,
        string function,
        int arity,
        out ErlangFunction body
    )
    {
        if (owner is null || IntPtr.Size != sizeof(ulong) || module != AtomicsNames413.Module
            || !AtomicsNames413.Signatures.Contains((function, arity)))
        {
            body = null!;

            return false;
        }
        body = (context, arguments) => ValueTask.FromResult(Invoke(
            owner,
            context,
            function,
            arguments
        ));

        return true;
    }

    private static Term Invoke(
        ProcessRuntime owner,
        ProcessContext context,
        string function,
        IReadOnlyList<Term> arguments
    )
    {
        owner.RequireAtomicsActor413(context);
        using var invocation = owner.AtomicsOwner413.Enter();
        if (arguments[0] is not ReferenceTerm reference) throw new ErlangException(ErlangErrorReasons.BadArgument);
        using var resource = owner.ReferenceResources.AcquireAtomics413(reference);
        if (resource is null) throw new ErlangException(ErlangErrorReasons.BadArgument);
        AtomicsGuestRoot413 root = resource.Value;
        if (!ReferenceEquals(root.Owner, owner.AtomicsOwner413)) throw new ErlangException(ErlangErrorReasons.BadArgument);
        using var lease = root.Array.Acquire();
        if (lease is null) throw new ErlangException(ErlangErrorReasons.BadArgument);
        var array = lease.Value;
        if (function == AtomicsNames413.Info)
            return new MapTerm(new Dictionary<Term, Term>
            {
                [Term.A(AtomicsNames413.Size)] = new Integer(new System.Numerics.BigInteger(array.Length)),
                [Term.A(AtomicsNames413.Minimum)] = array.Signed ? Term.I(long.MinValue) : Term.I(0),
                [Term.A(AtomicsNames413.Maximum)] = array.Signed ? Term.I(long.MaxValue) : new Integer(new System.Numerics.BigInteger(ulong.MaxValue)),
                [Term.A(AtomicsNames413.Memory)] = new Integer(new System.Numerics.BigInteger(array.AllocatedBytes))
            });
        ulong index = AtomicsValues413.Index(arguments[1], array.Length);
        if (function == AtomicsNames413.Get)
            return AtomicsValues413.Term(Interlocked.Read(ref array.Cell(index)), array.Signed);
        bool increment = function is AtomicsNames413.Add or AtomicsNames413.AddGet;
        long value = AtomicsValues413.Value(arguments[2], array.Signed, increment);
        context.Cancellation.ThrowIfCancellationRequested();
        if (function == AtomicsNames413.Put)
        {
            Interlocked.Exchange(ref array.Cell(index), value);

            return Term.A(CommonAtomNames.Ok);
        }
        if (function == AtomicsNames413.Add || function == AtomicsNames413.AddGet)
        {
            long result = Interlocked.Add(ref array.Cell(index), value);

            return function == AtomicsNames413.Add ? Term.A(CommonAtomNames.Ok) : AtomicsValues413.Term(result, array.Signed);
        }
        if (function == AtomicsNames413.Exchange)
            return AtomicsValues413.Term(Interlocked.Exchange(ref array.Cell(index), value), array.Signed);
        long desired = AtomicsValues413.Value(arguments[3], array.Signed, false);
        context.Cancellation.ThrowIfCancellationRequested();
        long previous = Interlocked.CompareExchange(ref array.Cell(index), desired, value);

        return previous == value ? Term.A(CommonAtomNames.Ok) : AtomicsValues413.Term(previous, array.Signed);
    }
}
