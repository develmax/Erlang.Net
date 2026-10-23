using System.Numerics;
using System.Runtime.InteropServices;

namespace Erlang;

internal sealed class PrimTtyBuiltinOwner : IAsyncDisposable, INifImageRetirementPreparation
{
    private readonly ProcessRuntime runtime;
    private readonly object gate = new();
    private readonly HashSet<PrimTtyResourceState> active = new();
    private readonly HashSet<Task> retiring = new();
    private readonly List<Exception> failures = new();
    private readonly nint locale;
    private bool closed;
    private Task? disposal;
    private int invocations;
    private TaskCompletionSource? invocationDrain;

    internal PrimTtyBuiltinOwner(ProcessRuntime runtime)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        this.runtime = runtime;
        locale = PrimTtyWindowsNative._get_current_locale();
        if (locale == 0) throw new System.ComponentModel.Win32Exception(Marshal.GetLastPInvokeError());
    }

    internal IReadOnlyList<KeyValuePair<(string Name, int Arity), ErlangFunction>> Bindings()
        => PrimTtyNames.Signatures.Select(signature => new KeyValuePair<(string Name, int Arity), ErlangFunction>(
            signature, (context, args) => InvokeCaptured(context, signature.Name, args))).ToArray();

    private async ValueTask<Term> InvokeCaptured(ProcessContext context, string name, IReadOnlyList<Term> args)
    {
        // A loaded NIF replaces its Erlang stub; a native error retains the
        // actual arguments without the stub's source location or duplicate frame.
        using var stub = context.EnterBeamNifReplacement(PrimTtyNames.Module, name, args.Count);
        try
        {
            return await Invoke(context, name, args).ConfigureAwait(false);
        }
        catch (ErlangException error)
        {
            throw context.CaptureNativeFailure(
                error,
                PrimTtyNames.Module,
                name,
                args
            );
        }
    }

    private async ValueTask<Term> Invoke(ProcessContext context, string name, IReadOnlyList<Term> args)
    {
        if (!ReferenceEquals(context.Runtime, runtime)) throw new ArgumentException(PrimTtyNames.Foreign);
        context.Cancellation.ThrowIfCancellationRequested();
        runtime.ThrowIfHalted();
        using IDisposable invocation = EnterInvocation();
        // Genuine configured no-termcap Windows branches: no argument validation.
        if (name is PrimTtyNames.FunctionSetuptermNif or PrimTtyNames.FunctionTigetnumNif or PrimTtyNames.FunctionTigetflagNif or PrimTtyNames.FunctionTigetstrNif or PrimTtyNames.FunctionTinfoNif or PrimTtyNames.FunctionTputsNif)
            return PrimTtyAtoms.Failure(PrimTtyAtoms.NoSupport);
        if (name == PrimTtyNames.FunctionSizeofWchar) return Term.I(PrimTtyNames.WcharBytes);
        if (name == PrimTtyNames.FunctionWcwidth)
        {
            _ = NativeInt(args[0]);

            return PrimTtyAtoms.Failure(PrimTtyAtoms.NoSupport);
        }
        if (name == PrimTtyNames.FunctionWcswidth)
        {
            byte[] bytes = ((BitString)ErlangIoData.Flatten(args[0], false, false)).ToArray();

            return Term.Tuple(PrimTtyAtoms.Ok, Term.I(bytes.Length / PrimTtyNames.WcharBytes));
        }
        if (name == PrimTtyNames.FunctionIsprint)
        {
            int value = NativeInt(args[0]);

            return PrimTtyAtoms.Boolean(PrimTtyWindowsNative._isprint_l(unchecked((sbyte)value), locale) != 0);
        }
        if (name == PrimTtyNames.FunctionIsatty)
        {
            int selector = Selector(args[0]);
            if (selector == 0) return Term.A(PrimTtyNames.BadDescriptor);
            var result = PrimTtyRedirectedHandleScope.IsTerminal(selector);

            return result.BadDescriptor ? Term.A(PrimTtyNames.BadDescriptor) : PrimTtyAtoms.Boolean(result.IsTerminal!.Value);
        }
        if (name == PrimTtyNames.FunctionTtyCreate) return Create(args[0]);
        if (args[0] is not ReferenceTerm reference) throw BadArg();
        using var rootLease = runtime.ReferenceResources.AcquirePrimTty(reference);
        if (rootLease is null) throw BadArg();
        PrimTtyResourceRoot root = rootLease.Value;
        if (!ReferenceEquals(root.Owner, this)) throw BadArg();
        using IDisposable operation = root.State.Enter();
        PrimTtyResourceState state = root.State;
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(context.Cancellation, state.Stop);
        switch (name)
        {
            case PrimTtyNames.FunctionTtyInit:
                if (args[1] is not MapTerm map || !map.TryGet(Term.A(PrimTtyNames.Input), out Term? input)) throw BadArg();

                return input!.Equals(Term.A(PrimTtyNames.Raw)) ? PrimTtyAtoms.Failure(PrimTtyAtoms.NoSupport) : PrimTtyAtoms.Ok;
            case PrimTtyNames.FunctionSetlocale:
                // Windows source changes only SetConsoleOutputCP when captured
                // dwOutMode != 0. This exact profile captured neither console mode.
                if (!state.Handles.CapturedUnavailable)
                    throw new InvalidOperationException(PrimTtyWindowsNames.ConsoleRefusal);

                return PrimTtyAtoms.True;
            case PrimTtyNames.FunctionTtyEncoding:
                return state.Handles.InputIsUtf16Little()
                    ? Term.Tuple(Term.A(PrimTtyNames.Utf16), Term.A(PrimTtyNames.Little)) : Term.A(PrimTtyNames.Utf8);
            case PrimTtyNames.FunctionTtyWindowSize:
                var size = state.Handles.WindowSize();

                return size.Error is { } error ? PrimTtyWindowsGuestError.Error(error.Code, error.Operation)
                    : Term.Tuple(PrimTtyAtoms.Ok, Term.Tuple(Term.I(size.Width), Term.I(size.Height)));
            case PrimTtyNames.FunctionTtyIsOpen:
                int descriptor = Selector(args[1]);
                if (descriptor == 0) throw BadArg();
                var status = state.Handles.IsOpen(descriptor);
                if (status.BadArgument) throw BadArg();

                return status.Error is { } failure ? PrimTtyWindowsGuestError.Error(failure.Code, failure.Operation)
                    : PrimTtyAtoms.Boolean(status.Open!.Value);
            case PrimTtyNames.FunctionTtySelect:
                state.Select(context, (ResourceReferenceTerm)reference, args[1]);

                return PrimTtyAtoms.Ok;
            case PrimTtyNames.FunctionReadNif:
                if (args[2] is not Integer count || count.Value < 0 || count.Value > ulong.MaxValue) throw BadArg();
                int length = (int)BigInteger.Min(count.Value, PrimTtyNames.ReadMaximum);
                Term read = await PrimTtyInterruptibleIo.Run(() => Read(state.Handles.InputHandle, length), cancellation.Token).ConfigureAwait(false);
                if (read is TupleTerm tuple && tuple.Items[0].Equals(PrimTtyAtoms.Ok))
                    state.Select(context, (ResourceReferenceTerm)reference, args[1]);

                return read;
            case PrimTtyNames.FunctionWriteNif:
                foreach (BitString segment in ErlangIoData.Segments(args[1], false, true))
                {
                    Term written = await PrimTtyInterruptibleIo.Run(() => Write(state.Handles.OutputHandle, segment.ToArray()), cancellation.Token).ConfigureAwait(false);
                    if (!written.Equals(PrimTtyAtoms.Ok)) return written;
                }

                return PrimTtyAtoms.Ok;
            default:
                throw new InvalidOperationException(name);
        }
    }

    private Term Create(Term selector)
    {
        if (selector.Equals(Term.A(PrimTtyNames.Stdin))) throw BadArg();
        // Current explicit profile supports genuine stdout/stderr selectors.
        // Unknown-selector NUL behavior is held, never silently mapped to stderr.
        if (selector is not Atom atom || atom.Name is not (PrimTtyNames.Stdout or PrimTtyNames.Stderr))
            throw new NotSupportedException(PrimTtyNames.UnsupportedSelector);
        var handles = PrimTtyRedirectedHandleScope.Create(atom.Name == PrimTtyNames.Stdout);
        var state = new PrimTtyResourceState(handles);
        bool owned = false;
        try
        {
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(closed, this);
                active.Add(state);
                owned = true;
            }
            var reference = runtime.ReferenceResources.CreatePrimTty(new PrimTtyResourceRoot(this, state));

            return Term.Tuple(PrimTtyAtoms.Ok, reference);
        }
        catch (Exception original)
        {
            if (owned) Retire(state);
            else
                try
                {
                    handles.Dispose();
                }
                catch (Exception cleanup)
                {
                    throw new AggregateException(original, cleanup);
                }
            throw;
        }
    }

    private static int Selector(Term value) => value is Atom atom ? atom.Name switch
    {
        PrimTtyNames.Stdin => PrimTtyWindowsNames.StandardInput,
        PrimTtyNames.Stdout => PrimTtyWindowsNames.StandardOutput,
        PrimTtyNames.Stderr => PrimTtyWindowsNames.StandardError,
        _ => 0
    } : 0;

    private static int NativeInt(Term value) => value is Integer integer && integer.Value >= int.MinValue && integer.Value <= int.MaxValue
        ? (int)integer.Value : throw BadArg();

    private static ErlangException BadArg() => new(ErlangErrorReasons.BadArgument);

    private static Term Read(nint handle, int length)
    {
        bool character = PrimTtyWindowsNative.GetFileType(handle) == PrimTtyWindowsNames.TypeCharacter;
        byte[] buffer = new byte[character ? length * PrimTtyNames.WcharBytes : length];
        bool success = character
            ? PrimTtyWindowsNative.ReadConsoleW(
                handle,
                buffer,
                (uint)length,
                out uint readCharacters,
                0
            )
            : PrimTtyWindowsNative.ReadFile(
                handle,
                buffer,
                (uint)length,
                out readCharacters,
                0
            );
        uint error = success ? 0 : (uint)Marshal.GetLastPInvokeError();
        uint read = character ? readCharacters * PrimTtyNames.WcharBytes : readCharacters;
        if (!success)
            return error == PrimTtyWindowsNames.BrokenPipe ? PrimTtyAtoms.Failure(PrimTtyAtoms.Closed)
                : PrimTtyWindowsGuestError.Error(error, character ? PrimTtyNames.ReadConsole : PrimTtyNames.ReadFile);

        return read == 0 ? PrimTtyAtoms.Failure(PrimTtyAtoms.Closed)
            : Term.Tuple(PrimTtyAtoms.Ok, new BitString(buffer.AsSpan(0, checked((int)read))));
    }

    private static Term Write(nint handle, byte[] bytes)
    {
        int offset = 0;
        while (offset < bytes.Length)
        {
            byte[] remaining = offset == 0 ? bytes : bytes.AsSpan(offset).ToArray();
            if (!PrimTtyWindowsNative.WriteFile(
                handle,
                remaining,
                (uint)remaining.Length,
                out uint written,
                0
            ))
                return PrimTtyWindowsGuestError.Error((uint)Marshal.GetLastPInvokeError());
            if (written == 0) throw new IOException(PrimTtyNames.WriteFile);
            offset += checked((int)written);
        }

        return PrimTtyAtoms.Ok;
    }

    private IDisposable EnterInvocation()
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, this);
            invocations++;

            return new Invocation(this);
        }
    }

    private sealed class Invocation(PrimTtyBuiltinOwner owner) : IDisposable
    {
        private PrimTtyBuiltinOwner? current = owner;

        public void Dispose()
        {
            var target = Interlocked.Exchange(ref current, null);
            if (target is null) return;
            lock (target.gate)
                if (--target.invocations == 0) target.invocationDrain?.TrySetResult();
        }
    }

    internal void Retire(PrimTtyResourceState state)
    {
        TaskCompletionSource completion;
        lock (gate)
        {
            if (!active.Remove(state)) return;
            completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            retiring.Add(completion.Task);
        }
        _ = FinishRetirement(state, completion);
    }

    private async Task FinishRetirement(PrimTtyResourceState state, TaskCompletionSource completion)
    {
        Exception? failure = null;
        try
        {
            await state.BeginClose().ConfigureAwait(false);
        }
        catch (Exception error)
        {
            failure = error;
        }
        lock (gate)
        {
            if (failure is not null) failures.Add(failure);
            retiring.Remove(completion.Task);
            if (failure is null) completion.TrySetResult();
            else completion.TrySetException(failure);
        }
    }

    public void BeginRetirement()
    {
        PrimTtyResourceState[] states;
        lock (gate)
        {
            closed = true;
            states = active.ToArray();
        }
        foreach (var state in states) Retire(state);
    }

    public ValueTask DisposeAsync()
    {
        TaskCompletionSource completion;
        lock (gate)
        {
            if (disposal is not null) return new(disposal);
            completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            disposal = completion.Task;
        }
        _ = CompleteClose(completion);

        return new(completion.Task);
    }

    private async Task CompleteClose(TaskCompletionSource completion)
    {
        try
        {
            await Close().ConfigureAwait(false);
            completion.TrySetResult();
        }
        catch (Exception error)
        {
            completion.TrySetException(error);
        }
    }

    private async Task Close()
    {
        BeginRetirement();
        Task calls;
        lock (gate) calls = invocations == 0 ? Task.CompletedTask
            : (invocationDrain ??= new(TaskCreationOptions.RunContinuationsAsynchronously)).Task;
        await calls.ConfigureAwait(false);
        while (true)
        {
            Task[] pending;
            lock (gate) pending = retiring.ToArray();
            if (pending.Length == 0) break;
            try
            {
                await Task.WhenAll(pending).ConfigureAwait(false);
            }
            catch { /* FinishRetirement retains exact faults. */ }
            await Task.Yield();
        }
        PrimTtyWindowsNative._free_locale(locale);
        lock (gate) if (failures.Count != 0) throw new AggregateException(failures);
    }
}
