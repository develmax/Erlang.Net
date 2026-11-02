namespace Erlang.NifNativeBackend;

internal static unsafe class NifNativeReferenceEnvironmentHostChecks
{
    internal static void Lifetime(ProcessContext context)
    {
        var image = new ModuleCodeImage(NifNativeReferenceEnvironmentHostNames.Module);
        var state = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
        using var caller = new NifNativeCallEnvironment(
            context,
            image,
            0,
            state
        );
        using (var callback = NifNativeCallEnvironment.CreateResourceCallback524(
            context.Runtime,
            image,
            caller.PrivateData,
            state
        ))
        {
            Require(callback.IsIndependent && ReferenceEquals(callback.ModuleImage709, image));
        }
        using var table = new NifNativeWindowsCallbacks();
        table.BindIndependentEnvironments(context.Runtime);
        using (var nextTable = new NifNativeWindowsCallbacks())
        {
            nextTable.BindIndependentEnvironments(context.Runtime);
            Require(Slot(table, NifNativeEnvironmentLayout.Allocate)
                == Slot(nextTable, NifNativeEnvironmentLayout.Allocate));
        }
        nint independent = Allocate(table);
        Require(independent != 0);
        var moduleless = NifNativeCallEnvironment.Resolve(independent);
        Require(moduleless.IsIndependent && moduleless.ModuleImage709 is null
            && moduleless.PrivateData == 0 && ReferenceEquals(moduleless.Runtime, context.Runtime));
        nuint copy = 0;
        Term? retained = null;
        try
        {
            nuint original = Make(table, independent);
            copy = Copy(table, caller.Pointer, original);
            retained = caller.Get(copy);
            Require(retained is ReferenceTerm reference && reference.Node == context.Runtime.Node);
            Require(retained.Equals(NifNativeCallEnvironment.Resolve(independent).Get(original)));
            Clear(table, independent);
            nuint next = Make(table, independent);
            Require(!retained.Equals(NifNativeCallEnvironment.Resolve(independent).Get(next)));
            Require(caller.Get(copy).Equals(retained));
        }
        finally
        {
            Free(table, independent);
        }
        Require(retained is not null && caller.Get(copy).Equals(retained));
        state.ThrowPending();
    }

    internal static void ForeignThreads(ProcessContext context)
    {
        using var table = new NifNativeWindowsCallbacks();
        table.BindIndependentEnvironments(context.Runtime);
        nint independent = 0;
        nuint first = 0;
        nuint second = 0;
        Exception? failure = null;
        var creator = new Thread(() =>
        {
            try
            {
                independent = Allocate(table);
                first = Make(table, independent);
            }
            catch (Exception error)
            {
                failure = error;
            }
        })
        { IsBackground = true };
        creator.Start();
        Require(creator.Join(NifNativeReferenceEnvironmentHostNames.Deadline) && failure is null && independent != 0 && first != 0);
        try
        {
            var successor = new Thread(() =>
            {
                try
                {
                    second = Make(table, independent);
                    Require(NifNativeCallEnvironment.Resolve(independent).Get(first) is ReferenceTerm);
                }
                catch (Exception error)
                {
                    failure = error;
                }
            })
            { IsBackground = true };
            successor.Start();
            Require(successor.Join(NifNativeReferenceEnvironmentHostNames.Deadline) && failure is null && second != 0);
            var owner = NifNativeCallEnvironment.Resolve(independent);
            Require(owner.ModuleImage709 is null && ReferenceEquals(owner.Runtime, context.Runtime));
            Require(owner.Get(first) is ReferenceTerm firstReference && firstReference.Node == context.Runtime.Node);
            Require(!owner.Get(first).Equals(owner.Get(second)));
            Require(!owner.Get(first).Equals(context.Runtime.MakeReference()));
        }
        finally
        {
            Free(table, independent);
        }
    }

    private static nint Slot(NifNativeWindowsCallbacks table, int index) => ((nint*)table.Pointer)[index];

    private static nint Allocate(NifNativeWindowsCallbacks table)
        => ((delegate* unmanaged[Cdecl]<nint>)Slot(table, NifNativeEnvironmentLayout.Allocate))();

    private static nuint Make(NifNativeWindowsCallbacks table, nint env)
        => ((delegate* unmanaged[Cdecl]<nint, nuint>)Slot(table, NifNativeWindowsCallbackLayout.MakeReference))(env);

    private static nuint Copy(NifNativeWindowsCallbacks table, nint env, nuint value)
        => ((delegate* unmanaged[Cdecl]<nint, nuint, nuint>)Slot(table, NifNativeEnvironmentLayout.Copy))(env, value);

    private static void Clear(NifNativeWindowsCallbacks table, nint env)
        => ((delegate* unmanaged[Cdecl]<nint, void>)Slot(table, NifNativeEnvironmentLayout.Clear))(env);

    private static void Free(NifNativeWindowsCallbacks table, nint env)
        => ((delegate* unmanaged[Cdecl]<nint, void>)Slot(table, NifNativeEnvironmentLayout.Free))(env);

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(NifNativeReferenceEnvironmentHostNames.Failure);
    }
}
