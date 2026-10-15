namespace Erlang;

public sealed partial class ModuleRegistry
{
    private ModuleSourceMutationPermission? sourceMutationPermission;
    private readonly Dictionary<ModuleCodeImage, SourcePurgeFenceSession> sourcePurgeSessions = new();
    private TaskCompletionSource? sourceShutdown;
    private bool sourceClosing;

    private ModuleSourceMutationPermission SourceMutation
    {
        get
        {
            if (Volatile.Read(ref sourceMutationPermission) is { } existing)
                return existing;
            lock (gate)
                return sourceMutationPermission ??= new ModuleSourceMutationPermission(this, sourceClosing);
        }
    }

    private void RequireSourceAdmissionCaller()
    {
        if (NifImageInitializationFlow.HoldsPermission(CodeModification))
            throw new NifImageAdmissionException(NifImageAdmissionFailure.ReentrantInitializationOperation);
    }

    private void ThrowIfSourceClosing()
    {
        if (Volatile.Read(ref sourceClosing))
            throw new ModuleSourceMutationAdmissionException(ModuleSourceMutationFailure.RuntimeClosing, this);
    }

    internal ModuleSourceMutationScope EnterSourceMutation()
    {
        RequireSourceAdmissionCaller();
        ThrowIfSourceClosing();
        ModuleSourceMutationPermission permission = SourceMutation;
        if (ModuleSourceMutationScope.Current(permission) is { } current)
            return new(current, false);
        if (CodeModification.IsOwned)
        {
            var frame = ModuleRegistryApplyFrame.Find(this)
                ?? throw new ModuleSourceMutationAdmissionException(ModuleSourceMutationFailure.WriterOrder);

            return new(frame.Adopt(permission), false);
        }
        if (Monitor.IsEntered(gate))
            throw new ModuleSourceMutationAdmissionException(ModuleSourceMutationFailure.WriterOrder);
        ModuleSourceMutationTicket ticket = permission.TryAcquire();
        try
        {
            return new(ticket, true);
        }
        catch
        {
            ticket.Dispose();
            throw;
        }
    }

    internal void RequireSourceMutationScope()
    {
        if (sourceMutationPermission is not { } permission || ModuleSourceMutationScope.Current(permission) is null)
            throw new ModuleSourceMutationAdmissionException(ModuleSourceMutationFailure.WriterOrder);
    }

    internal void ApplySourceCode(Action<ModuleRegistry> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        using var source = EnterSourceMutation();
        Apply(configure);
    }

    private void RequireAsyncSourceCaller()
    {
        RequireSourceAdmissionCaller();
        if (CodeModification.IsOwned || Monitor.IsEntered(gate))
            throw new ModuleSourceMutationAdmissionException(ModuleSourceMutationFailure.WriterOrder);
    }

    internal async ValueTask<T> ModifySourceCodeAsync<T>(Func<T> operation, CancellationToken cancellation = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        RequireAsyncSourceCaller();
        using var source = await SourceMutation.AcquireAsync(cancellation).ConfigureAwait(false);
        using var writer = await CodeModification.AcquireAsync(cancellation).ConfigureAwait(false);

        return writer.Run(() =>
        {
            using var scope = new ModuleSourceMutationScope(source, false);
            ThrowIfSourceClosing();

            return operation();
        });
    }

    internal async ValueTask<T> ModifySourceCodeAsync<T>(ProcessContext context, Func<T> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        RequireAsyncSourceCaller();
        context.Runtime.ThrowIfHalted();
        try
        {
            using var source = await AcquireSourceMutationAsync(context).ConfigureAwait(false);
            using var writer = await AcquireCodeModificationAsync(context).ConfigureAwait(false);

            return writer.Run(() =>
            {
                using var scope = new ModuleSourceMutationScope(source, false);
                context.Cancellation.ThrowIfCancellationRequested();
                ThrowIfSourceClosing();

                return operation();
            });
        }
        catch (ModuleSourceMutationAdmissionException failure) when (failure.Failure == ModuleSourceMutationFailure.RuntimeClosing && ReferenceEquals(failure.Registry, this))
        {
            throw new ModuleSourceMutationClosedException(this, context.Cancellation);
        }
    }

