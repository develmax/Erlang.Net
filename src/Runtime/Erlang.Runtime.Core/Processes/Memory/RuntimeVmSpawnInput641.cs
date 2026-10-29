namespace Erlang;

internal sealed class RuntimeVmSpawnInput641 : IDisposable
{
    private readonly string? module;
    private readonly string? function;
    private TupleTerm? source;
    private RuntimeProcessPayload641? prepared;
    private int preparation, claimed, closed;

    private RuntimeVmSpawnInput641(
        string? module,
        string? function,
        Term callback,
        IReadOnlyList<Term> arguments
    )
    {
        this.module = module;
        this.function = function;
        source = Term.Tuple(callback, Cons.From(arguments));
    }

    internal static Func<ProcessContext, ValueTask<Term>> Mfa641(string module, string function, IReadOnlyList<Term> arguments)
        => new RuntimeVmSpawnInput641(
            module,
            function,
            Nil.Value,
            arguments
        ).Invoke641;

    internal static Func<ProcessContext, ValueTask<Term>> Callback641(Term callback, IReadOnlyList<Term> arguments)
        => new RuntimeVmSpawnInput641(
            null,
            null,
            callback,
            arguments
        ).Invoke641;

    internal static IDisposable? PrepareBody641(ProcessRuntime runtime, Func<ProcessContext, ValueTask<Term>> body, bool outsideGate)
    {
        if (body.Target is not RuntimeVmSpawnInput641 input)
            return null;
        if (Volatile.Read(ref input.preparation) == 2)
            return input;
        if (!outsideGate || Interlocked.CompareExchange(ref input.preparation, 1, 0) != 0)
            throw new InvalidOperationException(RuntimeProcessPayloadNames641.Invalid);
        try
        {
            input.prepared = RuntimeProcessPayload641.Prepare641(runtime, null, input.source!);
            input.source = null;
            Volatile.Write(ref input.preparation, 2);

            return input;
        }
        catch
        {
            input.prepared?.Close641();
            throw;
        }
    }

    internal void Claim641(ProcessContext context)
    {
        RuntimeProcessPayload641 original = prepared
            ?? throw new InvalidOperationException(RuntimeProcessPayloadNames641.Invalid);
        if (!ReferenceEquals(original.Runtime641, context.Runtime) || Volatile.Read(ref claimed) != 0 || Volatile.Read(ref closed) != 0)
            throw new InvalidOperationException(RuntimeProcessPayloadNames641.Invalid);
        RuntimeProcessPayload641 adopted = original.Borrow641(context);
        RuntimeProcessPayload641? bridge = null;
        try
        {
            // This is process-local actual work, never a global native-domain join.
            bridge = RuntimeProcessPayload641.Reserve641(context.Runtime, context);
            bridge.CloseAfter641(original.Completion641);
            prepared = adopted;
            context.VmSpawnInput641 = this;
            Volatile.Write(ref claimed, 1);
            original.Close641();
        }
        catch
        {
            bridge?.Close641();
            adopted.Close641();
            throw;
        }
    }

    private ValueTask<Term> Invoke641(ProcessContext child)
    {
        RuntimeProcessPayload641 input = prepared
            ?? throw new InvalidOperationException(RuntimeProcessPayloadNames641.Invalid);
        var values = (TupleTerm)input.Value641;
        Term[] arguments = Cons.Items(values.Items[1]).ToArray();

        return module is null
            ? child.Runtime.Modules.CallVmCallbackRoot641(
                child,
                values.Items[0],
                arguments,
                input
            )
            : child.Runtime.Modules.CallVmMfaRoot641(
                child,
                module,
                function!,
                arguments,
                input
            );
    }

    internal void CloseClaimed641()
    {
        if (Interlocked.Exchange(ref closed, 1) == 0)
            prepared?.Close641();
    }

    public void Dispose()
    {
        // The outer API owns only failed/unclaimed admission. A claimed child
        // releases after real root adoption or in its actual actor finally.
        if (Volatile.Read(ref claimed) == 0)
            CloseClaimed641();
    }
}
