namespace Erlang;

public sealed partial class ModuleRegistry
{
    private readonly Dictionary<string, BeamOnLoadTicket> pendingBeamImages = new(StringComparer.Ordinal);

    public BeamOnLoadTicket StagePreparedOnLoad(PreparedBeamImage image)
    {
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
        Term down = await callback.Completion.ConfigureAwait(false);
        await unwind.ConfigureAwait(false);
        lock (gate)
        {
            if (!ticket.OutcomeCompletion.Task.IsCompleted)
            {
                ticket.CurrentPhase = BeamOnLoadPhase.Completed;
                ticket.OutcomeCompletion.TrySetResult(new(callback.Pid, null, new BeamOnLoadCallbackAbortedException(down)));
            }
            ticket.CallbackUnwound.TrySetResult();
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
                    Array.Empty<Term>()
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
                    registry.Register(
                        ticket.Module,
                        function.Name,
                        function.Arity,
                        ticket.Prepared.Bodies[index]
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
        BeamOnLoadTicket ticket = StagePreparedOnLoad(image);
        ProcessHandle callback;
        try
        {
            callback = StartPendingOnLoad(runtime, ticket);
        }
        catch
        {
            FinishPendingOnLoad(ticket, false);
            throw;
        }
        BeamOnLoadCallbackOutcome outcome = await ticket.Outcome.ConfigureAwait(false);
        Term down = await callback.Completion.ConfigureAwait(false);
        await ticket.CallbackQuiescence.ConfigureAwait(false);
        bool keep = down is Atom { Name: BeamOnLoadNames.Ok };
        try
        {
            bool published = FinishPendingOnLoad(ticket, keep);

            return new(
                published,
                callback.Pid,
                outcome,
                down
            );
        }
        catch
        {
            if (ticket.Phase == BeamOnLoadPhase.Completed)
                FinishPendingOnLoad(ticket, false);
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
    }
}
