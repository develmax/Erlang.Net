using System.Runtime.ExceptionServices;
using System.Text;

namespace Erlang.NifNativeBackend;

internal static class NifNativeLegacyStringHostChecks
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
        Term reason = await actor.Completion.WaitAsync(NifNativeLegacyStringHostNames.Timeout);
        if (failure is not null)
            ExceptionDispatchInfo.Capture(failure).Throw();
        Require(reason.Equals(Term.A(ProcessExitReasons.Normal)));
        Require(NifNativeCallEnvironment.ActiveCount == active);
    }

    internal static unsafe void Latin1(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
        using var owner = Environment(context, state);
        using var table = new NifNativeWindowsCallbacks();
        Require(Result(owner, state, Call(
            table,
            owner.Pointer,
            [0],
            NifNativeLegacyStringHostNames.Latin1
        )) is Nil);
        byte[] greeting = Encoding.ASCII.GetBytes(NifNativeLegacyStringHostNames.LegacyGreeting).Append((byte)0).ToArray();
        Require(Result(owner, state, Call(
            table,
            owner.Pointer,
            greeting,
            NifNativeLegacyStringHostNames.Latin1
        )).Equals(Term.String(NifNativeLegacyStringHostNames.LegacyGreeting)));
        byte[] everyByte = Enumerable.Range(1, byte.MaxValue).Select(value => (byte)value).Append((byte)0).ToArray();
        Term actual = Result(owner, state, Call(
            table,
            owner.Pointer,
            everyByte,
            NifNativeLegacyStringHostNames.Latin1
        ));
        Require(Cons.Items(actual).Cast<Integer>().Select(number => (int)number.Value).SequenceEqual(Enumerable.Range(1, byte.MaxValue)));
        Term rawUtf8Bytes = Result(owner, state, Call(
            table,
            owner.Pointer,
            [195, 169, 0],
            NifNativeLegacyStringHostNames.Latin1
        ));
        Require(rawUtf8Bytes.Equals(Term.List(Term.I(195), Term.I(169))));
    }

    internal static unsafe void Utf8(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
        using var owner = Environment(context, state);
        using var table = new NifNativeWindowsCallbacks();
        Require(Result(owner, state, Call(
            table,
            owner.Pointer,
            [0],
            NifNativeLegacyStringHostNames.Utf8
        )) is Nil);
        byte[] bytes = Encoding.UTF8.GetBytes(NifNativeLegacyStringHostNames.Unicode).Append((byte)0).ToArray();
        Require(Result(owner, state, Call(
            table,
            owner.Pointer,
            bytes,
            NifNativeLegacyStringHostNames.Utf8
        )).Equals(Term.List(Term.I(233), Term.I(0x1f600))));
        // The UTF8 BOM is a character in a string, not an out-of-band file marker.
        Require(Result(owner, state, Call(
            table,
            owner.Pointer,
            [239, 187, 191, 0],
            NifNativeLegacyStringHostNames.Utf8
        )).Equals(Term.List(Term.I(0xfeff))));
        Require(Result(owner, state, Call(
            table,
            owner.Pointer,
            [244, 143, 191, 191, 0],
            NifNativeLegacyStringHostNames.Utf8
        )).Equals(Term.List(Term.I(0x10ffff))));
    }

    internal static unsafe void LongInputs(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
        using var owner = Environment(context, state);
        using var table = new NifNativeWindowsCallbacks();
        foreach (int encoding in new[] { NifNativeLegacyStringHostNames.Latin1, NifNativeLegacyStringHostNames.Utf8 })
        {
            foreach (int length in NifNativeLegacyStringHostNames.Lengths)
            {
                byte[] ascii = Enumerable.Repeat((byte)65, length).Append((byte)0).ToArray();
                Term list = Result(owner, state, Call(
                    table,
                    owner.Pointer,
                    ascii,
                    encoding
                ));
                Integer[] items = Cons.Items(list).Cast<Integer>().ToArray();
                Require(items.Length == length && items.All(value => value.Value == 65));
            }
        }
        // Byte length exceeds the previous cap even with fewer Unicode characters.
        byte[] unicode = Enumerable.Repeat(new byte[] { 195, 169 }, 4097).SelectMany(value => value).Append((byte)0).ToArray();
        Term scalars = Result(owner, state, Call(
            table,
            owner.Pointer,
            unicode,
            NifNativeLegacyStringHostNames.Utf8
        ));
        Integer[] decoded = Cons.Items(scalars).Cast<Integer>().ToArray();
        Require(decoded.Length == 4097 && decoded.All(value => value.Value == 233));
    }

    internal static unsafe void Termination(ProcessContext context)
    {
        var state = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
        using var owner = Environment(context, state);
        using var table = new NifNativeWindowsCallbacks();
        foreach (int encoding in new[] { NifNativeLegacyStringHostNames.Latin1, NifNativeLegacyStringHostNames.Utf8 })
        {
            // Bytes after the first NUL do not participate, including invalid UTF8.
            Require(Result(owner, state, Call(
                table,
                owner.Pointer,
                [65, 0, 255, 192, 175, 0],
                encoding
            )).Equals(Term.List(Term.I(65))));
            Require(Result(owner, state, Call(
                table,
                owner.Pointer,
                [0, 255, 0],
                encoding
            )) is Nil);
        }
        byte[] partialBeforeZero = [195, 0, 169, 0];
        var failure = new NifNativeFailureState();
        using var failureScope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), failure);
        using var failureOwner = Environment(context, failure);
        Require(Call(
            table,
            failureOwner.Pointer,
            partialBeforeZero,
            NifNativeLegacyStringHostNames.Utf8
        ) == 0);
        RequireBadArgument(failure);
    }

    internal static unsafe void RejectedUtf8(ProcessContext context)
    {
        using var table = new NifNativeWindowsCallbacks();
        byte[][] invalid = [[192, 175, 0], [128, 0], [194, 0], [226, 130, 0],
            [240, 159, 152, 0], [237, 160, 128, 0], [244, 144, 128, 128, 0]];
        foreach (byte[] bytes in invalid)
        {
            var state = new NifNativeFailureState();
            using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
            using var owner = Environment(context, state);
            Require(Call(
                table,
                owner.Pointer,
                bytes,
                NifNativeLegacyStringHostNames.Utf8
            ) == 0);
            RequireBadArgument(state);
            var isException = (delegate* unmanaged[Cdecl]<nint, nuint, int>)((nint*)table.Pointer)[NifNativeLegacyStringHostNames.IsException];
            var pending = (delegate* unmanaged[Cdecl]<nint, nuint*, int>)((nint*)table.Pointer)[NifNativeLegacyStringHostNames.PendingException];
            nuint reason = 0;
            Require(isException(owner.Pointer, 0) == 1 && pending(owner.Pointer, &reason) == 1);
            Require(owner.Get(reason).Equals(Term.A(ErlangErrorReasons.BadArgument)));
        }
        foreach (int encoding in new[] { 0, 3 })
        {
            var state = new NifNativeFailureState();
            using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
            using var owner = Environment(context, state);
            Require(Call(
                table,
                owner.Pointer,
                [0],
                encoding
            ) == 0);
            RequireBadArgument(state);
        }
    }

    internal static unsafe void EnvironmentAndContainment(ProcessContext context)
    {
        using var table = new NifNativeWindowsCallbacks();
        var state = new NifNativeFailureState();
        using (var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state))
        using (var owner = Environment(context, state))
        using (var other = Environment(context, new NifNativeFailureState()))
        {
            nuint token = Call(
                table,
                other.Pointer,
                [65, 0],
                NifNativeLegacyStringHostNames.Utf8
            );
            Require(Result(other, state, token).Equals(Term.List(Term.I(65))));
            bool foreign = false;
            try
            {
                owner.Get(token);
            }
            catch (InvalidOperationException error) when (error.Message == NifNativeDiagnostics.ForeignTermHandle)
            {
                foreign = true;
            }
            Require(foreign);
        }
        foreach (bool nullInput in new[] { false, true })
        {
            state = new NifNativeFailureState();
            using var scope = new NifNativeCallbackScope(new NifNativeAllocationOwner(), state);
            using var owner = Environment(context, state);
            nint pointer = owner.Pointer;
            if (!nullInput)
                owner.Dispose();
            byte zero = 0;
            var make = (delegate* unmanaged[Cdecl]<nint, byte*, int, nuint>)((nint*)table.Pointer)[NifNativeLegacyStringHostNames.MakeString];
            Require(make(pointer, nullInput ? null : &zero, NifNativeLegacyStringHostNames.Latin1) == 0);
            Require(state.HostFailure?.SourceException is InvalidOperationException && !state.HasGuestException);
        }
    }

    private static unsafe nuint Call(
        NifNativeWindowsCallbacks table,
        nint env,
        byte[] bytes,
        int encoding
    )
    {
        // Only our bounded owned arrays, each with a real trailing NUL, reach the scanner.
        Require(bytes.Length != 0 && bytes[^1] == 0);
        var make = (delegate* unmanaged[Cdecl]<nint, byte*, int, nuint>)((nint*)table.Pointer)[NifNativeLegacyStringHostNames.MakeString];
        fixed (byte* input = bytes)
            return make(env, input, encoding);
    }

    private static Term Result(NifNativeCallEnvironment owner, NifNativeFailureState state, nuint token)
    {
        state.ThrowPending();

        return owner.Get(token);
    }

    private static void RequireBadArgument(NifNativeFailureState state)
        => Require(state.HostFailure is null && state.GuestPendingReason is Atom atom && atom.Name == ErlangErrorReasons.BadArgument);

    private static NifNativeCallEnvironment Environment(ProcessContext context, NifNativeFailureState state)
        => new(
            context,
            new ModuleCodeImage(NifNativeLegacyStringHostNames.Module),
            0,
            state
        );

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(NifNativeLegacyStringHostNames.Failure);
    }
}
