namespace Erlang.NifNativeBackend;

internal static unsafe class NifNativeMapCreation501HostChecks
{
    internal static void CreationAndOwnership(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
        var image = new ModuleCodeImage(NifNativeMapCreation501HostNames.Module);
        using var caller = new NifNativeCallEnvironment(
            context,
            image,
            0,
            state
        );
        using var table = new NifNativeWindowsCallbacks();
        table.BindIndependentEnvironments(context.Runtime, image);
        nuint first = Make(table, caller.Pointer);
        nuint second = Make(table, caller.Pointer);
        Require(first != 0 && second != 0 && first != second);
        Require(caller.Get(first) is MapTerm { Entries.Count: 0 });
        Require(caller.Get(first).Equals(caller.Get(second)));
        nuint size = nuint.MaxValue;
        var isMap = (delegate* unmanaged[Cdecl]<nint, nuint, int>)Slot(table, NifNativeMapCreation501HostNames.IsMapSlot);
        var getSize = (delegate* unmanaged[Cdecl]<nint, nuint, nuint*, int>)Slot(table, NifNativeMapCreation501HostNames.SizeSlot);
        Require(isMap(caller.Pointer, first) == 1 && getSize(caller.Pointer, first, &size) == 1 && size == 0);
        using var independent = new NifNativeCallEnvironment(context.Runtime, image);
        nuint original = Make(table, independent.Pointer);
        var copy = (delegate* unmanaged[Cdecl]<nint, nuint, nuint>)Slot(table, NifNativeEnvironmentLayout.Copy);
        nuint copied = copy(caller.Pointer, original);
        var clear = (delegate* unmanaged[Cdecl]<nint, void>)Slot(table, NifNativeEnvironmentLayout.Clear);
        clear(independent.Pointer);
        Require(caller.Get(copied) is MapTerm { Entries.Count: 0 });
        Require(Make(table, independent.Pointer) != original);
        state.ThrowPending();
    }

    internal static void InvalidEnvironments(ProcessContext context)
    {
        using var table = new NifNativeWindowsCallbacks();
        foreach (nint address in new nint[] { 0, 1 })
            Reject(table, address);
        var retired = new NifNativeCallEnvironment(context.Runtime, new ModuleCodeImage(NifNativeMapCreation501HostNames.Module));
        nint retiredPointer = retired.Pointer;
        retired.Dispose();
        Reject(table, retiredPointer);
    }

    internal static void ThreadAccess(ProcessContext context)
    {
        var image = new ModuleCodeImage(NifNativeMapCreation501HostNames.Module);
        var state = new NifNativeFailureState();
        using var caller = new NifNativeCallEnvironment(
            context,
            image,
            0,
            state
        );
        using var independent = new NifNativeCallEnvironment(context.Runtime, image);
        using var table = new NifNativeWindowsCallbacks();
        Exception? error = null;
        var worker = new Thread(() =>
        {
            try
            {
                Reject(table, caller.Pointer);
                var workerState = new NifNativeFailureState();
                using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), workerState);
                nuint handle = Make(table, independent.Pointer);
                Require(handle != 0 && independent.Get(handle) is MapTerm { Entries.Count: 0 });
                workerState.ThrowPending();
            }
            catch (Exception failure)
            {
                error = failure;
            }
        })
        { IsBackground = true };
        worker.Start();
        Require(worker.Join(NifNativeMapCreation501HostNames.Deadline));
        if (error is not null)
            throw new InvalidOperationException(NifNativeMapCreation501HostNames.Failure, error);
        state.ThrowPending();
    }

    private static void Reject(NifNativeWindowsCallbacks table, nint env)
    {
        var state = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
        Require(Make(table, env) == 0 && state.HostFailure?.SourceException is InvalidOperationException);
        Require(!state.HasGuestException);
    }

    private static nuint Make(NifNativeWindowsCallbacks table, nint env)
        => ((delegate* unmanaged[Cdecl]<nint, nuint>)Slot(table, NifNativeMapCreation501HostNames.MakeSlot))(env);

    private static nint Slot(NifNativeWindowsCallbacks table, int slot) => ((nint*)table.Pointer)[slot];

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(NifNativeMapCreation501HostNames.Failure);
    }
}
