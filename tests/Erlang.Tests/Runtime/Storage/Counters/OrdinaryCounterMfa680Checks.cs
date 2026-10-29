using System.Runtime.ExceptionServices;
using Erlang.Differential;
using static Erlang.Tests.OrdinaryCounterMfa680Names;
using static Erlang.Tests.ProcessPayloadCore641Access;

namespace Erlang.Tests;

internal static class OrdinaryCounterMfa680Checks
{
    internal static Task Run() => RunCore(Source);

    internal static Task DictionaryHibernate() => RunCore(SourceHibernate);

    private static async Task RunCore(string source)
    {
        using var compiled = GeneratedModuleCompiler.Compile(source);
        var runtime = new ProcessRuntime();
        var failures = new List<Exception>();
        RuntimeNodeFoundationHostActor? parent = null;
        ResourceReferenceTerm? reference = null;
        IDisposable? sourceHost = null;
        object? array = null;
        ProcessHandle? child = null;
        ProcessContext? childContext = null;
        try
        {
            Check(!(bool)Property(runtime, Profile));
            await runtime.InitializeModulesAsync(registry =>
            {
                PreparedBeamImage facet = CountersOriginal421Preparation.Prepare(runtime, registry);
                Check(registry.PublishPreparedBeam(facet).Status == BeamPublicationStatus.Published);
            });
            compiled.Definition.Register(runtime.Modules);
            parent = new RuntimeNodeFoundationHostActor(runtime);
            await parent.Ready.WaitAsync(Deadline);
            reference = (ResourceReferenceTerm)await parent.InvokeAsync(async context =>
                await runtime.Modules.Call(
                    context,
                    NativeModule,
                    Create,
                    Term.I(1)
                ));
            sourceHost = Property(reference, Host) as IDisposable ?? throw new InvalidOperationException(Failure);
            array = Property(Property(reference, Backing), ArrayField);
            Term started = await parent.InvokeAsync(async context =>
            {
                Term pair = await runtime.Modules.Call(
                    context,
                    Module,
                    Start,
                    context.Self,
                    reference
                );
                Check(pair is TupleTerm { Items: [Pid, ReferenceTerm] });
                Pid id = (Pid)((TupleTerm)pair).Items[0];
                Term? ready = await context.ReceiveAsync<Term>(message => message is TupleTerm
                {
                    Items: [Atom { Name: Ready }, Pid candidate]
                } && candidate.Equals(id) ? message : null, Deadline);
                Check(ready is not null);

                return pair;
            }).WaitAsync(Deadline);
            var ids = (TupleTerm)started;
            Pid childPid = (Pid)ids.Items[0];
            ReferenceTerm monitor = (ReferenceTerm)ids.Items[1];
            lock (Field(runtime, ProcessPayloadCore641Names.Gate)!)
            {
                childContext = ((Dictionary<Pid, ProcessContext>)Field(runtime, Processes)!)[childPid];
                Check(Field(childContext, Input) is not null);
                child = runtime.GetProcessHandle(childPid);
                Check(!child.Completion.IsCompleted);
            }
            // No finalizer/GC trick: end the only original wrapper's REAL host
            // lease while preserving that wrapper itself as a stale CLR object.
            sourceHost.Dispose();
            sourceHost = null;
            Check(!((Task)Property(array, Retired)).IsCompleted);
            runtime.Send(childPid, Term.A(Go));
            Term outcome = await parent.InvokeAsync(async context =>
            {
                Term? message = await context.ReceiveAsync<Term>(value => value is TupleTerm
                {
                    Items: [Atom { Name: Done }, Pid candidate, _]
                } && candidate.Equals(childPid) ? value : null, Deadline);
                Check(message is TupleTerm { Items.Count: 3 });

                return ((TupleTerm)message!).Items[2];
            }).WaitAsync(Deadline);
            if (!outcome.Equals(Term.Tuple(Term.A(Value), Term.I(1))))
                throw new ErlangException(outcome);
            Check(!child.Completion.IsCompleted && !((Task)Property(array, Retired)).IsCompleted);
            runtime.Send(childPid, Term.A(Finish));
            await parent.InvokeAsync(async context =>
            {
                Term? down = await context.ReceiveAsync<Term>(
                    value => value is TupleTerm
                    {
                        Items: [Atom { Name: Down }, ReferenceTerm candidate, _, Pid id, _]
                    } && candidate.Equals(monitor) && id.Equals(childPid) ? value : null,
                    Deadline
                );
                Check(down is TupleTerm { Items: [_, _, _, _, Atom { Name: Normal }] });

                return true;
            }).WaitAsync(Deadline);
            await child.Completion.WaitAsync(Deadline);
            await ((TaskCompletionSource)Property(childContext, Unwound)).Task.WaitAsync(Deadline);
            await ((Task)Property(array, Retired)).WaitAsync(Deadline);
            await CountersOwnerAccounting683Checks.AwaitEmpty(runtime, Deadline);
            GC.KeepAlive(reference);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            try
            {
                sourceHost?.Dispose();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            if (child is not null)
                try
                {
                    runtime.Send(child.Pid, Term.A(Go));
                    runtime.Send(child.Pid, Term.A(Finish));
                    await child.Completion.WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            if (parent is not null)
                try
                {
                    await parent.DisposeAsync().AsTask().WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            try
            {
                await runtime.DisposeAsync().AsTask().WaitAsync(Deadline);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            if (array is not null)
                try
                {
                    await ((Task)Property(array, Retired)).WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            RuntimeCountersReport421 report = runtime.CountersStorage421;
            if (!report.Closed || report.LiveArrays != 0 || report.OwnedNativeBytes != 0 || report.ActiveInvocations != 0 || report.Created != report.Freed)
                failures.Add(new InvalidOperationException(Failure));
            GC.KeepAlive(reference);
        }
        if (failures.Count == 1) ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1) throw new AggregateException(Failure, failures);
    }

    private static void Check(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }
}
