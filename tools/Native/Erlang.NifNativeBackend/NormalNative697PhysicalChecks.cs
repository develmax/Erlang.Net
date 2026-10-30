using System.Reflection;
using System.Security.Cryptography;

namespace Erlang.NifNativeBackend;

// Call only from an owned collector AFTER genuine v2_18_opts1 load and before
// its ordinary image retirement. No DLL/BEAM build, policy or Bind is fabricated.
internal static class NormalNative697PhysicalChecks
{
    internal static async Task RunLoaded(
        ProcessRuntime runtime,
        ModuleCodeImage image,
        string approvedLibrarySha256
    )
    {
        NifNativeHarness.Require(ReferenceEquals(runtime.Modules.CurrentCodeImage(NifVersion651DriverNames.Module), image));
        NifNativeHarness.Require(image.Module == NifVersion651DriverNames.Module && image.NifBindings.Phase == NifImageBindingPhase.Ready);
        object owner = Field(image.NifBindings, NormalNative697PhysicalNames.Owner);
        NifNativeHarness.Require(owner is NifNativeLibraryOwner);
        var native = (NifNativeLibraryOwner)owner;
        NifNativeHarness.Require((nint)Field(owner, NormalNative697PhysicalNames.Library) != 0);
        var libraryFile = (FileStream)Field(owner, NormalNative697PhysicalNames.File);
        NifNativeHarness.Require(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(libraryFile.Name))) == approvedLibrarySha256.ToUpperInvariant());
        var ready = (Dictionary<(string Name, int Arity), ErlangFunction>)Field(image.NifBindings, NormalNative697PhysicalNames.Ready);
        NifNativeHarness.Require(ready.TryGetValue((NormalNative697PhysicalNames.Identity, 0), out var actualBody)
            && native.OwnsGuestBinding535(
                image,
                NormalNative697PhysicalNames.Identity,
                0,
                actualBody
            ));

        object ownerGate = Field(owner, NormalNative697PhysicalNames.Gate);
        using var gateEntered = new ManualResetEventSlim();
        using var releaseGate = new ManualResetEventSlim();
        var gateDone = Signal();
        var contextReady = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseContext = Signal();
        var queuedBarrier = Signal();
        var barrierGranted = Signal();
        var releaseBarrier = Signal();
        Thread? holder = null;
        bool holderStarted = false;
        Task? gateWork = null;
        Task? contextLifetime = null, barrier = null;
        Task<Term>? first = null, sameContext = null;
        Exception? primary = null;
        try
        {
            contextLifetime = NifNativeHarness.Context(runtime, async context =>
            {
                contextReady.TrySetResult(context);
                await releaseContext.Task;
            });
            ProcessContext context = await contextReady.Task.WaitAsync(NifNativeHarnessNames.Deadline);
            holder = new Thread(() =>
            {
                try
                {
                    lock (ownerGate)
                    {
                        gateEntered.Set();
                        NifNativeHarness.Require(releaseGate.Wait(NifNativeHarnessNames.Deadline));
                    }
                    gateDone.TrySetResult();
                }
                catch (Exception failure)
                {
                    gateDone.TrySetException(failure);
                }
            })
            { IsBackground = true };
            holder.Start();
            holderStarted = true;
            gateWork = gateDone.Task;
            NifNativeHarness.Require(gateEntered.Wait(NifNativeHarnessNames.Deadline));
            first = Task.Run(() => InvokePinned(context, image));
            await WaitCounts(runtime, 1, 0);
            barrier = NifNativeHarness.Context(
                runtime,
                async requester =>
            {
                ValueTask<RuntimeNormalBarrierRequest696> waiting = runtime.AcquireNormalBarrier696(requester);
                queuedBarrier.TrySetResult();
                using var lease = await waiting;
                // Both real Native function calls traverse the normal factory.
                // The Task copies requester EC but has no genuine actor turn.
                sameContext = Task.Run(() => InvokePinned(requester, image));
                barrierGranted.TrySetResult();
                await releaseBarrier.Task;
            }
            );
            await queuedBarrier.Task.WaitAsync(NifNativeHarnessNames.Deadline);
            NifNativeHarness.Require(!barrierGranted.Task.IsCompleted);
            releaseGate.Set();
            await gateDone.Task.WaitAsync(NifNativeHarnessNames.Deadline);
            NifNativeHarness.Require((await first.WaitAsync(NifNativeHarnessNames.Deadline)).Equals(Term.I(NormalNative697PhysicalNames.ExpectedIdentity)));
            await barrierGranted.Task.WaitAsync(NifNativeHarnessNames.Deadline);
            await WaitCounts(runtime, 0, 1);
            NifNativeHarness.Require(!sameContext!.IsCompleted);
            releaseBarrier.TrySetResult();
            NifNativeHarness.Require((await sameContext.WaitAsync(NifNativeHarnessNames.Deadline)).Equals(Term.I(NormalNative697PhysicalNames.ExpectedIdentity)));
            await barrier.WaitAsync(NifNativeHarnessNames.Deadline);
            await WaitCounts(runtime, 0, 0);
        }
        catch (Exception failure)
        {
            primary = failure;
            throw;
        }
        finally
        {
            releaseGate.Set();
            releaseBarrier.TrySetResult();
            releaseContext.TrySetResult();
            var failures = new List<Exception>();
            if (holderStarted && holder is not null)
                try
                {
                    NifNativeHarness.Require(holder.Join(NifNativeHarnessNames.Deadline));
                }
                catch (Exception failure)
                {
                    failures.Add(failure);
                }
            foreach (Task? task in new[] { gateWork, first, sameContext, barrier, contextLifetime })
                if (task is not null)
                    try
                    {
                        await task.WaitAsync(NifNativeHarnessNames.Deadline);
                    }
                    catch (Exception failure)
                    {
                        failures.Add(failure);
                    }
            if (failures.Count != 0)
            {
                if (primary is not null) failures.Insert(0, primary);
                throw new AggregateException(failures);
            }
        }
        // Caller still owns current image. Its normal real retirement + runtime
        // shutdown must be awaited and reported after this helper returns.
    }

    private static async Task WaitCounts(ProcessRuntime runtime, int active, int queued)
    {
        using var timeout = new CancellationTokenSource(NifNativeHarnessNames.Deadline);
        while (true)
        {
            object dispatcher = Field(runtime, NormalNative697PhysicalNames.Dispatcher);
            lock (Field(dispatcher, NormalNative697PhysicalNames.Gate))
            {
                object queue = Field(dispatcher, NormalNative697PhysicalNames.Queue);
                int count = (int)queue.GetType().GetProperty(NormalNative697PhysicalNames.Count)!.GetValue(queue)!;
                if ((int)Field(dispatcher, NormalNative697PhysicalNames.Active) == active && count == queued) return;
            }
            await Task.Delay(1, timeout.Token);
        }
    }

    private static async Task<Term> InvokePinned(ProcessContext context, ModuleCodeImage image)
    {
        // Public module dispatch reenters the actor scheduler. Exercise the
        // actual installed native body with its real image-call custody here.
        using NifImageCall? call = image.NifBindings.Admit(NormalNative697PhysicalNames.Identity, 0);
        NifNativeHarness.Require(call is { GuestNative535: true });

        return await call!.InvokeAsync(context, Array.Empty<Term>());
    }

    private static object Field(object target, string name)
        => target.GetType().GetField(name, NormalNative697PhysicalNames.Members)!.GetValue(target)!;

    private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
