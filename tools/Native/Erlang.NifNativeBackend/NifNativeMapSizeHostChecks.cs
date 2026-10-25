namespace Erlang.NifNativeBackend;

internal static unsafe class NifNativeMapSizeHostChecks
{
    internal static void ShapeAndOutput(ProcessContext context)
    {
        var failure = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), failure);
        using var owner = new NifNativeCallEnvironment(
            context,
            new ModuleCodeImage(NifNativeMapSizeHostNames.Module),
            0,
            failure
        );
        using var table = new NifNativeWindowsCallbacks();
        nuint* output = stackalloc nuint[3];
        foreach (int size in NifNativeMapSizeHostNames.Sizes)
        {
            nuint handle = owner.Add(Map(size));
            output[0] = NifNativeMapSizeHostNames.Before;
            output[1] = nuint.MaxValue;
            output[2] = NifNativeMapSizeHostNames.After;
            Require(Call(
                table,
                owner.Pointer,
                handle,
                &output[1]
            ) == 1);
            Require(output[1] == (nuint)size);
            Require(output[0] == NifNativeMapSizeHostNames.Before && output[2] == NifNativeMapSizeHostNames.After);
        }
        Term[] wrong = [Nil.Value, Term.I(1), new Integer(System.Numerics.BigInteger.One << 200),
            new FloatTerm(1), Term.A(NifNativeMapSizeHostNames.Module), Term.Tuple(Map(0)),
            Term.List(Term.I(1)), new BitString(new byte[] { 1 })];
        foreach (Term term in wrong)
        {
            nuint handle = owner.Add(term);
            output[1] = nuint.MaxValue;
            Require(Call(
                table,
                0,
                handle,
                &output[1]
            ) == 0 && output[1] == nuint.MaxValue);
            Require(Call(
                table,
                0,
                handle,
                null
            ) == 0);
        }
        failure.ThrowPending();
    }

    internal static void PureEnvironment(ProcessContext context)
    {
        var failure = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), failure);
        var image = new ModuleCodeImage(NifNativeMapSizeHostNames.Module);
        using var owner = new NifNativeCallEnvironment(
            context,
            image,
            0,
            failure
        );
        using var other = new NifNativeCallEnvironment(
            context,
            image,
            0,
            failure
        );
        using var table = new NifNativeWindowsCallbacks();
        nuint term = owner.Add(Map(33));
        foreach (nint hint in new[] { (nint)0, (nint)1, other.Pointer })
        {
            nuint count = nuint.MaxValue;
            Require(Call(
                table,
                hint,
                term,
                &count
            ) == 1 && count == 33);
        }
        var nested = new MapTerm([new KeyValuePair<Term, Term>(Term.I(1), Map(8193))]);
        nuint nestedCount = nuint.MaxValue;
        Require(Call(
            table,
            0,
            owner.Add(nested),
            &nestedCount
        ) == 1 && nestedCount == 1);
        failure.ThrowPending();
    }

    internal static void OwnerLifetime(ProcessContext context)
    {
        var failure = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), failure);
        var image = new ModuleCodeImage(NifNativeMapSizeHostNames.Module);
        using var table = new NifNativeWindowsCallbacks();
        using var independent = new NifNativeCallEnvironment(context.Runtime, image);
        nuint copied;
        using (var caller = new NifNativeCallEnvironment(
            context,
            image,
            0,
            failure
        ))
        {
            copied = independent.Add(caller.Get(caller.Add(Map(33))));
        }
        nint address = ((nint*)table.Pointer)[NifNativeMapSizeHostNames.Slot];
        Exception? threadFailure = null;
        var protectedWorker = new Thread(() =>
        {
            try
            {
                nuint count = nuint.MaxValue;
                var callback = (delegate* unmanaged[Cdecl]<nint, nuint, nuint*, int>)address;
                Require(callback(0, copied, &count) == 1 && count == 33);
            }
            catch (Exception error)
            {
                threadFailure = error;
            }
        });
        protectedWorker.Start();
        Require(protectedWorker.Join(NifNativeMapSizeHostNames.Deadline));
        if (threadFailure is not null)
            throw new InvalidOperationException(NifNativeMapSizeHostNames.Failure, threadFailure);
        nuint retained = nuint.MaxValue;
        Require(Call(
            table,
            0,
            copied,
            &retained
        ) == 1 && retained == 33);
        failure.ThrowPending();
    }

    private static MapTerm Map(int size) => new(Enumerable.Range(0, size).Select(value =>
        new KeyValuePair<Term, Term>(Term.I(value), Term.Tuple(Term.I(value)))));

    private static int Call(
        NifNativeWindowsCallbacks table,
        nint hint,
        nuint term,
        nuint* output
    )
        => ((delegate* unmanaged[Cdecl]<nint, nuint, nuint*, int>)((nint*)table.Pointer)[NifNativeMapSizeHostNames.Slot])(hint, term, output);

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(NifNativeMapSizeHostNames.Failure);
    }
}
