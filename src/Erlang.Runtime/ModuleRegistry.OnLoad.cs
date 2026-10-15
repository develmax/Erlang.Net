namespace Erlang;

public sealed partial class ModuleRegistry
{
    private readonly Dictionary<string, BeamOnLoadTicket> pendingBeamImages = new(StringComparer.Ordinal);

    public BeamOnLoadTicket StagePreparedOnLoad(PreparedBeamImage image)
    {
        using var modification = CodeModification.Enter();
        ArgumentNullException.ThrowIfNull(image);
        lock (gate)
        {
            if (!ReferenceEquals(image.Owner, this))
                throw new ArgumentException(BeamPreparationNames.ForeignImage, nameof(image));
            if (image.Consumed)
                throw new InvalidOperationException(BeamPreparationNames.ConsumedImage);
            if (!image.HasOnLoad)
                throw new InvalidOperationException(BeamOnLoadDiagnostics.OnLoadRequired);
            if (pendingBeamImages.ContainsKey(image.Name))
                throw new InvalidOperationException(BeamOnLoadDiagnostics.BusyModule);
            if (loadedModules.ContainsKey(image.Name) && oldCode.ContainsKey(image.Name))
                throw new InvalidOperationException(BeamOnLoadDiagnostics.OldGeneration);
            var generation = new BeamPendingGeneration(
                codeImages.GetValueOrDefault(image.Name),
                oldCode.GetValueOrDefault(image.Name)?.Image,
                codeDescriptors.GetValueOrDefault(image.Name),
                functions.Where(entry => entry.Key.Module == image.Name).ToDictionary(entry => entry.Key, entry => entry.Value)
            );
            var ticket = new BeamOnLoadTicket(this, image, generation);
            pendingBeamImages.Add(image.Name, ticket);
            image.Consumed = true;

            return ticket;
        }
    }

    public ProcessHandle StartPendingOnLoad(ProcessRuntime runtime, BeamOnLoadTicket ticket)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(ticket);
        if (!ReferenceEquals(runtime.Modules, this))
            throw new ArgumentException(BeamOnLoadDiagnostics.ForeignRuntime, nameof(runtime));
        lock (gate)
        {
            RequirePending(ticket);
            if (ticket.CurrentPhase != BeamOnLoadPhase.Staged)
                throw new InvalidOperationException(BeamOnLoadDiagnostics.InvalidState);
            ticket.CurrentPhase = BeamOnLoadPhase.Running;
        }
        ProcessHandle callback;
        try
        {
            callback = runtime.Spawn(context => RunPendingOnLoad(context, ticket));
        }
        catch
        {
            lock (gate)
                ticket.CurrentPhase = BeamOnLoadPhase.Staged;
            throw;
        }
        lock (gate)
            ticket.CallbackOwner = callback;
        Task unwind = runtime.CallbackExecutionUnwind(callback.Pid);
        _ = ObservePendingExit(ticket, callback, unwind);

