namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal bool IsWallTimeClosing
    {
        get
        {
            lock (gate) return disposed || Volatile.Read(ref haltResult) is not null;
        }
    }

    private RuntimeManagedWallTimeController? managedWallTimeController;
    private bool? originalWallTimeController;

    private Task EnsureDirtyWallTimeReady(ProcessContext context)
    {
        DirtyNativeOwner419 owner;
        lock (gate)
        {
            RequireWallTimeActor(context);
            owner = dirtyNative419 ??= new(dirtyIoSchedulerCount522, Statistics.Microstates);
        }

        return owner.EnsureWallTimeReady(context.Cancellation);
    }

    private void RequireWallTimeActor(ProcessContext context)
    {
        if (!ReferenceEquals(context.Runtime, this)) throw new ErlangException(ErlangErrorReasons.BadArgument);
        context.Cancellation.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(disposed, this);
        ThrowIfHalted();
        RequireAlive(context.Self);
    }

    internal ValueTask<Term> SetNativeSchedulerWallTime(ProcessContext context, bool enabled)
        => SetNativeWallTimeCore(context, enabled, null);

    internal ValueTask<Term> SetManagedNativeWallTime(ProcessContext context, bool enabled, RuntimeWallTimeCommand command)
        => SetNativeWallTimeCore(context, enabled, command);

    private async ValueTask<Term> SetNativeWallTimeCore(ProcessContext context, bool enabled, RuntimeWallTimeCommand? command)
    {
        if (enabled) await EnsureDirtyWallTimeReady(context);
        using StatisticsTrapAdmission helper = await Modules.AdmitStatisticsTrap(context, StatisticsTrapNames.AwaitWallTime, 2);
        var trap = new StatisticsTrapRequest(this, context, MakeReference());
        lock (gate)
            RequireWallTimeActor(context);
        bool previous;
        Task completion;
        try
        {
            (previous, completion) = actorDispatcher.ChangeWallTimeWithStatisticsTrap(context, enabled, trap);
            command?.RecordAcknowledgments(completion);
        }
        catch (Exception error)
        {
            return await trap.FailAndDrain(error);
        }

        return await trap.RunHelper(
            helper,
            StatisticsTrapNames.AwaitWallTime,
            completion,
            trap.Reference,
            CoreModules.Boolean(previous)
        );
    }

    private (bool Previous, Task Completion) BeginNativeWallTimeChange(ProcessContext context, bool enabled)
    {
        lock (gate)
        {
            RequireWallTimeActor(context);

            return actorDispatcher.ChangeWallTime(context, enabled);
        }
    }

    internal ValueTask<Term> SetPublicSchedulerWallTime(ProcessContext context, Term value)
    {
        if (value is not Atom { Name: ErlangBooleanAtoms.True or ErlangBooleanAtoms.False })
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        bool sourceAvailable = Modules.SourceOwner(
            RuntimeSchedulerWallTimeNames.InternalModule,
            RuntimeSchedulerWallTimeNames.PublicWrapper,
            RuntimeSchedulerWallTimeNames.WrapperArity
        ) is not null;
        bool useOriginal;
        RuntimeWallTimeFlagCommand? command = null;
        lock (gate)
        {
            RequireWallTimeActor(context);
            originalWallTimeController ??= sourceAvailable;
            useOriginal = originalWallTimeController.Value;
            if (!useOriginal)
            {
                managedWallTimeController ??= new(this);
                command = managedWallTimeController.Flag(context.Self, value is Atom { Name: ErlangBooleanAtoms.True });
            }
        }
        if (useOriginal)
            return Modules.Call(
                context,
                RuntimeSchedulerWallTimeNames.InternalModule,
                RuntimeSchedulerWallTimeNames.PublicWrapper,
                value
            );

        return AwaitManagedWallTimeFlag(context, command!);
    }

    private static async ValueTask<Term> AwaitManagedWallTimeFlag(ProcessContext context, RuntimeWallTimeFlagCommand command)
    {
        await WaitForSchedulerObservation(context, command.Result.Task);

        return CoreModules.Boolean(await command.Result.Task);
    }

    private Task PublicWallTimeProcessRetirement(ProcessContext context, Task previous)
    {
        if (managedWallTimeController is null || disposed || Volatile.Read(ref haltResult) is not null
            || managedWallTimeController.Process.Pid.Equals(context.Self)) return previous;

        return Task.WhenAll(previous, managedWallTimeController.Exit(context.Self));
    }

    internal async ValueTask<Term> SampleSchedulerWallTime(ProcessContext context, bool includeIo)
    {
        lock (gate)
        {
            RequireWallTimeActor(context);
            if (!actorDispatcher.RequestingLaneWallTimeEnabled(context)) return Term.A(CommonAtomNames.Undefined);
        }
        await EnsureDirtyWallTimeReady(context);
        using StatisticsTrapAdmission helper = await Modules.AdmitStatisticsTrap(context, StatisticsTrapNames.GatherWallTime, 1);
        var trap = new StatisticsTrapRequest(this, context, MakeReference());
        var observation = new RuntimeWallTimeObservation(ActorDispatchCapacity);
        lock (gate)
        {
            RequireWallTimeActor(context);
            if (!actorDispatcher.RequestingLaneWallTimeEnabled(context))
                return Term.A(CommonAtomNames.Undefined);
        }
        Task completion;
        try
        {
            completion = actorDispatcher.GatherWallTimeWithStatisticsTrap(
                context,
                observation,
                dirtyNative419!,
                includeIo,
                trap
            );
        }
        catch (Exception error)
        {
            return await trap.FailAndDrain(error);
        }

        return await trap.RunHelper(
            helper,
            StatisticsTrapNames.GatherWallTime,
            completion,
            trap.Reference
        );
    }
}
