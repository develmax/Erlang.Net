using System.Runtime.ExceptionServices;
using Erlang.Differential;
using static Erlang.Tests.MixedNif686Names;
using static Erlang.Tests.ProcessPayloadCore641Access;

namespace Erlang.Tests;

internal static class MixedNif686Checks
{
    internal static async Task Run()
    {
        using var compiled = GeneratedModuleCompiler.Compile(Source);
        var runtime = new ProcessRuntime();
        var failures = new List<Exception>();
        RuntimeNodeFoundationHostActor? parent = null;
        ResourceReferenceTerm? reference = null;
        IDisposable? sourceHost = null;
        object? array = null;
        MixedNif686Protocol? protocol = null;
        ResourceReferenceTerm? returned = null;
        IDisposable? returnedHost = null;
        ProcessHandle? child = null;
        ProcessContext? childContext = null;
        try
        {
            Check(!(bool)Property(runtime, Profile));
            using PreparedBeamImage facet = CountersOriginal421Preparation.Prepare(runtime, runtime.Modules);
            await runtime.InitializeModulesAsync(registry =>
            {
                Check(registry.PublishPreparedBeam(facet).Status == BeamPublicationStatus.Published);
            });
            compiled.Definition.Register(runtime.Modules);
            protocol = new MixedNif686Protocol(runtime, runtime.Modules.CurrentCodeImage(Module)!);
            runtime.Modules.Register(
                ProbeModule,
                Store,
                1,
                protocol.Store
            );
            runtime.Modules.Register(
                ProbeModule,
                Pop,
                0,
                protocol.Pop
            );
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
                childContext = ((Dictionary<Pid, ProcessContext>)Field(runtime, CancellationFailure669Names.Processes)!)[childPid];
                Check(Field(childContext, CancellationFailure669Names.Input) is not null);
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
            if (!outcome.Equals(Term.A(Ok)))
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
            await ((TaskCompletionSource)Property(childContext, CancellationFailure669Names.Unwound)).Task.WaitAsync(Deadline);
            // The parent entered BEAM too; retire its heap before observing the
            // independent environment as the sole remaining carrier.
            await parent.DisposeAsync().AsTask().WaitAsync(Deadline);
            parent = null;
            // Source process/env/root has physically retired; independent env
            // still owns the actual backing. Receiver context remains ordinary.
            Check(protocol.SourceContextOwned && !protocol.IndependentOwned);
            Check(!((Task)Property(array, Retired)).IsCompleted);
            // First genuine BEAM entry permanently admits the parent collector.
            // Use a fresh Host actor for the independent ordinary destination.
            await using var destination = new RuntimeNodeFoundationHostActor(runtime);
            returned = (ResourceReferenceTerm)await destination.InvokeAsync(async context =>
                await runtime.Modules.Call(context, ProbeModule, Pop));
            Check(!protocol.DestinationOwned);
            Term value = await destination.InvokeAsync(async context =>
                await runtime.Modules.Call(
                    context,
                    NativeModule,
                    Get,
                    returned,
                    Term.I(1)
                ));
            Check(value.Equals(Term.I(0)));
            Check(Property(returned, OwnedStorage) is null);
            returnedHost = Property(returned, Host) as IDisposable ?? throw new InvalidOperationException(Failure);
            protocol.Dispose();
            returnedHost.Dispose();
            returnedHost = null;
            await ((Task)Property(array, Retired)).WaitAsync(Deadline);
            await CountersOwnerAccounting683Checks.AwaitEmpty(runtime, Deadline);
            GC.KeepAlive(returned);
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
                returnedHost?.Dispose();
                if (returned is not null && Property(returned, Host) is IDisposable carrier) carrier.Dispose();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            try
            {
                protocol?.Dispose();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
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
