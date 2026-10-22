namespace Erlang;

public sealed partial class ProcessRuntime
{
    private bool originalBootStarted;

    public async Task<RuntimeOtpBootSession> StartOtpBootAsync(
        IEnumerable<BitString> arguments,
        CancellationToken cancellation = default
    )
    {
        ArgumentNullException.ThrowIfNull(arguments);
        Term[] owned = arguments.Select(argument => argument is { IsBinary: true }
            ? (Term)new BitString(argument.ToArray())
            : throw new ArgumentException(RuntimeOtpBootNames.InvalidArguments, nameof(arguments))).ToArray();
        cancellation.ThrowIfCancellationRequested();
        if (Modules.SourceOwner(InitializationNames.Module, InitializationNames.Boot, 1) is not PreparedBeamImage)
            throw new InvalidOperationException(RuntimeOtpBootNames.OriginalInitRequired);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            ThrowIfHalted();
            if (originalBootStarted || !names.TryGetValue(InitializationNames.Module, out Term? registered)
                || !registered.Equals(InitializationPid))
                throw new InvalidOperationException(RuntimeOtpBootNames.AlreadyStarted);
            originalBootStarted = true;
            names.Remove(InitializationNames.Module);
        }
        try
        {
            StartDirtySystemHandlersBeforeBoot428();
            StartManagedCollectorsBeforeBoot413();
            StartCodePurgerBeforeBoot432();
            ProcessHandle original = SpawnTraced(
                context => Modules.Call(
                    context,
                    InitializationNames.Module,
                    InitializationNames.Boot,
                    Cons.From(owned)
                ),
                null,
                false,
                null,
                systemProcess: true
            );
            var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            ProcessHandle observer = Spawn(async context =>
            {
                Send(original.Pid, Term.Tuple(context.Self,
                    Term.Tuple(Term.A(RuntimeOtpBootNames.NotifyWhenStarted), context.Self)));
                while (true)
                {
                    Term? reply = await context.ReceiveAsync(message => message is TupleTerm { Items.Count: 2 } tuple
                        && tuple.Items[0] is Atom { Name: InitializationNames.Module } ? tuple.Items[1] : null);
                    if (reply is Atom { Name: InitializationNames.Started })
                    {
                        ready.TrySetResult();

                        return Nil.Value;
                    }
                }
            });
            Task first = await Task.WhenAny(ready.Task, original.Completion, observer.Completion).WaitAsync(cancellation).ConfigureAwait(false);
            if (first != ready.Task && !ready.Task.IsCompletedSuccessfully)
            {
                Term reason = await (first == original.Completion ? original.Completion : observer.Completion).ConfigureAwait(false);

                throw new InvalidOperationException(RuntimeOtpBootNames.EndedBeforeReady + RuntimeOtpBootNames.FailureSeparator + reason);
            }
            await ready.Task.WaitAsync(cancellation).ConfigureAwait(false);
            await observer.Completion.ConfigureAwait(false);
            cancellation.ThrowIfCancellationRequested();

            return new(original.Pid, original.Completion);
        }
        catch (Exception failure)
        {
            try
            {
                await DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(failure, cleanup);
            }

            throw;
        }
    }
}
