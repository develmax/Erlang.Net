namespace Erlang.NifNativeBackend;

internal static unsafe class NifNativeMapValue503HostChecks
{
    internal static void ShapeAndOutput(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
        using var owner = new NifNativeCallEnvironment(
            context,
            new ModuleCodeImage(NifNativeMapValue503HostNames.Module),
            0,
            state
        );
        using var table = new NifNativeWindowsCallbacks();
        nuint* output = stackalloc nuint[3];
        Term integer = Term.I(1), floating = new FloatTerm(1.0);
        var map = new MapTerm([new(integer, Term.I(11)), new(floating, Term.I(22))]);
        nuint mapHandle = owner.Add(map);
        foreach (var entry in map.Entries)
        {
            Reset(output);
            Require(Call(
                table,
                owner.Pointer,
                mapHandle,
                owner.Add(entry.Key),
                &output[1]
            ) == 1);
            Require(owner.Get(output[1]).Equals(entry.Value));
            Require(output[0] == NifNativeMapValue503HostNames.Before && output[2] == NifNativeMapValue503HostNames.After);
        }
        Reset(output);
        nuint missing = owner.Add(Term.I(3));
        Require(Call(
            table,
            0,
            mapHandle,
            missing,
            &output[1]
        ) == 0 && output[1] == NifNativeMapValue503HostNames.Unchanged);
        Require(Call(
            table,
            0,
            mapHandle,
            missing,
            null
        ) == 0);
        foreach (Term nonmap in new Term[] { Nil.Value, integer, floating, Term.Tuple(map), Term.List(integer), new BitString([]) })
        {
            Reset(output);
            Require(Call(
                table,
                0,
                owner.Add(nonmap),
                nuint.MaxValue,
                &output[1]
            ) == 0);
            Require(output[1] == NifNativeMapValue503HostNames.Unchanged);
            Require(Call(
                table,
                0,
                owner.Add(nonmap),
                nuint.MaxValue,
                null
            ) == 0);
        }
        state.ThrowPending();
    }

    internal static void OwnersAndHints(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
        var image = new ModuleCodeImage(NifNativeMapValue503HostNames.Module);
        using var caller = new NifNativeCallEnvironment(
            context,
            image,
            0,
            state
        );
        using var source = new NifNativeCallEnvironment(context.Runtime, image);
        using var hint = new NifNativeCallEnvironment(context.Runtime, image);
        using var table = new NifNativeWindowsCallbacks();
        nuint key = caller.Add(Term.I(1));
        Term nested = Term.Tuple(new MapTerm([new(Term.I(2), Term.List(Term.I(3)))]));
        nuint map = source.Add(new MapTerm([new(Term.I(1), nested)]));
        foreach (nint env in new[] { (nint)0, (nint)1, caller.Pointer, hint.Pointer })
        {
            nuint value = 0;
            Require(Call(
                table,
                env,
                map,
                key,
                &value
            ) == 1 && source.Get(value).Equals(nested));
            Require(ReferenceEquals(source, NifNativeCallEnvironment.ResolveTermOwner(value, env)));
        }
        nuint borrowed = 0;
        Require(Call(
            table,
            0,
            map,
            key,
            &borrowed
        ) == 1);
        nuint copied = caller.Add(source.Get(borrowed));
        source.ClearIndependent();
        Require(caller.Get(copied).Equals(nested));
        try
        {
            _ = NifNativeCallEnvironment.ResolveTermOwner(borrowed, 0);
            throw new InvalidOperationException(NifNativeMapValue503HostNames.Failure);
        }
        catch (InvalidOperationException failure) when (failure.Message == NifNativeDiagnostics.ForeignTermHandle)
        {
        }
        state.ThrowPending();
    }

