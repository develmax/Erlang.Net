using Erlang.Compiler;

namespace Erlang.Tests;

public static partial class SourcePurgeStageHostChecks
{
    public static async Task Run()
    {
        await RelinkAfterPrepare();
        SourcePurgeStageHostBridge.Preflight();
        await BindingBeforePrepare();
        await StaleSession();
        await SettlementReleasesWriter();
        await ApplyAdoptionAndRollback();
        await BusyApplyAndTickets();
        await UnrelatedRetainedWriter();
        await CancellationAndGuestPrecedence();
        await HardPurgeCleanup(false);
        await HardPurgeCleanup(true);
        await PendingShutdownReentry();
        await UnrelatedCancellation();
        await UncaughtShutdown(false);
        await UncaughtShutdown(true);
        await CloseSessions(false);
        await CloseSessions(true);
    }

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(SourcePurgeStageHostNames.Broken);
    }

    private static ModuleDefinition Definition(string source = SourcePurgeStageHostNames.Source) => new Parser(source).ParseModule();

    private static void Refused(Action operation, string reason)
    {
        try
        {
            operation();
        }
        catch (Exception failure) when (SourcePurgeStageHostBridge.Is(failure, reason))
        {
            return;
        }
        throw new InvalidOperationException(SourcePurgeStageHostNames.Broken);
    }

    private static async Task RelinkAfterPrepare()
    {
        await using var runtime = new ProcessRuntime();
        Definition().Register(runtime.Modules);
        await NifAdmissionHostBridge.Context(
            runtime,
            async context =>
        {
            var held = (FunctionTerm)await runtime.Modules.Call(context, SourcePurgeStageHostNames.Module, SourcePurgeStageHostNames.Make);
            runtime.Modules.DeleteModule(SourcePurgeStageHostNames.Module);
            object session = SourcePurgeStageHostBridge.Prepare(runtime.Modules);
            ModuleCodeImage image = SourcePurgeStageHostBridge.Image(session);
            Task<Term>? invocation = null;
            try
            {
                bool refused = false;
                try
                {
                    Definition().Register(runtime.Modules);
                }
                catch (Exception failure) when (SourcePurgeStageHostBridge.Is(failure, SourcePurgeStageHostNames.StageBusy))
                {
                    refused = true;
                }
                invocation = held.Invoke(context, []).AsTask();
                Require(refused && !invocation.IsCompleted && runtime.Modules.CurrentCodeImage(SourcePurgeStageHostNames.Module) is null);
                Require(runtime.Modules.PendingPurge(image) is { IsCompleted: false });
            }
            finally
            {
                SourcePurgeStageHostBridge.Finish(runtime.Modules, session);
                if (invocation is not null)
                    Require((await invocation.WaitAsync(SourcePurgeStageHostNames.Deadline)).Equals(Term.A(SourcePurgeStageHostNames.Kept)));
            }
            Definition().Register(runtime.Modules);
            Require((await held.Invoke(context, [])).Equals(Term.A(SourcePurgeStageHostNames.Kept)));
        }
        );
    }

    private static async Task BindingBeforePrepare()
    {
        await using var runtime = new ProcessRuntime();
        Definition().Register(runtime.Modules);
        await NifAdmissionHostBridge.Context(
            runtime,
            async context =>
        {
            var held = (FunctionTerm)await runtime.Modules.Call(context, SourcePurgeStageHostNames.Module, SourcePurgeStageHostNames.Make);
            runtime.Modules.DeleteModule(SourcePurgeStageHostNames.Module);
            Definition().Register(runtime.Modules);
            ModuleCodeImage current = runtime.Modules.CurrentCodeImage(SourcePurgeStageHostNames.Module)!;
            object session = SourcePurgeStageHostBridge.Prepare(runtime.Modules);
            try
            {
                Require(!ReferenceEquals(current, SourcePurgeStageHostBridge.Image(session)));
                Require((await held.Invoke(context, []).AsTask().WaitAsync(SourcePurgeStageHostNames.Deadline)).Equals(Term.A(SourcePurgeStageHostNames.Kept)));
            }
            finally
            {
                SourcePurgeStageHostBridge.Finish(runtime.Modules, session);
            }
        }
        );
    }

    private static async Task StaleSession()
    {
        await using var runtime = new ProcessRuntime();
        Definition().Register(runtime.Modules);
        runtime.Modules.DeleteModule(SourcePurgeStageHostNames.Module);
        object first = SourcePurgeStageHostBridge.Prepare(runtime.Modules);
        SourcePurgeStageHostBridge.Finish(runtime.Modules, first);
        object second = SourcePurgeStageHostBridge.Prepare(runtime.Modules);
        ModuleCodeImage image = SourcePurgeStageHostBridge.Image(second);
        Task pending = runtime.Modules.PendingPurge(image)!;
        try
        {
            Require(ReferenceEquals(image, SourcePurgeStageHostBridge.Image(first)));
            Require((long)SourcePurgeStageHostBridge.Property(second, SourcePurgeStageHostNames.Generation)
                > (long)SourcePurgeStageHostBridge.Property(first, SourcePurgeStageHostNames.Generation));
            Refused(() => SourcePurgeStageHostBridge.Finish(runtime.Modules, first), SourcePurgeStageHostNames.InvalidSession);
            Require(ReferenceEquals(pending, runtime.Modules.PendingPurge(image)) && !pending.IsCompleted);
            Refused(() => Definition().Register(runtime.Modules), SourcePurgeStageHostNames.StageBusy);
            Refused(
                () => SourcePurgeStageHostBridge.ScopeFor(SourcePurgeStageHostBridge.Property(second, SourcePurgeStageHostNames.Ticket)),
                SourcePurgeStageHostNames.InvalidTicket
            );
        }
        finally
        {
            SourcePurgeStageHostBridge.Finish(runtime.Modules, second);
        }
        Require(pending.IsCompleted && runtime.Modules.PendingPurge(image) is null);
    }
}