        return callback;
    }

    private async Task ObservePendingExit(BeamOnLoadTicket ticket, ProcessHandle callback, Task unwind)
    {
        Term? down = null;
        Exception? failure = null;
        try
        {
            down = await callback.Completion.ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            failure = exception;
        }
        try
        {
            await unwind.ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            failure = failure is null ? exception : new AggregateException(failure, exception);
        }
        lock (gate)
        {
            if (!ticket.OutcomeCompletion.Task.IsCompleted)
            {
                ticket.CurrentPhase = BeamOnLoadPhase.Completed;
                ticket.OutcomeCompletion.TrySetResult(new(
                    callback.Pid,
                    null,
                    failure ?? new BeamOnLoadCallbackAbortedException(down!)
                ));
            }
            // Faults remain observable through the public ticket; the discarded observer task must still settle quiescence.
            if (failure is null)
                ticket.CallbackUnwound.TrySetResult();
            else
                ticket.CallbackUnwound.TrySetException(failure);
        }
    }

    private async ValueTask<Term> RunPendingOnLoad(ProcessContext context, BeamOnLoadTicket ticket)
    {
        Term? returned = null;
        Exception? failure = null;
        try
        {
            IDisposable code;
            lock (gate)
            {
                RequirePending(ticket);
                if (ticket.CurrentPhase != BeamOnLoadPhase.Running)
                    throw new InvalidOperationException(BeamOnLoadDiagnostics.InvalidState);
                code = context.EnterSourceCode(ticket.CodeImage, ticket);
            }
            try
            {
                int label = ticket.Prepared.Code.OnLoadEntryLabel!.Value;
                BeamLinkedFunction function = ticket.Prepared.Code.Functions.Single(entry => entry.EntryLabel == label);
                returned = await BeamRegisterInterpreter.ExecuteLocal(
                    context,
                    ticket.Prepared.Code,
                    function,
                    Array.Empty<Term>(),
                    Array.Empty<Term>(),
                    ticket.Prepared.ExecutionIdentity
                );
            }
            finally
            {
                code.Dispose();
                context.RestoreSourceCodeFlow(code);
            }
        }
        catch (Exception exception)
        {
            failure = exception;
        }
        lock (gate)
        {
            ticket.CurrentPhase = BeamOnLoadPhase.Completed;
            ticket.OutcomeCompletion.TrySetResult(new(context.Self, returned, failure));
        }
        if (failure is not null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        throw new ErlangException(returned!, ErlangExceptionClasses.Exit);
    }

    public bool FinishPendingOnLoad(BeamOnLoadTicket ticket, bool keep)
    {
        using var modification = CodeModification.Enter();
        ArgumentNullException.ThrowIfNull(ticket);
        lock (gate)
        {
            RequirePending(ticket);
            if (ticket.CurrentPhase == BeamOnLoadPhase.Staged && !keep)
            {
                RetirePending(ticket);

                return false;
            }
            if (ticket.CurrentPhase != BeamOnLoadPhase.Completed)
                throw new InvalidOperationException(BeamOnLoadDiagnostics.InvalidState);
            if (ticket.CallbackOwner is null || !ticket.CallbackOwner.Completion.IsCompleted || !ticket.CallbackQuiescence.IsCompleted)
                throw new InvalidOperationException(BeamOnLoadDiagnostics.CallbackNotTerminated);
            if (!keep)
            {
                RetirePending(ticket);

                return false;
            }
            // Both tasks are already complete: propagate cleanup failure before publication, without waiting under the gate.
            ticket.CallbackOwner.Completion.GetAwaiter().GetResult();
            ticket.CallbackQuiescence.GetAwaiter().GetResult();
            BeamPendingGeneration captured = ticket.Generation;
            var currentBodies = functions.Where(entry => entry.Key.Module == ticket.Module).ToArray();
            if (!ReferenceEquals(codeImages.GetValueOrDefault(ticket.Module), captured.Current)
                || !ReferenceEquals(oldCode.GetValueOrDefault(ticket.Module)?.Image, captured.Old)
                || !ReferenceEquals(codeDescriptors.GetValueOrDefault(ticket.Module), captured.Descriptor)
                || currentBodies.Length != captured.Bodies.Count
                || currentBodies.Any(entry => !captured.Bodies.TryGetValue(entry.Key, out ErlangFunction? body) || !ReferenceEquals(body, entry.Value)))
                throw new InvalidOperationException(BeamOnLoadDiagnostics.GenerationChanged);
            if (loadedModules.ContainsKey(ticket.Module) && oldCode.ContainsKey(ticket.Module))
                throw new InvalidOperationException(BeamOnLoadDiagnostics.OldGeneration);
            Apply(registry =>
            {
                if (loadedModules.ContainsKey(ticket.Module))
                    registry.DeleteModule(ticket.Module);
                for (int index = 0; index < ticket.Prepared.Code.Exports.Count; index++)
                {
                    BeamLinkedFunction function = ticket.Prepared.Code.Exports[index];
                    registry.RegisterSource(
                        ticket.Module,
                        function.Name,
                        function.Arity,
                        ticket.Prepared.Bodies[index],
                        ticket.Prepared.ExecutionIdentity.Owner
                    );
                }
                registry.PublishModuleCode(ticket.Module, ticket.Prepared.Descriptor);
                codeImages[ticket.Module] = ticket.CodeImage;
            });
            ticket.CurrentPhase = BeamOnLoadPhase.Published;
            pendingBeamImages.Remove(ticket.Module);

            return true;
        }
    }

    public async Task<BeamOnLoadResult> LoadPreparedOnLoadAsync(ProcessRuntime runtime, PreparedBeamImage image)
    {
        BeamOnLoadTicket ticket = await ModifyCodeAsync(() => StagePreparedOnLoad(image)).ConfigureAwait(false);
        ProcessHandle callback;
        try
        {
            callback = StartPendingOnLoad(runtime, ticket);
        }
        catch (Exception failure)
        {
            try
            {
                await ModifyCodeAsync(() => FinishPendingOnLoad(ticket, false)).ConfigureAwait(false);
                await RetireNifImageAsync(ticket.CodeImage).ConfigureAwait(false);
            }
            catch (Exception cleanupFailure)
            {
                throw new AggregateException(failure, cleanupFailure);
            }
            throw;
        }
        try
        {
            BeamOnLoadCallbackOutcome outcome = await ticket.Outcome.ConfigureAwait(false);
            Term down = await callback.Completion.ConfigureAwait(false);
            await ticket.CallbackQuiescence.ConfigureAwait(false);
            bool keep = down is Atom { Name: BeamOnLoadNames.Ok };
            bool published = await ModifyCodeAsync(() => FinishPendingOnLoad(ticket, keep)).ConfigureAwait(false);
            if (!published)
                await RetireNifImageAsync(ticket.CodeImage).ConfigureAwait(false);

            return new(
                published,
                callback.Pid,
                outcome,
                down
            );
        }
        catch (Exception failure)
        {
            var failures = new List<Exception> { failure };
            try
            {
                await ticket.CallbackQuiescence.ConfigureAwait(false);
            }
            catch (Exception cleanupFailure)
            {
                if (!ReferenceEquals(failure, cleanupFailure))
                    failures.Add(cleanupFailure);
            }
            try
            {
                if (ticket.Phase == BeamOnLoadPhase.Completed)
                    await ModifyCodeAsync(() => FinishPendingOnLoad(ticket, false)).ConfigureAwait(false);
                if (ticket.Phase == BeamOnLoadPhase.Retired)
                    await RetireNifImageAsync(ticket.CodeImage).ConfigureAwait(false);
            }
            catch (Exception retirementFailure)
            {
                if (!failures.Any(error => ReferenceEquals(error, retirementFailure)))
                    failures.Add(retirementFailure);
            }
            if (failures.Count != 1)
                throw new AggregateException(failures);
            throw;
        }
    }

    internal BeamOnLoadPhase PendingOnLoadPhase(BeamOnLoadTicket ticket)
    {
        lock (gate)
        {
            if (!ReferenceEquals(ticket.Owner, this))
                throw new ArgumentException(BeamPreparationNames.ForeignImage, nameof(ticket));

            return ticket.CurrentPhase;
        }
    }

    private void RequirePending(BeamOnLoadTicket ticket)
    {
        if (!ReferenceEquals(ticket.Owner, this)
            || !pendingBeamImages.TryGetValue(ticket.Module, out BeamOnLoadTicket? current) || !ReferenceEquals(current, ticket))
            throw new InvalidOperationException(BeamOnLoadDiagnostics.InvalidState);
    }

    private void RetirePending(BeamOnLoadTicket ticket)
    {
        purgeStates.GetOrCreateValue(ticket.CodeImage).Retired = true;
        ticket.CurrentPhase = BeamOnLoadPhase.Retired;
        if (ticket.CallbackOwner is null)
            ticket.CallbackUnwound.TrySetResult();
        pendingBeamImages.Remove(ticket.Module);
        _ = QueueNifRetirement(ticket.CodeImage);
    }
}
