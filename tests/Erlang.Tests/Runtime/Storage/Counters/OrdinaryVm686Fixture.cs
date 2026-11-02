using System.Collections;
using Erlang.Differential;
using static Erlang.Tests.OrdinaryVm686Names;
using static Erlang.Tests.ProcessPayloadCore641Access;

namespace Erlang.Tests;

internal sealed class OrdinaryVm686Fixture : IAsyncDisposable
{
    private readonly IDisposable compiled;
    private IDisposable? producer;
    private int entered;
    internal ProcessRuntime Runtime { get; } = new();
    internal RuntimeNodeFoundationHostActor Parent { get; private set; } = null!;
    internal ResourceReferenceTerm? Reference { get; private set; }
    internal object? ArrayObject { get; private set; }
    internal ProcessHandle? Child { get; private set; }
    internal ProcessContext? Context { get; private set; }
    internal TupleTerm? Started { get; private set; }
    private bool offlineChild;
    internal bool Stopped { get; private set; }
    internal HashSet<IDisposable> ObservedCarriers { get; } = new(ReferenceEqualityComparer.Instance);
    internal Term? ObservedDown { get; set; }
    internal int EntryCalls => Volatile.Read(ref entered);

    private OrdinaryVm686Fixture(IDisposable compiled) => this.compiled = compiled;

    internal static async Task<OrdinaryVm686Fixture> Create(bool native)
    {
        var source = GeneratedModuleCompiler.Compile(Source, SourceFile);
        var result = new OrdinaryVm686Fixture(source);
        try
        {
            Require(!(bool)Property(result.Runtime, Profile));
            using PreparedBeamImage facet = CountersOriginal421Preparation.Prepare(result.Runtime, result.Runtime.Modules);
            await result.Runtime.InitializeModulesAsync(registry =>
            {
                Require(registry.PublishPreparedBeam(facet).Status == BeamPublicationStatus.Published);
            });
            source.Definition.Register(result.Runtime.Modules);
            result.Runtime.Modules.Register(
                Probe,
                Entered,
                1,
                (_, _) =>
            {
                Interlocked.Increment(ref result.entered);

                return ValueTask.FromResult<Term>(Term.A(Ok));
            }
            );
            result.Parent = new(result.Runtime);
            await result.Parent.Ready.WaitAsync(Deadline);
            if (native)
            {
                result.Reference = (ResourceReferenceTerm)await result.Parent.InvokeAsync(async context =>
                    await result.Runtime.Modules.Call(
                        context,
                        NativeModule,
                        OrdinaryVm686Names.Create,
                        Term.I(1)
                    ));
                result.producer = Property(result.Reference, Host) as IDisposable
                    ?? throw new InvalidOperationException(Failure);
                result.ArrayObject = Property(Property(result.Reference, Backing), ArrayField);
            }

            return result;
        }
        catch
        {
            await result.DisposeAsync();
            throw;
        }
    }