    private async ValueTask<ModuleSourceMutationTicket> AcquireSourceMutationAsync(ProcessContext context)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(context.Cancellation);
        Task<ModuleSourceMutationTicket> pending = SourceMutation.AcquireAsync(cancellation.Token).AsTask();
        try
        {
            await context.Mailbox.WaitForControlCompletion(pending, context.Cancellation);
            ModuleSourceMutationTicket ticket = await pending.ConfigureAwait(false);
            try
            {
                context.Cancellation.ThrowIfCancellationRequested();

                return ticket;
            }
            catch
            {
                ticket.Dispose();
                throw;
            }
        }
        catch
        {
            cancellation.Cancel();
            try
            {
                using var abandoned = await pending.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            throw;
        }
    }

    internal SourcePurgeFenceSession? PreparePurge(string module)
    {
        RequireAsyncSourceCaller();
        ModuleSourceMutationTicket ticket = SourceMutation.TryAcquire();
        bool transferred = false;
        try
        {
            using var source = new ModuleSourceMutationScope(ticket, false);
            using var writer = CodeModification.Enter();
            SourcePurgeFenceSession? session = PreparePurgeCore(module, ticket);
            transferred = session is not null;

            return session;
        }
        finally
        {
            if (!transferred)
                ticket.Dispose();
        }
    }

    internal async ValueTask<SourcePurgeFenceSession?> PreparePurgeAsync(ProcessContext context, string module)
    {
        RequireAsyncSourceCaller();
        context.Runtime.ThrowIfHalted();
        ModuleSourceMutationTicket? ticket = null;
        bool transferred = false;
        try
        {
            ticket = await AcquireSourceMutationAsync(context).ConfigureAwait(false);
            using var writer = await AcquireCodeModificationAsync(context).ConfigureAwait(false);
            SourcePurgeFenceSession? session = writer.Run(() =>
            {
                using var source = new ModuleSourceMutationScope(ticket, false);
                context.Cancellation.ThrowIfCancellationRequested();

                return PreparePurgeCore(module, ticket);
            });
            transferred = session is not null;

            return session;
        }
        catch (ModuleSourceMutationAdmissionException failure) when (failure.Failure == ModuleSourceMutationFailure.RuntimeClosing && ReferenceEquals(failure.Registry, this))
        {
            throw new ModuleSourceMutationClosedException(this, context.Cancellation);
        }
        finally
        {
            if (!transferred)
                ticket?.Dispose();
        }
    }

    private SourcePurgeFenceSession? PreparePurgeCore(string module, ModuleSourceMutationTicket ticket)
    {
        lock (gate)
        {
            ThrowIfSourceClosing();
            if (!oldCode.TryGetValue(module, out var old))
                return null;
            var state = purgeStates.GetOrCreateValue(old.Image);
            if (state.Pending is not null || sourcePurgeSessions.ContainsKey(old.Image))
                throw new ModuleSourceMutationAdmissionException(ModuleSourceMutationFailure.InvalidSession);
            var session = new SourcePurgeFenceSession(
                this,
                module,
                old.Image,
                ticket
            );
            sourcePurgeSessions.EnsureCapacity(checked(sourcePurgeSessions.Count + 1));
            ticket.TransferToSession();
            sourcePurgeSessions.Add(old.Image, session);
            state.Session = session;
            state.Pending = session.Pending;

            return session;
        }
    }

    internal bool FinishPurge(SourcePurgeFenceSession session, bool complete)
    {
        ArgumentNullException.ThrowIfNull(session);
        RequireAsyncSourceCaller();
        bool release = false;
        try
        {
            using var writer = CodeModification.Enter();

            return FinishPurgeCore(session, complete, out release);
        }
        finally
        {
            if (release)
                ReleasePurgeSession(session);
        }
    }

