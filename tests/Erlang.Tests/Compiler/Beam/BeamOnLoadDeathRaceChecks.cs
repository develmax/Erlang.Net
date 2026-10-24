using System.Collections;
using System.Reflection;
using Erlang;
namespace Erlang.Tests;

internal static class BeamOnLoadDeathRaceChecks
{
    internal static async Task Run(byte[] genuine)
    {
        await using var runtime = new ProcessRuntime();
        var ready = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var finish = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle observer = runtime.Spawn(async context =>
        {
            ready.TrySetResult(context);

            return await finish.Task;
        });
        ProcessContext controller = await ready.Task;
        Task<BeamOnLoadResult>? loading = null;
        try
        {
            runtime.Register(BeamOnLoadHostNames.Observer, observer.Pid);
            loading = runtime.Modules.LoadPreparedOnLoadAsync(runtime, runtime.Modules.PrepareBeam(genuine));
            TupleTerm notice = (TupleTerm)(await controller.ReceiveAsync(
                term => term is TupleTerm tuple && tuple.Items.Count == 5
                && tuple.Items[0].Equals(Term.A(BeamOnLoadHostNames.Notice)) ? term : null,
                TimeSpan.FromSeconds(BeamOnLoadRaceNames.WatchdogSeconds)
            ))!;
            Pid callback = (Pid)notice.Items[2];
            object runtimeGate = Field(runtime, BeamOnLoadRaceNames.Gate);
            object registryGate = Field(runtime.Modules, BeamOnLoadRaceNames.Gate);
            ProcessContext actor;
            lock (runtimeGate)
                actor = (ProcessContext)((IDictionary)Field(runtime, BeamOnLoadRaceNames.Processes))[callback]!;
            BeamOnLoadTicket ticket;
            lock (registryGate)
                ticket = (BeamOnLoadTicket)((IDictionary)Field(runtime.Modules, BeamOnLoadRaceNames.Pending))[BeamOnLoadHostNames.FreshSuccessModule]!;
            ModuleCodeImage image = (ModuleCodeImage)typeof(BeamOnLoadTicket).GetProperty(BeamOnLoadRaceNames.CodeImage, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(ticket)!;
            bool runtimeHeld = false;
            bool registryHeld = false;
            try
            {
                Monitor.Enter(registryGate);
                registryHeld = true;
                runtime.Send(callback, Term.Tuple(Term.A(BeamOnLoadHostNames.Continue), observer.Pid));
                Require(
                    SpinWait.SpinUntil(() => !Executing(actor, image), TimeSpan.FromSeconds(BeamOnLoadRaceNames.WatchdogSeconds)),
                    BeamOnLoadRaceNames.LeaseNotReleased
                );
                Require(!ticket.Outcome.IsCompleted, BeamOnLoadRaceNames.OutcomeNotRecorded);
                Require(
                    Monitor.TryEnter(runtimeGate, TimeSpan.FromSeconds(BeamOnLoadRaceNames.WatchdogSeconds)),
                    BeamOnLoadRaceNames.OutcomeNotRecorded
                );
                runtimeHeld = true;
                Monitor.Exit(registryGate);
                registryHeld = false;
                Require(
                    SpinWait.SpinUntil(() => ticket.Outcome.IsCompleted, TimeSpan.FromSeconds(BeamOnLoadRaceNames.WatchdogSeconds)),
                    BeamOnLoadRaceNames.OutcomeNotRecorded
                );
                BeamOnLoadCallbackOutcome cached = ticket.Outcome.GetAwaiter().GetResult();
                Require(cached.Failure is null && cached.Returned is Atom { Name: BeamOnLoadHostNames.Ok }, BeamOnLoadRaceNames.OutcomeNotRecorded);
                runtime.Exit(controller, callback, Term.A(ProcessExitSignals.Kill));
            }
            finally
            {
                if (registryHeld)
                    Monitor.Exit(registryGate);
                if (runtimeHeld)
                    Monitor.Exit(runtimeGate);
            }
            BeamOnLoadResult result = await loading.WaitAsync(TimeSpan.FromSeconds(BeamOnLoadRaceNames.WatchdogSeconds));
            Require(
                !result.Published && result.DownReason.Equals(Term.A(ProcessExitReasons.Killed))
                && result.Outcome.Returned is Atom { Name: BeamOnLoadHostNames.Ok }
                && !runtime.Modules.IsModuleLoaded(BeamOnLoadHostNames.FreshSuccessModule)
                && ticket.Phase == BeamOnLoadPhase.Retired
                && ticket.CallbackQuiescence.IsCompleted,
                BeamOnLoadRaceNames.UnexpectedPublication
            );
        }
        finally
        {
            if (loading is not null && !loading.IsCompleted)
            {
                _ = runtime.RequestImmediateHalt(0);
                try
                {
                    _ = await loading.WaitAsync(TimeSpan.FromSeconds(BeamOnLoadRaceNames.WatchdogSeconds));
                }
                catch (Exception) { }
            }
            finish.TrySetResult(Term.A(BeamOnLoadHostNames.Ok));
            _ = await observer.Completion.WaitAsync(TimeSpan.FromSeconds(BeamOnLoadRaceNames.WatchdogSeconds));
        }
    }

    private static object Field(object target, string name) => target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(target)
        ?? throw new InvalidOperationException(BeamOnLoadRaceNames.Missing);

    private static bool Executing(ProcessContext context, ModuleCodeImage image) => (bool)typeof(ProcessContext)
        .GetMethod(BeamOnLoadRaceNames.Executing, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(context, [image])!;

    private static void Require(bool value, string message)
    {
        if (!value)
            throw new InvalidOperationException(message);
    }
}
