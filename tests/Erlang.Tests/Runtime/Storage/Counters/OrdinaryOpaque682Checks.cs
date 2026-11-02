using System.Runtime.ExceptionServices;
using Erlang.Differential;
using static Erlang.Tests.OrdinaryOpaque682Names;
using static Erlang.Tests.ProcessPayloadCore641Access;

namespace Erlang.Tests;

internal static class OrdinaryOpaque682Checks
{
    internal static Task RegisteredHost() => RunCore(false);

    internal static Task PublicOpaqueFunction() => RunCore(true);

    internal static Task SourceFunctionCapture() => RunCore(false, true);

    private static async Task RunCore(bool functionMode, bool functionCapture = false)
    {
        using var compiled = GeneratedModuleCompiler.Compile(Source);
        var runtime = new ProcessRuntime();
        var failures = new List<Exception>();
        RuntimeNodeFoundationHostActor? parent = null;
        ResourceReferenceTerm? reference = null;
        IDisposable? sourceHost = null;
        var escapedCarriers = new HashSet<IDisposable>(ReferenceEqualityComparer.Instance);
        FunctionTerm? escapedFunction = null;
        ResourceReferenceTerm? escapedResult = null;
        var capture = new OrdinaryOpaque682Capture();
        object? array = null;
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
            runtime.Modules.Register(
                HostModule,
                Store,
                1,
                (context, arguments) => capture.Store(context, arguments)
            );
            Term mode = functionCapture ? Term.A(CaptureMode)
                : functionMode ? new FunctionTerm(1, capture.Store)
                : Term.A(RegisteredMode);
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
                    reference,
                    mode
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
            if (!outcome.Equals(Term.A(Ok)))
                throw new ErlangException(outcome);
            Check(!child.Completion.IsCompleted && !((Task)Property(array, Retired)).IsCompleted);
            if (functionCapture)
            {
                // The Host callback has returned and retained the genuine Source
                // function before the child exits. Its visible environment must
                // already be exported, rather than a temporary native header.
                escapedFunction = capture.Received as FunctionTerm;
                Check(escapedFunction?.LocalIdentity is { } identity && identity.Module.Name == Module);
                ResourceReferenceTerm[] roots = escapedFunction!.Environment.OfType<ResourceReferenceTerm>().ToArray();
                Check(roots.Length == 1);
                AddCarrier(roots[0], escapedCarriers);
            }
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
            // Child has physically ended; only the actual EXPORTED host carrier
            // may now keep backing alive, despite all temporary frames being gone.
            Check(capture.Calls == 1);
            ResourceReferenceTerm escaped;
            if (functionCapture)
            {
                Term result = await parent.InvokeAsync(async context =>
                    await escapedFunction!.Invoke(context, []));
                Check(result is ResourceReferenceTerm);
                escaped = escapedResult = (ResourceReferenceTerm)result;
                Check(escapedFunction!.Environment.OfType<ResourceReferenceTerm>().Single().Equals(escaped));
            }
            else
            {
                Check(capture.Reference is not null);
                escaped = capture.Reference!;
            }
            AddCarrier(escaped, escapedCarriers);
            Check(!((Task)Property(array, Retired)).IsCompleted);
            Term initial = await parent.InvokeAsync(async context =>
                await runtime.Modules.Call(
                    context,
                    NativeModule,
                    Get,
                    escaped,
                    Term.I(1)
                ));
            Check(initial.Equals(Term.I(0)));
            await parent.InvokeAsync(async context =>
                await runtime.Modules.Call(
                    context,
                    NativeModule,
                    Add,
                    escaped,
                    Term.I(1),
                    Term.I(1)
                ));
            Term changed = await parent.InvokeAsync(async context =>
                await runtime.Modules.Call(
                    context,
                    NativeModule,
                    Get,
                    escaped,
                    Term.I(1)
                ));
            Check(changed.Equals(Term.I(1)));
            foreach (IDisposable carrier in escapedCarriers) carrier.Dispose();
            escapedCarriers.Clear();
            await ((Task)Property(array, Retired)).WaitAsync(Deadline);
            await CountersOwnerAccounting683Checks.AwaitEmpty(runtime, Deadline);
            GC.KeepAlive(reference);
            GC.KeepAlive(escaped);
            GC.KeepAlive(capture);
            GC.KeepAlive(escapedFunction);
            GC.KeepAlive(escapedResult);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            try
            {
                if (capture.Received is ResourceReferenceTerm held && Property(held, Host) is IDisposable direct) escapedCarriers.Add(direct);
                if (capture.Received is FunctionTerm function)
                    foreach (ResourceReferenceTerm resource in function.Environment.OfType<ResourceReferenceTerm>())
                        if (Property(resource, Host) is IDisposable host) escapedCarriers.Add(host);
                if (escapedResult is not null && Property(escapedResult, Host) is IDisposable returned) escapedCarriers.Add(returned);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            foreach (IDisposable carrier in escapedCarriers)
                try
                {
                    carrier.Dispose();
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
            GC.KeepAlive(capture);
            GC.KeepAlive(escapedFunction);
            GC.KeepAlive(escapedResult);
        }
        if (failures.Count == 1) ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1) throw new AggregateException(Failure, failures);
    }

    private static void AddCarrier(ResourceReferenceTerm reference, HashSet<IDisposable> carriers)
    {
        Check(Property(reference, OwnedStorage) is null);
        var host = Property(reference, Host) as IDisposable ?? throw new InvalidOperationException(Failure);
        carriers.Add(host);
    }

    private static void Check(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }
}