    internal async ValueTask<bool> FinishPurgeAsync(SourcePurgeFenceSession session, bool complete)
    {
        ArgumentNullException.ThrowIfNull(session);
        RequireAsyncSourceCaller();
        bool release = false;
        try
        {
            using var writer = await CodeModification.AcquireAsync(CancellationToken.None).ConfigureAwait(false);

            return writer.Run(() => FinishPurgeCore(session, complete, out release));
        }
        finally
        {
            if (release)
                ReleasePurgeSession(session);
        }
    }

    private static void ReleasePurgeSession(SourcePurgeFenceSession session)
    {
        session.Ticket.Dispose();
        session.Resume();
    }

    private bool FinishPurgeCore(SourcePurgeFenceSession session, bool complete, out bool release)
    {
        release = false;
        lock (gate)
        {
            if (!ReferenceEquals(session.Registry, this)
                || !purgeStates.TryGetValue(session.Image, out var state)
                || !ReferenceEquals(state.Session, session)
                || !ReferenceEquals(session.Ticket.Permission.Registry, this)
                || session.Generation != session.Ticket.Generation)
                throw new ModuleSourceMutationAdmissionException(ModuleSourceMutationFailure.InvalidSession);
            if (session.Settled)
                return session.Completed;
            if (!sourcePurgeSessions.TryGetValue(session.Image, out var active) || !ReferenceEquals(active, session)
                || !ReferenceEquals(state.Pending, session.Pending)
                || !oldCode.TryGetValue(session.Module, out var old) || !ReferenceEquals(old.Image, session.Image))
                throw new ModuleSourceMutationAdmissionException(ModuleSourceMutationFailure.InvalidSession);
            session.Ticket.RequireSession(session.Generation);
            if (complete)
            {
                _ = QueueNifRetirement(session.Image);
                oldCode.Remove(session.Module);
                state.Retired = true;
            }
            state.Pending = null;
            sourcePurgeSessions.Remove(session.Image);
            session.Settle(complete);
            release = true;

            return complete;
        }
    }

    internal void RequireSourceShutdownCaller()
    {
        if (!Monitor.IsEntered(gate) && !(codeModificationPermission?.IsOwned ?? false))
            return;
        lock (gate)
            if (sourcePurgeSessions.Count != 0
                || sourceShutdown is { Task.IsCompleted: false }
                || sourceMutationPermission is { } permission && ModuleSourceMutationScope.Current(permission) is not null
                || ModuleRegistryApplyFrame.Find(this)?.HasAdoptedTicket == true)
                throw new ModuleSourceMutationAdmissionException(ModuleSourceMutationFailure.ReentrantShutdown);
    }

    internal Task CloseSourceMutationsAsync()
    {
        RequireSourceShutdownCaller();
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Volatile.Write(ref sourceClosing, true);
        if (Interlocked.CompareExchange(ref sourceShutdown, completion, null) is { } existing)
        {
            RequireSourceShutdownCaller();

            return existing.Task;
        }
        if (Monitor.IsEntered(gate))
            _ = CloseSourceMutationsCoreAsync(completion);
        else
        {
            try
            {
                _ = Task.Run(NifImageInitializationFlow.IsolateActor(
                    NifRetirementFlow.IsolateActor(() => CloseSourceMutationsCoreAsync(completion))
                ));
            }
            catch (Exception failure)
            {
                completion.TrySetException(failure);
            }
        }

        return completion.Task;
    }

    private async Task CloseSourceMutationsCoreAsync(TaskCompletionSource completion)
    {
        try
        {
            SourcePurgeFenceSession[] sessions;
            ModuleSourceMutationPermission? permission;
            lock (gate)
            {
                sessions = sourcePurgeSessions.Values.ToArray();
                permission = sourceMutationPermission;
            }
            permission?.Close();
            var failures = new List<Exception>();
            foreach (SourcePurgeFenceSession session in sessions)
            {
                try
                {
                    await FinishPurgeAsync(session, false).ConfigureAwait(false);
                }
                catch (Exception failure)
                {
                    failures.Add(failure);
                }
            }
            if (failures.Count == 0)
                completion.TrySetResult();
            else
                completion.TrySetException(new AggregateException(failures));
        }
        catch (Exception failure)
        {
            completion.TrySetException(failure);
        }
    }
}
