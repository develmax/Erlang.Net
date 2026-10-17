using System.Numerics;

namespace Erlang.NifNativeBackend;

internal static unsafe class NifNativeComparisonHostChecks
{
    internal static void Ordering(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        using var scope = Scope(state);
        using var env = Environment(context, state);
        using var table = new NifNativeWindowsCallbacks();
        Term[] ordered = [
            Term.I(0), Term.A(NifNativeComparisonHostNames.Atom), context.Runtime.MakeReference(),
            new FunctionTerm(0, (_, _) => ValueTask.FromResult<Term>(Nil.Value)),
            new PortTerm(NifNativeComparisonHostNames.PeerNode, 1), new Pid(NifNativeComparisonHostNames.PeerNode, 1),
            Term.Tuple(), new MapTerm([]), Nil.Value, Term.List(Term.I(0)), new BitString(new byte[] { 0 })
        ];
        for (int left = 0; left < ordered.Length; left++)
            for (int right = 0; right < ordered.Length; right++)
                Require(Compare(table, env.Add(ordered[left]), env.Add(ordered[right])) == Math.Sign(left.CompareTo(right)));
        Term a = Term.I(41), b = Term.I(42);
        for (int depth = 0; depth < NifNativeComparisonHostNames.DeepLevels; depth++)
        {
            a = Term.Tuple(a);
            b = Term.Tuple(b);
        }
        Require(Compare(table, env.Add(a), env.Add(b)) == -1);
        Require(Compare(table, env.Add(new Integer(BigInteger.One << 200)), env.Add(new FloatTerm(double.MaxValue))) == -1);
        Require(Compare(table, env.Add(Term.I(1)), env.Add(new FloatTerm(1))) == 0);
        Require(Compare(table, env.Add(Term.Tuple(Term.I(1))), env.Add(Term.Tuple(new FloatTerm(1)))) == 0);
        state.ThrowPending();
    }

    internal static void ExactEquality(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        using var scope = Scope(state);
        using var env = Environment(context, state);
        using var other = Environment(context, state);
        using var table = new NifNativeWindowsCallbacks();
        Require(Identical(table, env.Add(Term.I(1)), env.Add(new FloatTerm(1))) == 0);
        Require(Identical(table, env.Add(Term.Tuple(Term.I(1))), env.Add(Term.Tuple(new FloatTerm(1)))) == 0);
        var positive = new FloatTerm(0.0);
        var negative = new FloatTerm(BitConverter.Int64BitsToDouble(long.MinValue));
        Require(Compare(table, env.Add(positive), env.Add(negative)) == 0);
        Require(Identical(table, env.Add(positive), env.Add(negative)) == 0);
        Term a = Term.List(Term.I(42)), b = Term.List(Term.I(42));
        for (int depth = 0; depth < NifNativeComparisonHostNames.DeepLevels; depth++)
        {
            a = Term.Tuple(a);
            b = Term.Tuple(b);
        }
        Require(!ReferenceEquals(a, b) && Identical(table, env.Add(a), other.Add(b)) == 1);
        var map1 = new MapTerm([new(Term.I(1), Term.I(2)), new(new FloatTerm(1), Term.I(3))]);
        var map2 = new MapTerm([new(new FloatTerm(1), Term.I(3)), new(Term.I(1), Term.I(2))]);
        Require(Identical(table, env.Add(map1), other.Add(map2)) == 1);
        Require(Identical(table, env.Add(new BitString(new byte[] { 128 }, 1)), other.Add(new BitString(new byte[] { 128 }, 1))) == 1);
        Require(Identical(table, env.Add(new BitString(new byte[] { 128 }, 1)), other.Add(new BitString(new byte[] { 128 }, 2))) == 0);
        state.ThrowPending();
    }