    internal static void PendingKeys(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
        using var owner = new NifNativeCallEnvironment(
            context,
            new ModuleCodeImage(NifNativeMapValue503HostNames.Module),
            0,
            state
        );
        using var table = new NifNativeWindowsCallbacks();
        nuint binary = 0;
        var make = (delegate* unmanaged[Cdecl]<nint, nuint, nuint*, byte*>)((nint*)table.Pointer)[NifNativeMapValue503HostNames.NewBinarySlot];
        byte* bytes = make(owner.Pointer, 2, &binary);
        Require(bytes != null && binary != 0);
        var pending = new NifNativeTermValue(new NifNativeTupleNode([
            new NifNativeTermValue(new NifNativeConsNode(owner.GetValue(binary), new NifNativeTermValue(Nil.Value)))
        ]));
        nuint nestedKey = owner.AddValue(pending);
        var first = new BitString(new byte[] { 1, 2 });
        var second = new BitString(new byte[] { 3, 4 });
        nuint simpleMap = owner.Add(new MapTerm([new(first, Term.I(11)), new(second, Term.I(22))]));
        nuint nestedMap = owner.Add(new MapTerm([
            new(Term.Tuple(Term.List(first)), Term.I(11)), new(Term.Tuple(Term.List(second)), Term.I(22))
        ]));
        foreach (int step in new[] { 1, 3, 1 })
        {
            bytes[0] = (byte)step;
            bytes[1] = (byte)(step + 1);
            nuint value = 0;
            Term expected = Term.I(step == 1 ? 11 : 22);
            Require(Call(
                table,
                0,
                simpleMap,
                binary,
                &value
            ) == 1 && owner.Get(value).Equals(expected));
            Require(Call(
                table,
                0,
                nestedMap,
                nestedKey,
                &value
            ) == 1 && owner.Get(value).Equals(expected));
        }
        state.ThrowPending();
    }

    internal static void InvalidOwnersAndThreads(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        var image = new ModuleCodeImage(NifNativeMapValue503HostNames.Module);
        using var caller = new NifNativeCallEnvironment(
            context,
            image,
            0,
            state
        );
        using var independent = new NifNativeCallEnvironment(context.Runtime, image);
        using var table = new NifNativeWindowsCallbacks();
        nuint key = caller.Add(Term.I(1));
        nuint map = caller.Add(new MapTerm([new(Term.I(1), Term.I(2))]));
        Reject(
            table,
            map,
            key,
            true
        );
        Reject(
            table,
            map,
            nuint.MaxValue,
            false
        );
        Reject(
            table,
            nuint.MaxValue,
            key,
            false
        );
        var foreignRuntime = new ProcessRuntime();
        try
        {
            using var foreignOwner = new NifNativeCallEnvironment(foreignRuntime, image);
            Reject(
                table,
                map,
                foreignOwner.Add(Term.I(1)),
                false
            );
        }
        finally
        {
            foreignRuntime.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
        nuint independentMap = independent.Add(new MapTerm([new(Term.I(1), Term.I(2))]));
        nuint independentKey = independent.Add(Term.I(1));
        Exception? workerFailure = null;
        var worker = new Thread(() =>
        {
            try
            {
                Reject(
                    table,
                    map,
                    key,
                    false
                );
                var workerState = new NifNativeFailureState();
                using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), workerState);
                nuint result = 0;
                Require(Call(
                    table,
                    0,
                    independentMap,
                    independentKey,
                    &result
                ) == 1 && independent.Get(result).Equals(Term.I(2)));
                workerState.ThrowPending();
            }
            catch (Exception failure)
            {
                workerFailure = failure;
            }
        })
        { IsBackground = true };
        worker.Start();
        Require(worker.Join(NifNativeMapValue503HostNames.Deadline));
        if (workerFailure is not null)
            throw new InvalidOperationException(NifNativeMapValue503HostNames.Failure, workerFailure);
        independent.ClearIndependent();
        Reject(
            table,
            independentMap,
            independentKey,
            false
        );
        state.ThrowPending();
    }

    private static void Reject(
        NifNativeWindowsCallbacks table,
        nuint map,
        nuint key,
        bool nullOutput
    )
    {
        var state = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
        nuint output = NifNativeMapValue503HostNames.Unchanged;
        Require(Call(
            table,
            0,
            map,
            key,
            nullOutput ? null : &output
        ) == 0);
        Require(output == NifNativeMapValue503HostNames.Unchanged && state.HostFailure is not null && !state.HasGuestException);
    }

    private static int Call(
        NifNativeWindowsCallbacks table,
        nint env,
        nuint map,
        nuint key,
        nuint* output
    )
        => ((delegate* unmanaged[Cdecl]<nint, nuint, nuint, nuint*, int>)((nint*)table.Pointer)[NifNativeMapValue503HostNames.Slot])(
            env,
            map,
            key,
            output
        );

    private static void Reset(nuint* output)
    {
        output[0] = NifNativeMapValue503HostNames.Before;
        output[1] = NifNativeMapValue503HostNames.Unchanged;
        output[2] = NifNativeMapValue503HostNames.After;
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(NifNativeMapValue503HostNames.Failure);
    }
}
