using System.Collections;
using System.Reflection;
using Erlang;

namespace Erlang.Tests;

public static class BeamOnLoadCallbackFaultHostChecks
{
    public static async Task Run()
    {
        BeamOnLoadResourceHashChecks.Run();
        await Observe(highLevel: false);
        await Observe(highLevel: true);
    }

    private static async Task Observe(bool highLevel)
    {
        var runtime = new ProcessRuntime();
        var ready = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var finish = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle observer = runtime.Spawn(async context =>
        {
            ready.TrySetResult(context);

            return await finish.Task;
        });
        ProcessContext controller = await ready.Task.WaitAsync(BeamOnLoadCallbackFaultHostNames.Deadline);
        bool injected = false;
        try
        {
            runtime.Register(BeamOnLoadHostNames.Observer, observer.Pid);
            byte[] bytes = Resource();
            BeamOnLoadTicket ticket;
            ProcessHandle? callback = null;
            Task<BeamOnLoadResult>? loading = null;
            if (highLevel)
                loading = runtime.Modules.LoadPreparedOnLoadAsync(runtime, runtime.Modules.PrepareBeam(bytes));
            else
            {
                ticket = runtime.Modules.StagePreparedOnLoad(runtime.Modules.PrepareBeam(bytes));
                callback = runtime.Modules.StartPendingOnLoad(runtime, ticket);
            }
            var notice = (TupleTerm)(await controller.ReceiveAsync(
                term => term is TupleTerm { Items.Count: 5 } tuple &&
                    tuple.Items[0].Equals(Term.A(BeamOnLoadHostNames.Notice)) ? tuple : null,
                BeamOnLoadCallbackFaultHostNames.Deadline
            ))!;
            Pid pid = (Pid)notice.Items[2];
            object runtimeGate = Field(runtime, BeamOnLoadCallbackFaultHostNames.Gate);
            ProcessContext actor;
            lock (runtimeGate)
                actor = (ProcessContext)((IDictionary)Field(runtime, BeamOnLoadCallbackFaultHostNames.Processes))[pid]!;
            object registryGate = Field(runtime.Modules, BeamOnLoadCallbackFaultHostNames.Gate);
            lock (registryGate)
                ticket = (BeamOnLoadTicket)((IDictionary)Field(runtime.Modules, BeamOnLoadCallbackFaultHostNames.Pending))[BeamOnLoadHostNames.FreshSuccessModule]!;
            _ = actor.Cancellation.Register(() => throw new InvalidOperationException(BeamOnLoadCallbackFaultHostNames.InjectedFailure));
            injected = true;
            runtime.Send(pid, Term.Tuple(Term.A(BeamOnLoadHostNames.Continue), observer.Pid));
            BeamOnLoadCallbackOutcome outcome = await ticket.Outcome.WaitAsync(BeamOnLoadCallbackFaultHostNames.Deadline);
            Check(outcome.Returned is Atom { Name: BeamOnLoadHostNames.Ok } && outcome.Failure is null);
            if (highLevel)
                await ExpectInjected(loading!);
            else
                await ExpectInjected(callback!.Completion);
            await ExpectInjected(ticket.CallbackQuiescence);
            Check(ticket.CallbackQuiescence.IsFaulted && !runtime.IsAlive(pid));
            if (!highLevel)
            {
                // A completed but faulted callback cannot be published by the manual keep path.
                await ExpectInjected(Task.Run(() => runtime.Modules.FinishPendingOnLoad(ticket, true)));
                Check(!await Task.Run(() => runtime.Modules.FinishPendingOnLoad(ticket, false))
                    .WaitAsync(BeamOnLoadCallbackFaultHostNames.Deadline));
            }
            Check(ticket.Phase == BeamOnLoadPhase.Retired);
            Check(!runtime.Modules.IsModuleLoaded(BeamOnLoadHostNames.FreshSuccessModule) &&
                !runtime.Modules.HasExport(BeamOnLoadHostNames.FreshSuccessModule, BeamOnLoadHostNames.Run, 0));
            // This requires both real pending-image retirement and released code-modification permission.
            BeamOnLoadTicket next = await Task.Run(() => runtime.Modules.StagePreparedOnLoad(runtime.Modules.PrepareBeam(bytes)))
                .WaitAsync(BeamOnLoadCallbackFaultHostNames.Deadline);
            Check(next.Phase == BeamOnLoadPhase.Staged);
            Check(!await Task.Run(() => runtime.Modules.FinishPendingOnLoad(next, false))
                .WaitAsync(BeamOnLoadCallbackFaultHostNames.Deadline));
        }
        finally
        {
            // Baseline red runs must not wait on the broken CallbackQuiescence promise during cleanup.
            finish.TrySetResult(Term.A(BeamOnLoadHostNames.Ok));
            await observer.Completion.WaitAsync(BeamOnLoadCallbackFaultHostNames.Deadline);
            try
            {
                await runtime.DisposeAsync().AsTask().WaitAsync(BeamOnLoadCallbackFaultHostNames.Deadline);
                Check(!injected);
            }
            catch (Exception exception) when (injected && OnlyInjected(exception))
            {
                // The runtime also retains the injected cleanup failure; accepting it here preserves the primary test result.
            }
        }
    }

    private static async Task ExpectInjected(Task operation)
    {
        try
        {
            await operation.WaitAsync(BeamOnLoadCallbackFaultHostNames.Deadline);
        }
        catch (Exception exception) when (OnlyInjected(exception))
        {
            return;
        }

        throw new InvalidOperationException(BeamOnLoadCallbackFaultHostNames.Failure);
    }

    private static bool OnlyInjected(Exception exception)
    {
        if (exception is AggregateException aggregate)
            return aggregate.InnerExceptions.Count != 0 && aggregate.Flatten().InnerExceptions.All(OnlyInjected);

        return exception is InvalidOperationException && exception.Message == BeamOnLoadCallbackFaultHostNames.InjectedFailure;
    }

    private static byte[] Resource()
    {
        Assembly assembly = typeof(BeamOnLoadHostChecks).Assembly;
        string resource = assembly.GetManifestResourceNames().Single(name => name.EndsWith(
            BeamOnLoadCallbackFaultHostNames.ResourceSuffix,
            StringComparison.Ordinal
        ));
        using Stream stream = assembly.GetManifestResourceStream(resource)!;
        using var output = new MemoryStream();
        stream.CopyTo(output);

        return output.ToArray();
    }

    private static object Field(object target, string name) => target.GetType().GetField(
        name,
        BindingFlags.Instance | BindingFlags.NonPublic
    )!.GetValue(target)!;

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(BeamOnLoadCallbackFaultHostNames.Failure);
    }
}
