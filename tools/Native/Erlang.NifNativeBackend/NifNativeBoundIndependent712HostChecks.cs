namespace Erlang.NifNativeBackend;

internal static unsafe class NifNativeBoundIndependent712HostChecks
{
    internal static void Run(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
        using var caller = new NifNativeCallEnvironment(
            context,
            new ModuleCodeImage(NifNativeBoundIndependent712HostNames.Module),
            0,
            state
        );
        // This is the discriminator missing from the prior Lifetime control.
        using var invocation = scope.BindEnvironment(caller);
        using var table = new NifNativeWindowsCallbacks();
        table.BindIndependentEnvironments(context.Runtime);
        var slots = (nint*)table.Pointer;
        var allocate = (delegate* unmanaged[Cdecl]<nint>)slots[NifNativeEnvironmentLayout.Allocate];
        var free = (delegate* unmanaged[Cdecl]<nint, void>)slots[NifNativeEnvironmentLayout.Free];
        var copy = (delegate* unmanaged[Cdecl]<nint, nuint, nuint>)slots[NifNativeEnvironmentLayout.Copy];
        var makeInt = (delegate* unmanaged[Cdecl]<nint, int, nuint>)slots[NifNativeWindowsCallbackLayout.MakeInt];
        var makeTuple = (delegate* unmanaged[Cdecl]<nint, nuint*, uint, nuint>)slots[NifNativeArrayHostNames.TupleSlot];
        Term payload = Term.Tuple(
            Term.A(NifNativeBoundIndependent712HostNames.Payload),
            Term.I(NifNativeBoundIndependent712HostNames.Value)
        );
        nuint original = caller.Add(payload);
        nint independent = allocate();
        Require(independent != 0);
        nuint returned = 0;
        try
        {
            var owner = NifNativeCallEnvironment.Resolve(independent);
            Require(owner.IsIndependent && owner.ModuleImage709 is null
                && ReferenceEquals(owner.Runtime, context.Runtime));
            nuint copied = copy(independent, original);
            nuint generation = makeInt(independent, NifNativeBoundIndependent712HostNames.Generation);
            nuint* elements = stackalloc nuint[2];
            elements[0] = generation;
            elements[1] = copied;
            nuint tuple = makeTuple(independent, elements, 2);
            returned = copy(caller.Pointer, tuple);
        }
        finally
        {
            free(independent);
        }
        // On RED this exposes the original managed callback exception and its
        // complete host message; do not replace it with a boolean assertion.
        state.ThrowPending();
        Require(returned != 0 && caller.Get(returned).Equals(Term.Tuple(
            Term.I(NifNativeBoundIndependent712HostNames.Generation),
            payload
        )));
    }

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(NifNativeBoundIndependent712HostNames.Failure);
    }
}
