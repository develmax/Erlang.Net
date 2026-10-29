using Erlang.Differential;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.MonitorShutdown652Names;
using static Erlang.Tests.NativeResource524Access;

namespace Erlang.Tests;

internal static class MonitorShutdown652Checks
{
    internal static async Task EarlierTarget()
    {
        var fixture = new AtomicsP2a586Fixture();
        var failures = new List<Exception>();
        var actors = new List<RuntimeNodeFoundationHostActor>();
        IDisposable? producer = null;
        try
        {
            producer = fixture.Lease(AtomicsP2a586Names.ProducerKind);
            await fixture.Runtime.InitializeModulesAsync(_ => { });
            using var compiled = GeneratedModuleCompiler.Compile(Source, SourceFile);
            compiled.Definition.Register(fixture.Runtime.Modules);
            var target = new RuntimeNodeFoundationHostActor(fixture.Runtime);
            actors.Add(target);
            await target.Ready.WaitAsync(MonitorPayload641Names.Deadline);
            var observer = new RuntimeNodeFoundationHostActor(fixture.Runtime);
            actors.Add(observer);
            await observer.Ready.WaitAsync(MonitorPayload641Names.Deadline);
            var sentinel = new RuntimeNodeFoundationHostActor(fixture.Runtime);
            actors.Add(sentinel);
            await sentinel.Ready.WaitAsync(MonitorPayload641Names.Deadline);
            await observer.Invoke(context =>
            {
                context.Dictionary[Term.A(Stored)] = fixture.Reference;

                return true;
            });
            fixture.ReleaseProducer();
            producer.Dispose();
            producer = null;
            _ = await observer.InvokeAsync(async context => await fixture.Runtime.Modules.Call(
                context,
                Module,
                Arm,
                target.Process.Pid
            ));
            if (((Task)Property(fixture.Array, AtomicsP2a586Names.Retired)).IsCompleted)
                throw new InvalidOperationException(Failure);
            lock (ProcessPayloadCore641Access.Field(fixture.Runtime, RuntimeGate)!)
            {
                var processes = (Dictionary<Pid, ProcessContext>)ProcessPayloadCore641Access.Field(fixture.Runtime, ProcessTable)!;
                Pid[] order = processes.Keys.ToArray();
                int targetIndex = Array.IndexOf(order, target.Process.Pid);
                int observerIndex = Array.IndexOf(order, observer.Process.Pid);
                int sentinelIndex = Array.IndexOf(order, sentinel.Process.Pid);
                if (targetIndex < 0 || observerIndex <= targetIndex || sentinelIndex <= observerIndex)
                    throw new InvalidOperationException(Failure);
                var monitors = (System.Collections.IDictionary)ProcessPayloadCore641Access.Field(fixture.Runtime, MonitorTable)!;
                object entry = monitors.Values.Cast<object>().Single(value =>
                    ((Pid)Property(value, Observer)).Equals(observer.Process.Pid)
                    && ((Pid)Property(value, Target)).Equals(target.Process.Pid));
                object custody = Property(entry, Custody);
                if (custody is null || Property(custody, Graph) is null)
                    throw new InvalidOperationException(Failure);
            }
            fixture.ReleaseInvocationForShutdown();
            await fixture.Runtime.DisposeAsync().AsTask().WaitAsync(MonitorPayload641Names.Deadline);
            foreach (var actor in actors)
            {
                await actor.Process.Completion.WaitAsync(MonitorPayload641Names.Deadline);
                if (!(await actor.Ready).Cancellation.IsCancellationRequested)
                    throw new InvalidOperationException(Failure);
            }
            await ((Task)Property(fixture.Array, AtomicsP2a586Names.Retired)).WaitAsync(MonitorPayload641Names.Deadline);
            GC.KeepAlive(fixture.Reference);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            producer?.Dispose();
            foreach (var actor in actors)
                try
                {
                    await actor.DisposeAsync().AsTask().WaitAsync(MonitorPayload641Names.Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            await fixture.Drain(failures);
        }
        if (failures.Count == 1) ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1) throw new AggregateException(Failure, failures);
    }
}
