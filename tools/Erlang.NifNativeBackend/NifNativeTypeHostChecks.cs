using System.Numerics;
using System.Runtime.ExceptionServices;

namespace Erlang.NifNativeBackend;

internal static class NifNativeTypeHostChecks
{
    internal static async Task Run(Action<ProcessContext> check)
    {
        int active = NifNativeCallEnvironment.ActiveCount;
        await using var runtime = new ProcessRuntime();
        Exception? failure = null;
        ProcessHandle actor = runtime.Spawn(context =>
        {
            try
            {
                check(context);
            }
            catch (Exception error)
            {
                failure = error;
            }

            return ValueTask.FromResult<Term>(Nil.Value);
        });
        Term reason = await actor.Completion.WaitAsync(NifNativeTypeHostNames.Timeout);
        if (failure is not null)
            ExceptionDispatchInfo.Capture(failure).Throw();
        Require(reason.Equals(Term.A(ProcessExitReasons.Normal)));
        Require(NifNativeCallEnvironment.ActiveCount == active);
    }

    internal static unsafe void Classification(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
        using var environment = Environment(context, state);
        using var table = new NifNativeWindowsCallbacks();
        Atom atom = Term.A(NifNativeTypeHostNames.Atom);
        FunctionTerm localFunction = new(
            new LocalFunctionIdentity(
                atom,
                atom,
                1,
                2,
                new BitString(new byte[16])
            ),
            0,
            [],
            NeverInvoke
        );
        FunctionTerm externalFunction = new(new ExternalFunctionIdentity(atom, atom, 0), NeverInvoke);
        Term[] values = [
            atom,
            Term.I(1),
            new Integer(BigInteger.One << 200),
            new FloatTerm(0.5),
            Term.Tuple(),
            Nil.Value,
            new Cons(atom, Nil.Value),
            new Cons(atom, atom),
            new MapTerm([]),
            context.Self,
            new Pid(NifNativeTypeHostNames.ExternalNode, 1),
            context.Runtime.RehydratePort(new PortTerm(context.Self.Node, 1, context.Self.Creation)),
            new PortTerm(NifNativeTypeHostNames.ExternalNode, 1),
            context.Runtime.MakeReference(),
            new ReferenceTerm(NifNativeTypeHostNames.ExternalNode, 1),
            context.Runtime.ReferenceResources.CreateBinary(new byte[] { 1 }),
            new FunctionTerm(0, NeverInvoke),
            localFunction,
            externalFunction,
            new BitString([]),
            new BitString(new byte[] { 128 }),
            new BitString(new byte[] { 128 }, 1),
            new NativeRecordTerm(
                atom,
                atom,
                true,
                []
            )
        ];
        // Expectations are keyed to original Erlang term categories, independent of callback implementation.
        int[][] expected = [
            [3], [104], [104], [104], [83], [74, 82], [82], [82], [108],
            [66], [66], [67], [67], [5], [5], [5], [65], [65], [65], [4], [4], [], []
        ];
        Require(values.Length == expected.Length);
        for (int row = 0; row < values.Length; row++)
        {
            nuint handle = environment.Add(values[row]);
            foreach (int slot in NifNativeTypeHostNames.Slots)
                Require(Predicate(
                    table,
                    slot,
                    environment.Pointer,
                    handle
                ) == (expected[row].Contains(slot) ? 1 : 0));
        }
        state.ThrowPending();
    }

