namespace Erlang;

internal sealed class PrimFileBuiltinOwner(ProcessRuntime runtime) : IAsyncDisposable
{
    private int disposed;

    internal IReadOnlyList<KeyValuePair<(string Name, int Arity), ErlangFunction>> Bindings()
    {
        return
        [
            new(
                (PrimFileBuiltinNames.GetCwd, 0),
                (context, _) => Invoke(context,
                () => PrimFileWindowsOperations410.GetCwd(context.Cancellation))
            ),
            new(
                (PrimFileBuiltinNames.ReadFile, 1),
                (context, arguments) => Invoke(context,
                () => PrimFileWindowsOperations410.ReadFile(arguments[0], context.Cancellation))
            ),
            new(
                (PrimFileBuiltinNames.ReadInfo, 2),
                (context, arguments) => Invoke(context,
                () => PrimFileWindowsOperations410.ReadInfo(arguments[0], arguments[1], context.Cancellation))
            ),
            new(
                (PrimFileBuiltinNames.ListDir, 1),
                (context, arguments) => Invoke(context,
                () => PrimFileWindowsOperations410.ListDir(arguments[0], context.Cancellation))
            )
        ];
    }

    private ValueTask<Term> Invoke(ProcessContext context, Func<Term> body)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) != 0, this);
        if (!ReferenceEquals(context.Runtime, runtime))
            throw new ArgumentException(RuntimeBootstrapFileNames.ForeignRegistry, nameof(context));
        context.Runtime.ThrowIfHalted();
        context.Cancellation.ThrowIfCancellationRequested();

        return ValueTask.FromResult(body());
    }

    public ValueTask DisposeAsync()
    {
        Interlocked.Exchange(ref disposed, 1);

        return ValueTask.CompletedTask;
    }
}
