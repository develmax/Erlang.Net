using System.Runtime.InteropServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.NativeResource524Names;
using static Erlang.Tests.NativeResource536Access;

namespace Erlang.Tests;

internal static class NativeResource536Checks
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Destructor(nint environment, nint payload);

    internal static async Task GuestGcUsesActualNormalAuxiliaryOnce()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        var provider = Provider(runtime);
        int calls = 0, kind = -1, lane = 0, thread = 0;
        bool selected = false;
        Exception? observation = null;
        Destructor callback = (_, _) =>
        {
            try
            {
                kind = ThreadType();
                lane = AuxiliaryLane();
                thread = Environment.CurrentManagedThreadId;
                selected = ActualSelectedAuxiliary(runtime, lane);
                Interlocked.Increment(ref calls);
            }
            catch (Exception error)
            {
                observation = error;
            }
        };
        object payload = Allocate(runtime, provider.Provider, Marshal.GetFunctionPointerForDelegate(callback));
        int broker = BrokerThread(runtime);
        BitString binary = Binary(runtime, payload);
        try
        {
            await actor.Invoke(context =>
 {
     ReleaseViaGuestGc(context, payload, binary);

     return true;
 });
            await ((Task)Property(payload, NativeResource536TestNames.Retired)).WaitAsync(Deadline);
            NativeResource524Access.Check(observation is null && calls == 1 && kind == NativeResource536TestNames.NormalThread
                && lane > 0 && selected && thread != broker);
            NativeResource524Access.Check(ThreadType() == NativeResource536TestNames.ForeignThread && AuxiliaryLane() == 0);
        }
        finally
        {
            await ((Task)Call(provider.Bindings, NativeResource536TestNames.RetireImage)!).WaitAsync(Deadline);
            GC.KeepAlive(callback);
        }
    }

    internal static async Task NonGcRouteIsNotSilentlyReclassified()
    {
        await HeldActualAuxiliaryAcknowledgment(false);
        await HeldActualAuxiliaryAcknowledgment(true);
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        var provider = Provider(runtime);
        int kind = -1, lane = -1, calls = 0;
        Destructor callback = (_, _) =>
        {
            kind = ThreadType();
            lane = AuxiliaryLane();
            Interlocked.Increment(ref calls);
        };
        object payload = Allocate(runtime, provider.Provider, Marshal.GetFunctionPointerForDelegate(callback));
        var root = (IDisposable)Call(payload, NativeResource536TestNames.RetainRoot)!;
        try
        {
            Call(payload, NativeResource536TestNames.ReleaseNative);
            root.Dispose();
            await ((Task)Property(payload, NativeResource536TestNames.Retired)).WaitAsync(Deadline);
            NativeResource524Access.Check(calls == 1 && kind == NativeResource536TestNames.ForeignThread && lane == 0);
        }
        finally
        {
            root.Dispose();
            await ((Task)Call(provider.Bindings, NativeResource536TestNames.RetireImage)!).WaitAsync(Deadline);
            GC.KeepAlive(callback);
        }
    }

    internal static async Task HeldNormalDestructorKeepsImageAndClosePhysical()
    {
        var runtime = OwnedGuest506RuntimeFactory.Create();
        var actor = new RuntimeNodeFoundationHostActor(runtime);
        var provider = Provider(runtime);
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        int kind = -1, calls = 0;
        bool released = false;
        Destructor callback = (_, _) =>
        {
            kind = ThreadType();
            entered.Set();
            released = release.Wait(Deadline);
            Interlocked.Increment(ref calls);
        };
        object payload = Allocate(runtime, provider.Provider, Marshal.GetFunctionPointerForDelegate(callback));
        BitString binary = Binary(runtime, payload);
        Task? closing = null, retiring = null;
        try
        {
            await actor.Invoke(context =>
 {
     ReleaseViaGuestGc(context, payload, binary);

     return true;
 });
            NativeResource524Access.Check(entered.Wait(Deadline));
            object domain = Property(runtime, NativeResource536TestNames.ResourceDomain);
            await ((Task)Property(domain, NativeResource536TestNames.AuxReady)).WaitAsync(Deadline);
            object accounting = Property(Property(runtime, NativeResource536TestNames.Statistics), NativeResource536TestNames.Microstates);
            object action = Enum.Parse(
                typeof(ProcessRuntime).Assembly.GetType(NativeResource536TestNames.MicrostateActionType, true)!,
                NativeResource536TestNames.Gather
            );
            object request = Call(
                accounting,
                NativeResource536TestNames.CreateMicrostateRequest,
                action,
                0
            )!;
            object control = Call(domain, NativeResource536TestNames.PrepareAuxiliary, request)!;
            Call(control, NativeResource536TestNames.PublishAuxiliary);
            await ((Task)Property(control, NativeResource536TestNames.AuxCompletion)).WaitAsync(Deadline);
            var row = (MapTerm)Cons.Items((Term)Call(request, NativeResource536TestNames.MicrostateResult)!).Single();
            NativeResource524Access.Check(row.Entries.Any(pair =>
                pair.Key is Atom { Name: NativeResource536TestNames.MicrostateType }
                && pair.Value is Atom { Name: NativeResource536TestNames.AuxiliaryType }));
            NativeResource524Access.Check(!release.IsSet && calls == 0);
            retiring = (Task)Call(provider.Bindings, NativeResource536TestNames.RetireImage)!;
            NativeResource524Access.Check(!retiring.IsCompleted && !provider.Owner.Disposed);
            closing = runtime.DisposeAsync().AsTask();
            NativeResource524Access.Check(!closing.IsCompleted && !provider.Owner.Disposed);
        }
        finally
        {
            release.Set();
            await ((Task)Property(payload, NativeResource536TestNames.Retired)).WaitAsync(Deadline);
            if (retiring is not null) await retiring.WaitAsync(Deadline);
            if (closing is not null) await closing.WaitAsync(Deadline);
            await runtime.DisposeAsync();
            try
            {
                await actor.DisposeAsync();
            }
            catch (OperationCanceledException) { }
            GC.KeepAlive(callback);
        }
        NativeResource524Access.Check(released && calls == 1 && kind == NativeResource536TestNames.NormalThread && provider.Owner.Disposed);
    }

    private static async Task HeldActualAuxiliaryAcknowledgment(bool closeVm)
    {
        // Reserve failure storage before acquiring any fixture ownership.
        var cleanupFailures = new Exception?[NativeResource536TestNames.CleanupFailureCapacity];
        int cleanupCount = 0;
        Exception? primary = null;
        ProcessRuntime? runtime = null;
        RuntimeNodeFoundationHostActor? actor = null, killer = null;
        object? payload = null, bindings = null;
        IDisposable? root = null;
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        int calls = 0, kind = -1, lane = -1;
        bool released = false, nativeReleased = false;
        Destructor callback = (_, _) =>
        {
            kind = ThreadType();
            lane = AuxiliaryLane();
            entered.Set();
            released = release.Wait(Deadline);
            Interlocked.Increment(ref calls);
        };
        Task? closing = null, retiring = null;
        Task<Term>? invocation = null;
        try
        {
            runtime = OwnedGuest506RuntimeFactory.Create();
            actor = new RuntimeNodeFoundationHostActor(runtime);
            killer = new RuntimeNodeFoundationHostActor(runtime);
            var provider = Provider(runtime);
            bindings = provider.Bindings;
            payload = Allocate(runtime, provider.Provider, Marshal.GetFunctionPointerForDelegate(callback));
            root = (IDisposable)Call(payload, NativeResource536TestNames.RetainRoot)!;
            ProcessContext context = await actor.Ready;
            await killer.Ready;
            object domain = Property(runtime, NativeResource536TestNames.ResourceDomain);
            await ((Task)Property(domain, NativeResource536TestNames.AuxReady)).WaitAsync(Deadline);
            // Native release plus its final real resource root selects the
            // existing non-GC route, executing this destructor on actual Aux.
            Call(payload, NativeResource536TestNames.ReleaseNative);
            nativeReleased = true;
            root.Dispose();
            NativeResource524Access.Check(entered.Wait(Deadline));
            invocation = actor.InvokeAsync(async caller => await runtime.Modules.Call(
                caller,
                NativeResource536TestNames.ErlangModule,
                NativeResource536TestNames.SystemFlag,
                Term.A(NativeResource536TestNames.MicrostateFlag),
                Term.A(NativeResource536TestNames.True)
            ));
            object? ticket = null;
            await StatisticsTrapTestAccess.WaitUntil(() =>
            {
                ticket = QueuedActualAuxiliary(domain);

                return ticket is not null;
            });
            Task acknowledged = (Task)Property(ticket!, NativeResource536TestNames.AuxCompletion);
            NativeResource524Access.Check(!acknowledged.IsCompleted && !invocation.IsCompleted
                && !actor.Process.Completion.IsCompleted && calls == 0);
            retiring = (Task)Call(provider.Bindings, NativeResource536TestNames.RetireImage)!;
            NativeResource524Access.Check(!retiring.IsCompleted && !provider.Owner.Disposed);
            if (closeVm)
                closing = runtime.DisposeAsync().AsTask();
            else
                await killer.InvokeAsync(caller =>
                {
                    runtime.Exit(caller, actor.Process.Pid, Term.A(NativeResource536TestNames.Kill));

                    return Task.FromResult<Term>(Nil.Value);
                });
            await StatisticsTrapTestAccess.WaitUntil(() => context.Cancellation.IsCancellationRequested);
            // Cancellation cannot manufacture the ACK, retire the request, or
            // complete VM/process teardown while its actual callback is held.
            NativeResource524Access.Check(!acknowledged.IsCompleted && !invocation.IsCompleted
                && !actor.Process.Completion.IsCompleted && !retiring.IsCompleted
                && (closing is null || !closing.IsCompleted) && calls == 0);
            release.Set();
            await acknowledged.WaitAsync(Deadline);
            await StatisticsTrapTestAccess.Failed(invocation);
            await actor.Process.Completion.WaitAsync(Deadline);
            await ((Task)Property(payload, NativeResource536TestNames.Retired)).WaitAsync(Deadline);
            await retiring.WaitAsync(Deadline);
            closing ??= runtime.DisposeAsync().AsTask();
            await closing.WaitAsync(Deadline);
            object snapshot = Call(domain, NativeResource536TestNames.DomainSnapshot)!;
            object heap = Property(context, NativeResource536TestNames.Heap);
            object heapSnapshot = Property(heap, NativeResource536TestNames.HeapSnapshot);
            NativeResource524Access.Check(acknowledged.IsCompletedSuccessfully
                && released && calls == 1 && kind == NativeResource536TestNames.ForeignThread && lane == 0
                && provider.Owner.Disposed && QueuedActualAuxiliary(domain) is null
                && (long)Property(snapshot, NativeResource536TestNames.LiveNativeBytes) == 0
                && (int)Property(snapshot, NativeResource536TestNames.LiveResources) == 0
                && (int)Property(snapshot, NativeResource536TestNames.Pending) == 0
                && (bool)Property(snapshot, NativeResource536TestNames.WorkerJoined)
                && (bool)Property(snapshot, NativeResource536TestNames.CompletedSuccessfully)
                && (int)Property(heapSnapshot, NativeResource536TestNames.RootFrames) == 0
                && (long)Property(heapSnapshot, NativeResource536TestNames.LivePayloadWords) == 0
                && (int)Property(heapSnapshot, NativeResource536TestNames.Blocks) == 0
                && (int)runtime.Modules.GetType().GetField(NativeResource536TestNames.TrapUses, Instance)!.GetValue(runtime.Modules)! == 0);
        }
        catch (Exception error)
        {
            primary = error;
        }
        finally
        {
            // Every actual owner gets an independent cleanup attempt. Captures
            // cannot allocate and prevent a later owner from being released.
            try
            {
                release.Set();
            }
            catch (Exception error)
            {
                cleanupFailures[cleanupCount++] = error;
            }
            try
            {
                if (payload is not null && !nativeReleased)
                    Call(payload, NativeResource536TestNames.ReleaseNative);
            }
            catch (Exception error)
            {
                cleanupFailures[cleanupCount++] = error;
            }
            try
            {
                root?.Dispose();
            }
            catch (Exception error)
            {
                cleanupFailures[cleanupCount++] = error;
            }
            try
            {
                if (payload is not null)
                    await ((Task)Property(payload, NativeResource536TestNames.Retired)).WaitAsync(Deadline);
            }
            catch (Exception error)
            {
                cleanupFailures[cleanupCount++] = error;
            }
            try
            {
                if (bindings is not null)
                {
                    retiring ??= (Task)Call(bindings, NativeResource536TestNames.RetireImage)!;
                    await retiring.WaitAsync(Deadline);
                }
            }
            catch (Exception error)
            {
                cleanupFailures[cleanupCount++] = error;
            }
            try
            {
                if (runtime is not null) await runtime.DisposeAsync();
            }
            catch (Exception error)
            {
                cleanupFailures[cleanupCount++] = error;
            }
            try
            {
                if (actor is not null) await actor.DisposeAsync();
            }
            catch (OperationCanceledException) { }
            catch (Exception error)
            {
                cleanupFailures[cleanupCount++] = error;
            }
            try
            {
                if (killer is not null) await killer.DisposeAsync();
            }
            catch (OperationCanceledException) { }
            catch (Exception error)
            {
                cleanupFailures[cleanupCount++] = error;
            }
            GC.KeepAlive(callback);
        }
        if (cleanupCount != 0)
        {
            var failures = new List<Exception>();
            if (primary is not null) failures.Add(primary);
            for (int index = 0; index < cleanupCount; index++)
                failures.Add(cleanupFailures[index]!);
            throw new AggregateException(failures);
        }
        if (primary is not null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(primary).Throw();
    }

    private static object? QueuedActualAuxiliary(object domain)
    {
        // Observe the actual inbox without mutating ticket state, synthesizing
        // replies, or invoking the callback outside its owning worker.
        object domainGate = domain.GetType().GetField(NativeResource536TestNames.DomainGate, Instance)!.GetValue(domain)!;
        lock (domainGate)
        {
            var queued = (System.Collections.IEnumerable)domain.GetType().GetField(NativeResource536TestNames.AuxiliaryQueue, Instance)!.GetValue(domain)!;

            return queued.Cast<object>().SingleOrDefault();
        }
    }
}
