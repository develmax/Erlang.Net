using System.Runtime.InteropServices;
using static Erlang.NifNativeBackend.NifConsCell539HostNames;

namespace Erlang.NifNativeBackend;

internal static unsafe class NifConsCell539HostChecks
{
    internal static void ShapeAndAbi(ProcessContext context)
    {
        Require(Marshal.SizeOf<NifNativeBridgeTable>() == 464);
        Require((int)Marshal.OffsetOf<NifNativeBridgeTable>(nameof(NifNativeBridgeTable.MakeListCell539)) == 456);
        Require(NifNativeNames.BridgeVersion == 7);
        var state = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
        using var env = new NifNativeCallEnvironment(
            context,
            new ModuleCodeImage(Module),
            0,
            state
        );
        using var table = new NifNativeWindowsCallbacks();
        Require(((nint*)table.Pointer)[Slot] != 0);
        foreach (Term head in new Term[] { Term.I(1), Nil.Value, Term.Tuple(Term.I(2)), new BitString([3]) })
            foreach (Term tail in new Term[] { Nil.Value, Term.List(Term.I(4)), Term.I(5), Term.Tuple(Term.I(6)) })
            {
                nuint cell = Create(
                    table,
                    env.Pointer,
                    env.Add(head),
                    env.Add(tail)
                );
                Require(cell != 0 && env.Get(cell).Equals(new Cons(head, tail)));
                nuint actualHead = 0, actualTail = 0;
                Require(((delegate* unmanaged[Cdecl]<nint, nuint, nuint*, nuint*, int>)((nint*)table.Pointer)[13])(
                    env.Pointer,
                    cell,
                    &actualHead,
                    &actualTail
                ) == 1);
                Require(env.Get(actualHead).Equals(head) && env.Get(actualTail).Equals(tail));
            }
        state.ThrowPending();
    }

    internal static void PendingHeadAndImproperTail(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
        using var env = new NifNativeCallEnvironment(
            context,
            new ModuleCodeImage(Module),
            0,
            state
        );
        using var table = new NifNativeWindowsCallbacks();
        NifNativeBinaryStorage storage = env.CreateBinaryStorage(2);
        storage.Pointer[0] = 1;
        storage.Pointer[1] = 2;
        var binary = new NifNativeTermValue(new NifNativeBinaryNode(storage, 0, 2));
        nuint head = env.AddValue(binary);
        nuint tail = env.AddValue(new(new NifNativeTupleNode([binary])));
        nuint cell = Create(
            table,
            env.Pointer,
            head,
            tail
        );
        Require(env.GetValue(cell).Pending is NifNativeConsNode);
        storage.Pointer[0] = 9;
        Term expected = new Cons(new BitString([9, 2]), Term.Tuple(new BitString([9, 2])));
        Require(env.Get(cell).Equals(expected));
        head = env.Add(Term.I(99)); // C variable reassignment is not child mutation.
        tail = env.Add(Nil.Value);
        nuint next = Create(
            table,
            env.Pointer,
            head,
            tail
        );
        Require(env.Get(next).Equals(Term.List(Term.I(99))) && env.Get(cell).Equals(expected));
        state.ThrowPending();
    }

    internal static void InvalidOwnersAndThreads(ProcessContext context)
    {
        var image = new ModuleCodeImage(Module);
        var state = new NifNativeFailureState();
        using var owner = new NifNativeCallEnvironment(
            context,
            image,
            0,
            state
        );
        using var independent = new NifNativeCallEnvironment(context.Runtime, image);
        using var table = new NifNativeWindowsCallbacks();
        nuint head = owner.Add(Term.I(1)), tail = owner.Add(Nil.Value);
        Reject(
            table,
            0,
            head,
            tail
        );
        Reject(
            table,
            owner.Pointer,
            nuint.MaxValue,
            tail
        );
        Reject(
            table,
            owner.Pointer,
            head,
            independent.Add(Nil.Value)
        );
        nuint independentHead = independent.Add(Term.I(2));
        nuint independentTail = independent.Add(Term.I(3));
        Exception? failure = null;
        var worker = new Thread(() =>
        {
            try
            {
                Reject(
                    table,
                    owner.Pointer,
                    head,
                    tail
                );
                var local = new NifNativeFailureState();
                using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), local);
                nuint cell = Create(
                    table,
                    independent.Pointer,
                    independentHead,
                    independentTail
                );
                Require(cell != 0 && independent.Get(cell).Equals(new Cons(Term.I(2), Term.I(3))));
                local.ThrowPending();
            }
            catch (Exception error)
            {
                failure = error;
            }
        })
        { IsBackground = true };
        worker.Start();
        Require(worker.Join(Deadline));
        if (failure is not null) throw new InvalidOperationException(Failure, failure);
        independent.ClearIndependent();
        Reject(
            table,
            independent.Pointer,
            independentHead,
            independentTail
        );
    }

    private static void Reject(
        NifNativeWindowsCallbacks table,
        nint env,
        nuint head,
        nuint tail
    )
    {
        var state = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
        Require(Create(
            table,
            env,
            head,
            tail
        ) == 0 && state.HostFailure is not null && !state.HasGuestException);
    }

    private static nuint Create(
        NifNativeWindowsCallbacks table,
        nint env,
        nuint head,
        nuint tail
    )
        => ((delegate* unmanaged[Cdecl]<nint, nuint, nuint, nuint>)((nint*)table.Pointer)[Slot])(env, head, tail);

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }
}
