namespace Erlang;

public sealed partial class ProcessRuntime
{
    private RuntimeDriverFrontend422? driverFrontend422;
    private TaskCompletionSource? driverFrontendInstalling422;
    private bool driverFrontendClosing422;

    internal void BeginDriverFrontendInstall422()
    {
        lock (gate)
        {
            RequireNativePrimitiveRegistration();
            ObjectDisposedException.ThrowIf(driverFrontendClosing422, this);
            if (driverFrontend422 is not null || driverFrontendInstalling422 is not null)
                throw new InvalidOperationException(DriverParentWireNames422.Invalid);
            driverFrontendInstalling422 = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }

    internal async ValueTask InstallDriverFrontend422(DriverFrontendConfiguration422 configuration,
        CancellationToken cancellation)
    {
        BeginDriverFrontendInstall422();
        RuntimeDriverFrontend422? pending = null;
        bool published = false;
        try
        {
            pending = await RuntimeDriverFrontend422.Create(
                this,
                gate,
                configuration,
                cancellation
            ).ConfigureAwait(false);
            lock (gate)
            {
                RequireNativePrimitiveRegistration();
                ObjectDisposedException.ThrowIf(driverFrontendClosing422, this);
                cancellation.ThrowIfCancellationRequested();
                driverFrontend422 = pending;
                published = true;
            }
        }
        finally
        {
            try
            {
                // A close/publication race cannot finish installing until the
                // unpublished actual native child has completed its rollback.
                if (!published && pending is not null) await pending.BeginClose().ConfigureAwait(false);
            }
            finally
            {
                lock (gate)
                {
                    driverFrontendInstalling422!.TrySetResult();
                    driverFrontendInstalling422 = null;
                }
            }
        }
    }

    internal bool TryCaptureDdll422(
        string module,
        string function,
        int arity,
        out ErlangFunction body
    )
    {
        body = null!;
        if (module != DdllParentAtoms422.Module)
            return false;
        lock (gate)
        {
            if (driverFrontend422 is null || driverFrontendClosing422)
                return false;

            return driverFrontend422.CreateDdllBodies().TryGetValue((function, arity), out body!);
        }
    }

    internal void RequireDriverFrontendReady422(ProcessContext context)
    {
        lock (gate)
        {
            RequireDriverParentActor422(context);
            (driverFrontend422 ?? throw new InvalidOperationException(DriverParentWireNames422.Invalid)).RequireReady();
        }
    }

    private Task CloseDriverFrontend422()
    {
        driverFrontendClosing422 = true;
        Task retirement = driverFrontend422?.BeginClose() ?? Task.CompletedTask;

        return driverFrontendInstalling422 is null ? retirement
            : Task.WhenAll(retirement, driverFrontendInstalling422.Task);
    }

    private Task DrainDriverFrontend422()
    {
        lock (gate) return CloseDriverFrontend422();
    }

    private bool DriverFrontendRetirementCompleted422
        => driverFrontendInstalling422 is null && driverFrontend422?.RetirementCompletedSuccessfully != false;

    private Task DriverFrontendProcessRetirement422(
        ProcessContext context,
        Task previous,
        RuntimeProcessPayload641? reason641 = null
    )
    {
        if (driverFrontend422 is null && driverFrontendInstalling422 is null) return previous;

        return Task.WhenAll(
            previous,
            driverFrontend422?.ProcessTerminated(
                context,
                context.ExitReason ?? Term.A(ProcessExitReasons.Normal),
                reason641
            ) ?? Task.CompletedTask,
            context.Self.Equals(InitializationPid) ? CloseDriverFrontend422() : Task.CompletedTask
        );
    }
}
