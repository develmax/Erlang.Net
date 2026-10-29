namespace Erlang;

internal static class BinarySearchPatterns
{
    internal static async ValueTask<Term> Compile(ProcessContext context, Term input)
    {
        BinarySearchPattern pattern = await BinarySearchCompiler.Compile(context, input);

        return Term.Tuple(Term.A(pattern.Tag), context.Runtime.ReferenceResources.CreateBinarySearchPattern(pattern));
    }

    internal static async ValueTask<BinarySearchPatternHandle> Resolve(ProcessContext context, Term input)
    {
        if (input is not TupleTerm tuple)
            return new(await BinarySearchCompiler.Compile(context, input));
        if (tuple.Items.Count != BinarySearchParameters.PairArity
            || tuple.Items[0] is not Atom tag
            || tag.Name is not (BinarySearchNames.SinglePattern or BinarySearchNames.MultiplePatterns)
            || tuple.Items[1] is not ReferenceTerm reference)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        ReferenceResourceLease<BinarySearchPattern>? lease = context.Runtime.ReferenceResources.AcquireBinarySearchPattern(reference);
        if (lease is null)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        try
        {
            BinarySearchPattern pattern = lease.Value;
            if (pattern.Tag != tag.Name)
                throw new ErlangException(ErlangErrorReasons.BadArgument);

            return new(pattern, lease);
        }
        catch (ObjectDisposedException)
        {
            lease.Dispose();
            await context.ChargeAsync(0);
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
        catch
        {
            lease.Dispose();
            throw;
        }
    }
}
