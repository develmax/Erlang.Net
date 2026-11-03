namespace Erlang;

internal static class BinarySearchPatterns
{
    internal static async ValueTask<Term> Compile(ProcessContext context, Term input)
    {
        BinarySearchPattern pattern = await BinarySearchCompiler.Compile(context, input);
        ResourceReferenceTerm? admitted = null;
        try
        {
            ResourceReferenceTerm identity = context.Runtime.ReferenceResources.CreateBinarySearchPattern(pattern);
            admitted = OwnedTermInvocation506.AdmitRehydratedBinary598(identity, pattern.Backing598);

            return Term.Tuple(Term.A(pattern.Tag), admitted);
        }
        catch (Exception original)
        {
            try
            {
                admitted?.BinaryHost598?.Dispose();
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(original, cleanup);
            }
            throw;
        }
        finally
        {
            pattern.Backing598.ReleaseProducer598();
        }
    }

    internal static async ValueTask<BinarySearchPatternHandle> Resolve(ProcessContext context, Term input)
    {
        if (input is not TupleTerm tuple)
        {
            BinarySearchPattern built = await BinarySearchCompiler.Compile(context, input);
            BinaryArenaReader598? reader = null;
            try
            {
                reader = built.Backing598.Read598();
                var result = new BinarySearchPatternHandle(built, reader);
                reader = null;

                return result;
            }
            finally
            {
                try
                {
                    reader?.Dispose();
                }
                finally
                {
                    built.Backing598.ReleaseProducer598();
                }
            }
        }
        if (tuple.Items.Count != BinarySearchParameters.PairArity
            || tuple.Items[0] is not Atom tag
            || tag.Name is not (BinarySearchNames.SinglePattern or BinarySearchNames.MultiplePatterns)
            || tuple.Items[1] is not ReferenceTerm reference)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        ReferenceResourceLease<BinarySearchPattern>? resource = context.Runtime.ReferenceResources.AcquireBinarySearchPattern(reference);
        if (resource is null) throw new ErlangException(ErlangErrorReasons.BadArgument);
        BinaryArenaReader598? pin = null;
        try
        {
            BinarySearchPattern pattern = resource.Value;
            if (pattern.Tag != tag.Name || !ReferenceEquals(BinaryProgramRoots598.Direct598(reference, context.Runtime), pattern.Backing598))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            pin = pattern.Backing598.Read598();
            GC.KeepAlive(reference);
            var result = new BinarySearchPatternHandle(
                pattern,
                pin,
                resource,
                reference
            );
            pin = null;
            resource = null;

            return result;
        }
        catch (ObjectDisposedException)
        {
            await context.ChargeAsync(0);
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
        finally
        {
            try
            {
                pin?.Dispose();
            }
            finally
            {
                resource?.Dispose();
            }
        }
    }
}