    internal static unsafe void EnvironmentIndependence(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
        using var owner = Environment(context, state);
        using var other = Environment(context, new NifNativeFailureState());
        using var table = new NifNativeWindowsCallbacks();
        nuint atom = owner.Add(Term.A(NifNativeTypeHostNames.Atom));
        foreach (int slot in NifNativeTypeHostNames.Slots)
        {
            int expected = slot == 3 ? 1 : 0;
            Require(Predicate(
                table,
                slot,
                0,
                atom
            ) == expected);
            Require(Predicate(
                table,
                slot,
                other.Pointer,
                atom
            ) == expected);
            Require(Predicate(
                table,
                slot,
                (nint)1,
                atom
            ) == expected);
        }
        // A normal valid-env predicate call needs no snapshot/allocation in the owner lookup.
        for (int index = 0; index < 512; index++)
            Require(Predicate(
                table,
                3,
                owner.Pointer,
                atom
            ) == 1);
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int index = 0; index < 1000; index++)
            Require(Predicate(
                table,
                3,
                owner.Pointer,
                atom
            ) == 1);
        Require(GC.GetAllocatedBytesForCurrentThread() == before);
        nuint list = owner.Add(Term.List(Term.I(7)));
        nuint head = 0, tail = 0;
        Require(Cell(
            table,
            0,
            list,
            &head,
            &tail
        ) == 1);
        Require(owner.Get(head).Equals(Term.I(7)) && owner.Get(tail) is Nil);
        Require(Cell(
            table,
            other.Pointer,
            list,
            &head,
            &tail
        ) == 1);
        Require(owner.Get(head).Equals(Term.I(7)) && owner.Get(tail) is Nil);
        state.ThrowPending();
    }

    internal static unsafe void ListCells(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
        using var environment = Environment(context, state);
        using var table = new NifNativeWindowsCallbacks();
        Term headTerm = Term.Tuple(Term.I(7));
        foreach (Term tailTerm in new Term[] { Nil.Value, Term.I(19), Term.List(Term.I(23)) })
        {
            nuint list = environment.Add(new Cons(headTerm, tailTerm));
            nuint head = 0, tail = 0;
            Require(Cell(
                table,
                environment.Pointer,
                list,
                &head,
                &tail
            ) == 1);
            Require(ReferenceEquals(environment.Get(head), headTerm));
            Require(ReferenceEquals(environment.Get(tail), tailTerm));
        }
        foreach (Term value in new Term[] { Nil.Value, Term.I(1), Term.Tuple(), new BitString([]) })
        {
            nuint token = environment.Add(value);
            nuint head = 123, tail = 456;
            Require(Cell(
                table,
                0,
                token,
                &head,
                &tail
            ) == 0 && head == 123 && tail == 456);
            Require(Cell(
                table,
                0,
                token,
                null,
                null
            ) == 0);
            Require(Cell(
                table,
                0,
                token,
                &head,
                null
            ) == 0 && head == 123);
            Require(Cell(
                table,
                0,
                token,
                null,
                &tail
            ) == 0 && tail == 456);
        }
        state.ThrowPending();
    }

    internal static unsafe void Containment(ProcessContext context)
    {
        using var table = new NifNativeWindowsCallbacks();
        foreach (int slot in NifNativeTypeHostNames.Slots.Append(NifNativeTypeHostNames.CellSlot))
        {
            var state = new NifNativeFailureState();
            using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
            var expired = Environment(context, state);
            nuint token = expired.Add(Term.List(Term.I(1)));
            expired.Dispose();
            using var active = Environment(context, state);
            nuint head = 123, tail = 456;
            int result = slot == NifNativeTypeHostNames.CellSlot
                ? Cell(
                    table,
                    active.Pointer,
                    token,
                    &head,
                    &tail
                )
                : Predicate(
                    table,
                    slot,
                    active.Pointer,
                    token
                );
            Require(result == 0 && head == 123 && tail == 456);
            Require(state.HostFailure?.SourceException is InvalidOperationException);
            Require(!state.HasGuestException);
        }
        foreach (bool nullHead in new[] { false, true })
        {
            var state = new NifNativeFailureState();
            using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
            using var environment = Environment(context, state);
            nuint token = environment.Add(Term.List(Term.I(1)));
            nuint output = 123;
            Require(Cell(
                table,
                0,
                token,
                nullHead ? null : &output,
                nullHead ? &output : null
            ) == 0);
            Require(output == 123 && !state.HasGuestException);
            Require(state.HostFailure?.SourceException is InvalidOperationException error && error.Message == NifNativeListCellNames.NullOutput);
        }
    }

    private static unsafe int Predicate(
        NifNativeWindowsCallbacks table,
        int slot,
        nint env,
        nuint value
    )
        => ((delegate* unmanaged[Cdecl]<nint, nuint, int>)((nint*)table.Pointer)[slot])(env, value);

    private static unsafe int Cell(
        NifNativeWindowsCallbacks table,
        nint env,
        nuint value,
        nuint* head,
        nuint* tail
    )
        => ((delegate* unmanaged[Cdecl]<nint, nuint, nuint*, nuint*, int>)((nint*)table.Pointer)[NifNativeTypeHostNames.CellSlot])(
            env,
            value,
            head,
            tail
        );

    private static NifNativeCallEnvironment Environment(ProcessContext context, NifNativeFailureState state)
        => new(
            context,
            new ModuleCodeImage(NifNativeTypeHostNames.Module),
            0,
            state
        );

    private static ValueTask<Term> NeverInvoke(ITermExecutionContext context, IReadOnlyList<Term> arguments)
        => throw new InvalidOperationException(NifNativeTypeHostNames.Failure);

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(NifNativeTypeHostNames.Failure);
    }
}
