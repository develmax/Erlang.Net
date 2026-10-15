namespace Erlang;

internal sealed class RuntimeInitializer(ProcessContext context)
{
    private sealed record Work(Action<ModuleRegistry> Configure, TaskCompletionSource Completed);

    private static readonly Atom WorkSignal = Term.A(InitializationNames.WorkSignal);
    [ThreadStatic]
    private static RuntimeInitializer? current;

    private readonly object gate = new();
    private readonly Queue<Work> pending = new();
    private bool stopped;
    public Task Completion { get; private set; } = Task.CompletedTask;

    public void Start()
    {
        context.TrapExits = true;
        context.Runtime.Register(InitializationNames.Module, context.Self);
        using (context.EnterFunction(InitializationNames.Module, InitializationNames.Boot, 1))
            context.Runtime.Modules.InstallPreloaded(registry =>
            {
                CoreModules.Register(registry);
                registry.PublishModule(InitializationNames.Module);
                registry.PublishModuleCode(InitializationNames.Module, ModuleCodeFingerprints.Owner(typeof(RuntimeInitializer)));
            });
        Completion = Task.Factory.StartNew(
            Run,
            CancellationToken.None,
            TaskCreationOptions.DenyChildAttach,
            TaskScheduler.Default
        ).Unwrap();
    }

    public ValueTask Configure(Action<ModuleRegistry> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        context.Cancellation.ThrowIfCancellationRequested();
        if (ReferenceEquals(current, this))
        {
            context.Runtime.Modules.Apply(configure);

            return ValueTask.CompletedTask;
        }
        var work = new Work(configure, new(TaskCreationOptions.RunContinuationsAsynchronously));
        lock (gate)
        {
            context.Cancellation.ThrowIfCancellationRequested();
            if (stopped)
                throw new OperationCanceledException(context.Cancellation);
            pending.Enqueue(work);
        }
        context.Mailbox.Send(WorkSignal);

        return new ValueTask(work.Completed.Task);
    }

    private async Task Run()
    {
        using var frame = context.EnterFunction(InitializationNames.Module, InitializationNames.Loop, 1);
        try
        {
            while (true)
            {
                var message = await context.ReceiveAsync(value => value).ConfigureAwait(false);
                if (message is Atom { Name: InitializationNames.WorkSignal })
                {
                    while (Take() is { } work)
                    {
                        try
                        {
                            context.Runtime.ThrowIfHalted();
                            context.Cancellation.ThrowIfCancellationRequested();
                            var previous = current;
                            current = this;
                            try
                            {
                                context.Runtime.Modules.Apply(work.Configure);
                            }
                            finally
                            {
                                current = previous;
                            }
                            work.Completed.TrySetResult();
                        }
                        catch (OperationCanceledException) when (context.Cancellation.IsCancellationRequested)
                        {
                            work.Completed.TrySetCanceled(context.Cancellation);
                        }
                        catch (ProcessHaltTransfer transfer) when (ReferenceEquals(transfer.Runtime, context.Runtime))
                        {
                            work.Completed.TrySetCanceled();
                            throw;
                        }
                        catch (Exception exception)
                        {
                            work.Completed.TrySetException(exception);
                        }
                    }
                }
                else if (IoDefaultDevice.IsRequest(message))
                    await IoDefaultDevice.Execute(context, message).ConfigureAwait(false);
                else if (message is TupleTerm { Items.Count: 2 } request && request.Items[0] is Pid from && request.Items[1] is Atom { Name: InitializationNames.GetStatus })
                {
                    context.Runtime.Send(
                        from,
                        Term.Tuple(
                        Term.A(InitializationNames.Module),
                        Term.Tuple(Term.A(InitializationNames.Started), Term.A(InitializationNames.Started))
                    )
                    );
                }
            }
        }
        catch (ProcessHaltTransfer transfer) when (ReferenceEquals(transfer.Runtime, context.Runtime))
        {
        }
        catch (OperationCanceledException) when (context.Cancellation.IsCancellationRequested)
        {
        }
        finally
        {
            lock (gate)
            {
                stopped = true;
                while (pending.TryDequeue(out var work))
                    work.Completed.TrySetCanceled(context.Cancellation);
            }
            context.Runtime.InitializationEnded(context);
        }
    }

    private Work? Take()
    {
        lock (gate)
            return pending.TryDequeue(out var work) ? work : null;
    }
}
