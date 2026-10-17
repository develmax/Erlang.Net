using System.Runtime.ExceptionServices;
using System.Text;

namespace Erlang.NifNativeBackend;

internal static class NifNativeSequenceTextHostChecks
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
        Term reason = await actor.Completion.WaitAsync(NifNativeSequenceTextHostNames.Timeout);
        if (failure is not null)
            ExceptionDispatchInfo.Capture(failure).Throw();
        Require(reason.Equals(Term.A(ProcessExitReasons.Normal)));
        Require(NifNativeCallEnvironment.ActiveCount == active);
    }

    internal static unsafe void TupleLifetime(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
        var owner = Environment(context, state);
        using var table = new NifNativeWindowsCallbacks();
        var get = (delegate* unmanaged[Cdecl]<nint, nuint, int*, nuint**, int>)Slots(table)[NifNativeSequenceTextHostNames.Tuple];
        try
        {
            Term shared = Term.List(Term.I(7));
            var tuple = Term.Tuple(shared, Nil.Value, shared);
            nuint token = owner.Add(tuple);
            nuint* elements = null;
            int arity = -1;
            Require(get(
                0,
                token,
                &arity,
                &elements
            ) == 1 && arity == 3 && elements != null);
            Require(ReferenceEquals(owner.Get(elements[0]), shared));
            Require(owner.Get(elements[1]) is Nil && ReferenceEquals(owner.Get(elements[2]), shared));
            nuint* first = elements;
            Require(get(
                (nint)1,
                token,
                &arity,
                &elements
            ) == 1 && elements == first);
            Require(owner.BorrowedTupleArrayCount == 1);
            nuint empty = owner.Add(Term.Tuple());
            Require(get(
                0,
                empty,
                &arity,
                &elements
            ) == 1 && arity == 0 && elements != null);
            // Additional tuple allocations must not move or invalidate the previously returned array.
            for (int index = 0; index < 128; index++)
            {
                nuint another = owner.Add(Term.Tuple(Term.I(index)));
                Require(get(
                    0,
                    another,
                    &arity,
                    &elements
                ) == 1);
            }
            Require(ReferenceEquals(owner.Get(first[0]), shared));
            Require(owner.Get(first[1]) is Nil && ReferenceEquals(owner.Get(first[2]), shared));
            arity = 123;
            elements = first;
            Require(get(
                0,
                owner.Add(Nil.Value),
                &arity,
                &elements
            ) == 0 && arity == 123 && elements == first);
            Require(get(
                0,
                owner.Add(Nil.Value),
                null,
                null
            ) == 0);
            state.ThrowPending();
        }
        finally
        {
            owner.Dispose();
        }
        Require(owner.BorrowedTupleArrayCount == 0);
    }

    internal static unsafe void ListLengths(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
        using var owner = Environment(context, state);
        using var table = new NifNativeWindowsCallbacks();
        var get = (delegate* unmanaged[Cdecl]<nint, nuint, uint*, int>)Slots(table)[NifNativeSequenceTextHostNames.ListLength];
        foreach (int count in new[] { 0, 1, 3, 8193 })
        {
            Term list = Cons.From(Enumerable.Repeat<Term>(Nil.Value, count));
            uint output = uint.MaxValue;
            Require(get(0, owner.Add(list), &output) == 1 && output == count);
        }
        foreach (Term value in new Term[] { Term.I(1), Term.Tuple(), new Cons(Nil.Value, Term.I(1)) })
        {
            uint output = uint.MaxValue;
            nuint token = owner.Add(value);
            Require(get(0, token, &output) == 0 && output == uint.MaxValue);
            Require(get(0, token, null) == 0);
        }
        state.ThrowPending();
    }

    internal static unsafe void Atoms(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
        using var owner = Environment(context, state);
        using var binding = scope.BindEnvironment(owner);
        using var table = new NifNativeWindowsCallbacks();
        var make = (delegate* unmanaged[Cdecl]<nint, byte*, nuint>)Slots(table)[NifNativeSequenceTextHostNames.AtomMake];
        var makeLength = (delegate* unmanaged[Cdecl]<nint, byte*, nuint, nuint>)Slots(table)[NifNativeSequenceTextHostNames.AtomMakeLength];
        var existing = (delegate* unmanaged[Cdecl]<nint, byte*, nuint*, int, int>)Slots(table)[NifNativeSequenceTextHostNames.AtomExisting];
        var existingLength = (delegate* unmanaged[Cdecl]<nint, byte*, nuint, nuint*, int, int>)Slots(table)[NifNativeSequenceTextHostNames.AtomExistingLength];
        byte[] input = [97, 0, 98, 0];
        fixed (byte* bytes = input)
        {
            Require(owner.Get(make(0, bytes)).Equals(Term.A(NifNativeSequenceTextHostNames.SingleAtom)));
            nuint made = makeLength(0, bytes, 3);
            Require(owner.Get(made) is Atom atom && atom.Name == NifNativeSequenceTextHostNames.EmbeddedZeroAtom);
            nuint output = 0;
            Require(existing(
                0,
                bytes,
                &output,
                NifNativeSequenceTextHostNames.Latin1
            ) == 1 && owner.Get(output).Equals(Term.A(NifNativeSequenceTextHostNames.SingleAtom)));
            Require(existingLength(
                (nint)1,
                bytes,
                3,
                &output,
                NifNativeSequenceTextHostNames.Latin1
            ) == 1 && owner.Get(output).Equals(owner.Get(made)));
        }
        Require(owner.Get(makeLength(0, null, 0)).Equals(Term.A(string.Empty)));
        byte[] maximum = Enumerable.Repeat((byte)120, 255).ToArray();
        fixed (byte* bytes = maximum)
            Require(owner.Get(makeLength(0, bytes, (nuint)maximum.Length)) is Atom atom && atom.Name.Length == maximum.Length);
        foreach (string text in new[] { string.Empty, NifNativeSequenceTextHostNames.Atom, NifNativeSequenceTextHostNames.UnicodeAtom })
        {
            Atom atom = Term.A(text);
            nuint token = owner.Add(atom);
            byte[] utf8 = Encoding.UTF8.GetBytes(text);
            ProbeAtom(
                table,
                token,
                NifNativeSequenceTextHostNames.Utf8,
                utf8
            );
            bool latin1 = text.All(character => character <= byte.MaxValue);
            ProbeAtom(
                table,
                token,
                NifNativeSequenceTextHostNames.Latin1,
                latin1 ? Encoding.Latin1.GetBytes(text) : null
            );
            fixed (byte* bytes = utf8)
            {
                nuint output = 0;
                Require(existingLength(
                    0,
                    bytes,
                    (nuint)utf8.Length,
                    &output,
                    NifNativeSequenceTextHostNames.Utf8
                ) == 1 && owner.Get(output).Equals(atom));
            }
        }
        byte[] absent = Encoding.ASCII.GetBytes(NifNativeSequenceTextHostNames.AbsentAtom);
        int before = AtomNameRegistry.Count;
        Require(!AtomNameRegistry.Exists(NifNativeSequenceTextHostNames.AbsentAtom));
        fixed (byte* bytes = absent)
        {
            nuint output = 123;
            Require(existingLength(
                0,
                bytes,
                (nuint)absent.Length,
                &output,
                NifNativeSequenceTextHostNames.Utf8
            ) == 0 && output == 123);
            Require(existingLength(
                0,
                bytes,
                (nuint)absent.Length,
                null,
                NifNativeSequenceTextHostNames.Utf8
            ) == 0);
        }
        byte[] invalid = [192, 175];
        fixed (byte* bytes = invalid)
        {
            nuint output = 123;
            Require(existingLength(
                0,
                bytes,
                2,
                &output,
                NifNativeSequenceTextHostNames.Utf8
            ) == 0 && output == 123);
        }
        Require(AtomNameRegistry.Count == before);
        state.ThrowPending();
    }

    internal static unsafe void Strings(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
        using var owner = Environment(context, state);
        using var table = new NifNativeWindowsCallbacks();
        var make = (delegate* unmanaged[Cdecl]<nint, byte*, nuint, int, nuint>)Slots(table)[NifNativeSequenceTextHostNames.StringMakeLength];
        byte[] latin1 = [0, 65, 233];
        fixed (byte* bytes = latin1)
            Require(owner.Get(make(
                owner.Pointer,
                bytes,
                3,
                NifNativeSequenceTextHostNames.Latin1
            )).Equals(Term.List(Term.I(0), Term.I(65), Term.I(233))));
        byte[] unicode = Encoding.UTF8.GetBytes(NifNativeSequenceTextHostNames.UnicodeAtom);
        fixed (byte* bytes = unicode)
            Require(owner.Get(make(
                owner.Pointer,
                bytes,
                (nuint)unicode.Length,
                NifNativeSequenceTextHostNames.Utf8
            )).Equals(Term.String(NifNativeSequenceTextHostNames.UnicodeAtom)));
        Require(owner.Get(make(
            owner.Pointer,
            null,
            0,
            NifNativeSequenceTextHostNames.Utf8
        )) is Nil);
        Term ascii = Term.List(Term.I(65), Term.I(66));
        foreach (int encoding in new[] { NifNativeSequenceTextHostNames.Latin1, NifNativeSequenceTextHostNames.Utf8 })
        {
            ProbeString(
                table,
                owner.Add(Nil.Value),
                1,
                encoding,
                1,
                [0]
            );
            ProbeString(
                table,
                owner.Add(ascii),
                3,
                encoding,
                3,
                [65, 66, 0]
            );
            ProbeString(
                table,
                owner.Add(ascii),
                2,
                encoding,
                -2,
                [65, 0]
            );
            ProbeString(
                table,
                owner.Add(ascii),
                1,
                encoding,
                -1,
                [0]
            );
            ProbeString(
                table,
                owner.Add(Term.I(1)),
                4,
                encoding,
                0,
                [0]
            );
            ProbeString(
                table,
                owner.Add(new Cons(Term.I(65), Term.I(1))),
                4,
                encoding,
                0,
                encoding == NifNativeSequenceTextHostNames.Latin1 ? [65, 0] : [0]
            );
        }
        Term bad = Term.List(Term.I(65), Term.I(66), Term.I(0xd800));
        ProbeString(
            table,
            owner.Add(bad),
            8,
            NifNativeSequenceTextHostNames.Latin1,
            0,
            [65, 66, 0]
        );
        ProbeString(
            table,
            owner.Add(bad),
            8,
            NifNativeSequenceTextHostNames.Utf8,
            0,
            [0, 66]
        );
        Term emoji = Term.List(Term.I(0x1f600));
        ProbeString(
            table,
            owner.Add(emoji),
            4,
            NifNativeSequenceTextHostNames.Utf8,
            -1,
            [0]
        );
        ProbeString(
            table,
            owner.Add(emoji),
            5,
            NifNativeSequenceTextHostNames.Utf8,
            5,
            [240, 159, 152, 128, 0]
        );
        var get = (delegate* unmanaged[Cdecl]<nint, nuint, byte*, uint, int, int>)Slots(table)[NifNativeSequenceTextHostNames.StringGet];
        Require(get(
            0,
            0,
            null,
            0,
            NifNativeSequenceTextHostNames.Utf8
        ) == 0);
        state.ThrowPending();
    }

    internal static unsafe void InvocationOwnership(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
        using var outer = Environment(context, state);
        using var outerBinding = scope.BindEnvironment(outer);
        using var table = new NifNativeWindowsCallbacks();
        var make = (delegate* unmanaged[Cdecl]<nint, byte*, nuint, nuint>)Slots(table)[NifNativeSequenceTextHostNames.AtomMakeLength];
        byte[] text = [120];
        fixed (byte* bytes = text)
        {
            nuint outerToken = make(0, bytes, 1);
            Require(outer.Get(outerToken) is Atom);
            using (var inner = Environment(context, state))
            {
                try
                {
                    using var binding = scope.BindEnvironment(inner);
                    nuint token = make(outer.Pointer, bytes, 1);
                    Require(inner.Get(token) is Atom);
                    throw new InvalidOperationException(NifNativeSequenceTextHostNames.BindingFailure);
                }
                catch (InvalidOperationException error) when (error.Message == NifNativeSequenceTextHostNames.BindingFailure)
                {
                }
            }
            Require(outer.Get(make(0, bytes, 1)) is Atom);
            var innerState = new NifNativeFailureState();
            using (var innerScope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), innerState))
            using (var inner = Environment(context, innerState))
            using (var binding = innerScope.BindEnvironment(inner))
            {
                Require(inner.Get(make(0, bytes, 1)) is Atom);
                innerState.ThrowPending();
            }
            Require(outer.Get(make(0, bytes, 1)) is Atom);
        }
        state.ThrowPending();
    }

    internal static unsafe void Containment(ProcessContext context)
    {
        using var table = new NifNativeWindowsCallbacks();
        var makeAtom = (delegate* unmanaged[Cdecl]<nint, byte*, nuint, nuint>)Slots(table)[NifNativeSequenceTextHostNames.AtomMakeLength];
        var makeString = (delegate* unmanaged[Cdecl]<nint, byte*, nuint, int, nuint>)Slots(table)[NifNativeSequenceTextHostNames.StringMakeLength];
        foreach (bool atom in new[] { false, true })
        {
            var state = new NifNativeFailureState();
            using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
            using var owner = Environment(context, state);
            using var binding = scope.BindEnvironment(owner);
            byte[] invalidUtf8 = [192, 175];
            nuint token;
            fixed (byte* bytes = invalidUtf8)
                token = atom ? makeAtom(owner.Pointer, null, 256) : makeString(
                    owner.Pointer,
                    bytes,
                    2,
                    NifNativeSequenceTextHostNames.Utf8
                );
            Require(token == 0 && state.HostFailure is null && state.GuestPendingReason!.Equals(Term.A(ErlangErrorReasons.BadArgument)));
        }
        foreach (bool disposed in new[] { false, true })
        {
            var state = new NifNativeFailureState();
            using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
            using var owner = Environment(context, state);
            using var binding = disposed ? scope.BindEnvironment(owner) : null;
            if (disposed)
                owner.Dispose();
            Require(makeAtom(0, null, 0) == 0);
            Require(state.HostFailure is not null && !state.HasGuestException);
        }
        // Valid results need output pointers; refusing inputs must not invent host errors.
        for (int operation = 0; operation < 7; operation++)
        {
            var state = new NifNativeFailureState();
            using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
            using var owner = Environment(context, state);
            using var binding = scope.BindEnvironment(owner);
            nuint atom = owner.Add(Term.A(NifNativeSequenceTextHostNames.SingleAtom));
            switch (operation)
            {
                case 0:
                    Require(((delegate* unmanaged[Cdecl]<nint, nuint, uint*, int>)Slots(table)[NifNativeSequenceTextHostNames.ListLength])(0, owner.Add(Nil.Value), null) == 0);
                    break;
                case 1:
                    Require(((delegate* unmanaged[Cdecl]<nint, nuint, uint*, int, int>)Slots(table)[NifNativeSequenceTextHostNames.AtomLength])(
                        0,
                        atom,
                        null,
                        NifNativeSequenceTextHostNames.Latin1
                    ) == 0);
                    break;
                case 2:
                    Require(((delegate* unmanaged[Cdecl]<nint, nuint, byte*, uint, int, int>)Slots(table)[NifNativeSequenceTextHostNames.AtomGet])(
                        0,
                        atom,
                        null,
                        2,
                        NifNativeSequenceTextHostNames.Latin1
                    ) == 0);
                    break;
                case 3:
                    Require(((delegate* unmanaged[Cdecl]<nint, nuint, byte*, uint, int, int>)Slots(table)[NifNativeSequenceTextHostNames.StringGet])(
                        0,
                        owner.Add(Nil.Value),
                        null,
                        1,
                        NifNativeSequenceTextHostNames.Utf8
                    ) == 0);
                    break;
                case 4:
                    byte character = 97;
                    Require(((delegate* unmanaged[Cdecl]<nint, byte*, nuint, nuint*, int, int>)Slots(table)[NifNativeSequenceTextHostNames.AtomExistingLength])(
                        0,
                        &character,
                        1,
                        null,
                        NifNativeSequenceTextHostNames.Latin1
                    ) == 0);
                    break;
                case 5:
                    Require(makeAtom(0, null, 1) == 0);
                    break;
                case 6:
                    Require(makeString(
                        owner.Pointer,
                        null,
                        1,
                        NifNativeSequenceTextHostNames.Latin1
                    ) == 0);
                    break;
            }
            Require(state.HostFailure is not null && !state.HasGuestException);
        }
        {
            var state = new NifNativeFailureState();
            using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
            using var owner = Environment(context, state);
            nuint wrongType = owner.Add(Nil.Value);
            nuint atom = owner.Add(Term.A(NifNativeSequenceTextHostNames.SingleAtom));
            var atomLength = (delegate* unmanaged[Cdecl]<nint, nuint, uint*, int, int>)Slots(table)[NifNativeSequenceTextHostNames.AtomLength];
            var atomGet = (delegate* unmanaged[Cdecl]<nint, nuint, byte*, uint, int, int>)Slots(table)[NifNativeSequenceTextHostNames.AtomGet];
            Require(atomLength(
                0,
                wrongType,
                null,
                NifNativeSequenceTextHostNames.Latin1
            ) == 0);
            Require(atomGet(
                0,
                wrongType,
                null,
                2,
                NifNativeSequenceTextHostNames.Latin1
            ) == 0);
            Require(atomGet(
                0,
                atom,
                null,
                1,
                NifNativeSequenceTextHostNames.Latin1
            ) == 0);
            Require(atomLength(
                0,
                atom,
                null,
                0
            ) == 0);
            state.ThrowPending();
            using var foreign = Environment(context, new NifNativeFailureState());
            bool refused = false;
            try
            {
                using var binding = scope.BindEnvironment(foreign);
            }
            catch (InvalidOperationException error) when (error.Message == NifNativeInvocationNames.ForeignFailureState)
            {
                refused = true;
            }
            Require(refused && scope.InvocationEnvironment is null);
        }
        var tupleGet = (delegate* unmanaged[Cdecl]<nint, nuint, int*, nuint**, int>)Slots(table)[NifNativeSequenceTextHostNames.Tuple];
        foreach (bool nullArity in new[] { false, true })
        {
            var state = new NifNativeFailureState();
            using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
            using var owner = Environment(context, state);
            int arity = 123;
            nuint* elements = null;
            Require(tupleGet(
                0,
                owner.Add(Term.Tuple()),
                nullArity ? null : &arity,
                nullArity ? &elements : null
            ) == 0);
            Require(arity == 123 && elements == null && state.HostFailure is not null);
            Require(owner.BorrowedTupleArrayCount == 0);
        }
    }

    private static unsafe void ProbeAtom(
        NifNativeWindowsCallbacks table,
        nuint token,
        int encoding,
        byte[]? expected
    )
    {
        var length = (delegate* unmanaged[Cdecl]<nint, nuint, uint*, int, int>)Slots(table)[NifNativeSequenceTextHostNames.AtomLength];
        var get = (delegate* unmanaged[Cdecl]<nint, nuint, byte*, uint, int, int>)Slots(table)[NifNativeSequenceTextHostNames.AtomGet];
        uint size = uint.MaxValue;
        Require(length(
            0,
            token,
            &size,
            encoding
        ) == (expected is null ? 0 : 1));
        Require(size == (expected is null ? uint.MaxValue : (uint)expected.Length));
        byte[] buffer = Enumerable.Repeat(NifNativeSequenceTextHostNames.Canary, 16).ToArray();
        fixed (byte* bytes = buffer)
        {
            if (expected is null)
            {
                Require(get(
                    0,
                    token,
                    bytes,
                    16,
                    encoding
                ) == 0);
                Require(buffer.All(value => value == NifNativeSequenceTextHostNames.Canary));
            }
            else
            {
                Require(get(
                    0,
                    token,
                    bytes,
                    (uint)expected.Length,
                    encoding
                ) == 0);
                Require(buffer.All(value => value == NifNativeSequenceTextHostNames.Canary));
                Require(get(
                    0,
                    token,
                    bytes,
                    (uint)expected.Length + 1,
                    encoding
                ) == expected.Length + 1);
                Require(buffer.Take(expected.Length).SequenceEqual(expected) && buffer[expected.Length] == 0);
                Require(buffer.Skip(expected.Length + 1).All(value => value == NifNativeSequenceTextHostNames.Canary));
            }
        }
    }

    private static unsafe void ProbeString(
        NifNativeWindowsCallbacks table,
        nuint token,
        uint capacity,
        int encoding,
        int result,
        byte[] prefix
    )
    {
        var get = (delegate* unmanaged[Cdecl]<nint, nuint, byte*, uint, int, int>)Slots(table)[NifNativeSequenceTextHostNames.StringGet];
        byte[] buffer = Enumerable.Repeat(NifNativeSequenceTextHostNames.Canary, 16).ToArray();
        fixed (byte* bytes = buffer)
            Require(get(
                0,
                token,
                bytes,
                capacity,
                encoding
            ) == result);
        Require(buffer.Take(prefix.Length).SequenceEqual(prefix));
        Require(buffer.Skip(prefix.Length).All(value => value == NifNativeSequenceTextHostNames.Canary));
    }

    private static unsafe nint* Slots(NifNativeWindowsCallbacks table) => (nint*)table.Pointer;

    private static NifNativeCallEnvironment Environment(ProcessContext context, NifNativeFailureState state)
        => new(
            context,
            new ModuleCodeImage(NifNativeSequenceTextHostNames.Module),
            0,
            state
        );

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(NifNativeSequenceTextHostNames.Failure);
    }
}