    internal static void PendingReads(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        using var scope = Scope(state);
        using var env = Environment(context, state);
        using var table = new NifNativeWindowsCallbacks();
        nuint pending;
        byte* data = ((delegate* unmanaged[Cdecl]<nint, nuint, nuint*, byte*>)Slot(table, NifNativeComparisonHostNames.NewBinarySlot))(env.Pointer, 2, &pending);
        data[0] = 1;
        data[1] = 2;
        nuint original = env.Add(new BitString(new byte[] { 1, 2 }));
        nuint later = env.Add(new BitString(new byte[] { 3, 2 }));
        nuint tuple = ((delegate* unmanaged[Cdecl]<nint, nuint*, uint, nuint>)Slot(table, NifNativeComparisonHostNames.TupleSlot))(env.Pointer, &pending, 1);
        nuint list = ((delegate* unmanaged[Cdecl]<nint, nuint*, uint, nuint>)Slot(table, NifNativeComparisonHostNames.ListSlot))(env.Pointer, &pending, 1);
        Require(Identical(table, pending, original) == 1 && Compare(table, pending, original) == 0);
        Require(Identical(table, tuple, env.Add(Term.Tuple(new BitString(new byte[] { 1, 2 })))) == 1);
        data[0] = 3;
        Require(Identical(table, pending, original) == 0 && Compare(table, pending, original) == 1);
        Require(Identical(table, pending, later) == 1 && Compare(table, pending, later) == 0);
        Require(Identical(table, tuple, env.Add(Term.Tuple(new BitString(new byte[] { 3, 2 })))) == 1);
        Require(Identical(table, list, env.Add(Term.List(new BitString(new byte[] { 3, 2 })))) == 1);
        data[0] = 1;
        Require(Identical(table, pending, original) == 1 && Compare(table, pending, original) == 0);
        Require(env.Get(tuple).Equals(Term.Tuple(new BitString(new byte[] { 1, 2 }))));
        state.ThrowPending();
    }

    internal static void ReferenceUniqueness(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        using var scope = Scope(state);
        using var env = Environment(context, state);
        using var other = Environment(context, state);
        using var table = new NifNativeWindowsCallbacks();
        var references = new HashSet<Term> { context.Runtime.MakeReference() };
        for (int index = 0; index < NifNativeComparisonHostNames.ReferenceCount; index++)
        {
            var owner = index % 2 == 0 ? env : other;
            nuint handle = MakeReference(table, owner.Pointer);
            Require(handle != 0 && owner.Get(handle) is ReferenceTerm);
            Term reference = owner.Get(handle);
            Require(references.Add(reference) && Identical(table, handle, env.Add(reference)) == 1);
            Require(((ReferenceTerm)reference).Node == context.Runtime.Node);
        }
        Require(references.Add(context.Runtime.MakeReference()));
        state.ThrowPending();
    }

    internal static void OwnershipBoundaries(ProcessContext context)
    {
        using var table = new NifNativeWindowsCallbacks();
        var state = new NifNativeFailureState();
        using var scope = Scope(state);
        using var env = Environment(context, state);
        nuint valid = env.Add(Term.I(7));
        nuint expired;
        using (var old = Environment(context, state))
            expired = old.Add(Term.I(7));
        Require(Compare(table, expired, valid) == 0 && state.HostFailure is not null);
        var next = new NifNativeFailureState();
        using (var inner = Scope(next))
            Require(Identical(table, expired, expired) == 0 && next.HostFailure is not null);
        var nullEnvironment = new NifNativeFailureState();
        using (var inner = Scope(nullEnvironment))
            Require(MakeReference(table, 0) == 0 && nullEnvironment.HostFailure is not null);
        var worker = new NifNativeFailureState();
        Exception? threadFailure = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var local = Scope(worker);
                Require(Compare(table, valid, valid) == 0 && worker.HostFailure is not null);
            }
            catch (Exception failure)
            {
                threadFailure = failure;
            }
        })
        { IsBackground = true };
        thread.Start();
        Require(thread.Join(TimeSpan.FromSeconds(5)) && threadFailure is null);
    }

    private static NifNativeCallbackScope Scope(NifNativeFailureState state) => new(new NifNativeAllocationOwner(), state);

    private static NifNativeCallEnvironment Environment(ProcessContext context, NifNativeFailureState state)
        => new(
            context,
            new ModuleCodeImage(NifNativeComparisonHostNames.Module),
            0,
            state
        );

    private static nint Slot(NifNativeWindowsCallbacks table, int slot) => ((nint*)table.Pointer)[slot];

    private static int Compare(NifNativeWindowsCallbacks table, nuint left, nuint right)
        => ((delegate* unmanaged[Cdecl]<nuint, nuint, int>)Slot(table, NifNativeComparisonHostNames.CompareSlot))(left, right);

    private static int Identical(NifNativeWindowsCallbacks table, nuint left, nuint right)
        => ((delegate* unmanaged[Cdecl]<nuint, nuint, int>)Slot(table, NifNativeComparisonHostNames.IdenticalSlot))(left, right);

    private static nuint MakeReference(NifNativeWindowsCallbacks table, nint env)
        => ((delegate* unmanaged[Cdecl]<nint, nuint>)Slot(table, NifNativeComparisonHostNames.ReferenceSlot))(env);

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(NifNativeComparisonHostNames.Failure);
    }
}