    internal async Task StartChild(string mode, bool offline = false)
    {
        offlineChild = offline;
        if (offline)
        {
            // Real public scheduler state, not a fake ctx/input/control flag.
            Require(Runtime.ActorDispatchCapacity >= 2);
            _ = await Parent.InvokeAsync(async context => await Runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                SystemFlag,
                Term.A(SchedulersOnline),
                Term.I(1)
            ));
        }
        Term argument = Reference is { } reference ? reference : Nil.Value;
        Started = (TupleTerm)await Parent.InvokeAsync(async context => await Runtime.Modules.Call(
            context,
            Module,
            offline ? StartOffline : Start,
            context.Self,
            argument,
            Term.A(mode)
        ));
        Require(Started.Items is [Pid, ReferenceTerm]);
        var pid = (Pid)Started.Items[0];
        lock (Field(Runtime, ProcessPayloadCore641Names.Gate)!)
        {
            Context = ((Dictionary<Pid, ProcessContext>)Field(Runtime, Processes)!)[pid];
            Child = Runtime.GetProcessHandle(pid);
        }
        if (!offline)
        {
            Term? ready = await Parent.InvokeAsync(async context => await context.ReceiveAsync<Term>(
                value => value is TupleTerm { Items: [Atom { Name: Ready }, Pid id] } && id.Equals(pid) ? value : null, Deadline));
            Require(ready is not null && EntryCalls == 1);
        }
    }

    internal void ReleaseProducer()
    {
        producer?.Dispose();
        producer = null;
    }

    internal bool EnabledNow() => (bool)Property(Context!, Enabled);

    internal object? HeapNow() => Field(Context!, HeapField);

    internal bool RegistryContainsChild()
    {
        lock (Field(Runtime, OwnerGate)!)
            return ((IDictionary)Field(Runtime, Owners)!).Contains(Context!);
    }

    internal void GoNow() => Runtime.Send(Child!.Pid, Term.A(Go));

    internal void FinishNow() => Runtime.Send(Child!.Pid, Term.A(Finish));

    internal async Task<Term> ReadDone()
    {
        Term? result = await Parent.InvokeAsync(async context => await context.ReceiveAsync<Term>(
            value => value is TupleTerm { Items: [Atom { Name: Done }, Pid id, _] }
                && id.Equals(Child!.Pid) ? value : null, Deadline));
        Require(result is TupleTerm { Items.Count: 3 });

        return ((TupleTerm)result!).Items[2];
    }

    internal async Task<Term> ReadDown()
    {
        var monitor = (ReferenceTerm)Started!.Items[1];
        Term? result = await Parent.InvokeAsync(async context => await context.ReceiveAsync<Term>(
            value => value is TupleTerm { Items: [Atom { Name: Down }, ReferenceTerm candidate, _, Pid id, _] }
                && candidate.Equals(monitor) && id.Equals(Child!.Pid) ? value : null,
            Deadline
        ));
        Require(result is TupleTerm { Items.Count: 5 });
        ObservedDown = result;

        return ((TupleTerm)result!).Items[4];
    }

    internal async Task AwaitPhysicalChild()
    {
        await Child!.Completion.WaitAsync(Deadline);
        await ((TaskCompletionSource)Property(Context!, Unwound)).Task.WaitAsync(Deadline);
        await ((Task)Property(Context!, Retirement)).WaitAsync(Deadline);
    }

    internal async Task StopVm()
    {
        await Runtime.DisposeAsync().AsTask().WaitAsync(Deadline);
        Stopped = true;
    }

    internal async Task AwaitNativeRetired()
    {
        if (!Stopped && !NativeRetired() && !Parent.Process.Completion.IsCompleted)
            await CounterParentCollection365.Collect(Runtime, Parent, ArrayObject!);
        await ((Task)Property(ArrayObject!, Retired)).WaitAsync(Deadline);
        await CountersOwnerAccounting683Checks.AwaitEmpty(Runtime, Deadline);
    }

    internal bool NativeRetired() => ((Task)Property(ArrayObject!, Retired)).IsCompleted;

    internal void CaptureResourceViews(Term source)
    {
        var pending = new Stack<Term>();
        pending.Push(source);
        int resources = 0;
        while (pending.TryPop(out Term? value))
        {
            switch (value)
            {
                case ResourceReferenceTerm resource:
                    Require(resource.Equals(Reference));
                    Require(Property(resource, OwnedStorage) is null);
                    Require(ReferenceEquals(Property(Property(resource, Backing), ArrayField), ArrayObject));
                    Require(Property(resource, Host) is IDisposable);
                    ObservedCarriers.Add((IDisposable)Property(resource, Host));
                    resources++;
                    break;
                case TupleTerm tuple:
                    foreach (Term item in tuple.Items) pending.Push(item);
                    break;
                case Cons list:
                    pending.Push(list.Head);
                    pending.Push(list.Tail);
                    break;
            }
        }
        Require(resources > 0);
    }

    internal async Task RequireActualResourceOperations(Term source)
    {
        var pending = new Stack<Term>();
        pending.Push(source);
        while (pending.TryPop(out Term? value))
        {
            if (value is ResourceReferenceTerm reference)
            {
                Term actual = await Parent.InvokeAsync(async context =>
                    await Runtime.Modules.Call(
                        context,
                        NativeModule,
                        Get,
                        reference,
                        Term.I(1)
                    ));
                Require(actual.Equals(Term.I(0)));
            }
            else if (value is TupleTerm tuple)
                foreach (Term child in tuple.Items) pending.Push(child);
            else if (value is Cons list)
            {
                pending.Push(list.Head);
                pending.Push(list.Tail);
            }
        }
    }

    internal void DisposeViews()
    {
        foreach (IDisposable value in ObservedCarriers) value.Dispose();
        ObservedCarriers.Clear();
    }

    public async ValueTask DisposeAsync()
    {
        var failures = new List<Exception>();
        try
        {
            ReleaseProducer();
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        try
        {
            DisposeViews();
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        if (Child is not null && !Stopped && !offlineChild)
            try
            {
                GoNow();
                FinishNow();
                await Child.Completion.WaitAsync(Deadline);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        if (Parent is not null)
            try
            {
                await Parent.DisposeAsync().AsTask().WaitAsync(Deadline);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        try
        {
            await Runtime.DisposeAsync().AsTask().WaitAsync(Deadline);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        if (Child is not null)
            try
            {
                await AwaitPhysicalChild();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        if (ArrayObject is not null)
            try
            {
                await AwaitNativeRetired();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        compiled.Dispose();
        RuntimeCountersReport421 report = Runtime.CountersStorage421;
        if (!report.Closed || report.LiveArrays != 0 || report.OwnedNativeBytes != 0 || report.ActiveInvocations != 0 || report.Created != report.Freed)
            failures.Add(new InvalidOperationException(Failure));
        GC.KeepAlive(Reference);
        GC.KeepAlive(ObservedDown);
        if (failures.Count != 0) throw new AggregateException(Failure, failures);
    }
}
