using System.Collections.Concurrent;
using System.Reflection;
using System.Security.Cryptography;

namespace Erlang.NifNativeBackend;

internal static class NifNativeLifecycleChecks
{
    internal static async Task Run(NifNativeCapabilityProfile profile, nint counter)
    {
        var first = new ProcessRuntime();
        var second = new ProcessRuntime();
        var failures = new List<Exception>();
        try
        {
            SourceCodeEntry sourceA = Register(first.Modules);
            SourceCodeEntry sourceB = Register(second.Modules);
            int before = Counter(counter);
            await NifNativeHarness.Context(first, async contextA =>
            {
                using var flow = contextA.EnterSourceCode(sourceA.Image, sourceA.Owner);
                await using var reservation = await first.Modules.ReserveNifImageAsync(contextA);
                NifNativeLibraryOwner? owner = null;
                await reservation.InitializeAndCommitAsync(async cancellation =>
                {
                    NifImageInitialized initialized = await NifNativeImageInitializer.InitializeAsync(reservation, profile, Term.I(NifNativeFenceNames.LoadInfo), cancellation);
                    owner = (NifNativeLibraryOwner)initialized.Owner;

                    return initialized;
                });
                await NifNativeHarness.Context(second, contextB => Task.Run(() =>
                    Exercise(profile, sourceA.Image, owner!, second, sourceB, contextB, counter, before)));
            });
            NifNativeHarness.Require(Counter(counter) == before + 1);
            await NifNativeHarness.Context(second, async context =>
            {
                using var flow = context.EnterSourceCode(sourceB.Image, sourceB.Owner);
                await using var reservation = await second.Modules.ReserveNifImageAsync(context);
                await reservation.InitializeAndCommitAsync(cancellation => NifNativeImageInitializer.InitializeAsync(
                    reservation, profile, Term.I(NifNativeFenceNames.LoadInfo), cancellation));
                await sourceB.Image.NifBindings.RetireAsync().WaitAsync(NifNativeFenceNames.Deadline);
            });
            NifNativeHarness.Require(Counter(counter) == before + NifNativeFenceNames.BothOwners);
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        finally
        {
            await CleanupRuntime(second, failures);
            await CleanupRuntime(first, failures);
        }
        if (failures.Count == 1)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1)
            throw new AggregateException(failures);
    }

    private static async Task CleanupRuntime(ProcessRuntime runtime, List<Exception> failures)
    {
        try
        {
            await runtime.DisposeAsync().AsTask().WaitAsync(NifNativeFenceNames.Deadline);
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
    }

    private static SourceCodeEntry Register(ModuleRegistry registry)
    {
        var owner = new object();
        var functions = new[] { (NifNativeNames.Increment, NifNativeNames.IncrementArity), (NifNativeNames.Greeting, NifNativeNames.GreetingArity) };
        foreach (var function in functions)
            registry.RegisterSource(NifNativeNames.Module, function.Item1, function.Item2,
                (_, _) => ValueTask.FromResult<Term>(Nil.Value), owner);
        registry.PublishModuleCode(NifNativeNames.Module, new ModuleCodeDescriptor(
            functions, functions, new BitString(new byte[MD5.HashSizeInBytes]), Nil.Value, sourceOwner: owner)
        {
            DeclaredNifs = functions
        });

        return new(owner, registry.CurrentCodeImage(NifNativeNames.Module)!);
    }

    private static unsafe int Counter(nint pointer) => ((delegate* unmanaged[Cdecl]<int>)pointer)();

    private static void ProveRetirementOwnsFence(Thread retirement, ConcurrentQueue<Exception> failures)
    {
        var semaphore = (SemaphoreSlim)typeof(NifNativeLifecycleGate)
            .GetField(NifNativeFenceNames.LifecycleSemaphore, NifNativeFenceNames.StaticMembers)!.GetValue(null)!;
        NifNativeHarness.Require(SpinWait.SpinUntil(() => semaphore.CurrentCount == 0
            || !retirement.IsAlive || !failures.IsEmpty, NifNativeFenceNames.Deadline));
        // A's owner monitor is held; B has not been allowed to enter its initializer.
        // No probe owns a permit here, so the owned retirement is the only possible holder.
        NifNativeHarness.Require(semaphore.CurrentCount == 0 && retirement.IsAlive && failures.IsEmpty);
        using var deadline = new CancellationTokenSource(NifNativeFenceNames.Deadline);
        using var probe = CancellationTokenSource.CreateLinkedTokenSource(deadline.Token);
        probe.CancelAfter(NifNativeFenceNames.ProbeMilliseconds);
        try
        {
            using var scope = NifNativeLifecycleGate.Enter(probe.Token);
        }
        catch (OperationCanceledException) when (!deadline.IsCancellationRequested)
        {
            NifNativeHarness.Require(semaphore.CurrentCount == 0 && retirement.IsAlive && failures.IsEmpty);

            return;
        }
        throw new InvalidOperationException(NifNativeHarnessNames.Broken);
    }

    private static void Exercise(
        NifNativeCapabilityProfile profile,
        ModuleCodeImage imageA,
        NifNativeLibraryOwner ownerA,
        ProcessRuntime runtimeB,
        SourceCodeEntry sourceB,
        ProcessContext contextB,
        nint counter,
        int before
    )
    {
        object ownerGate = typeof(NifNativeLibraryOwner).GetField(NifNativeFenceNames.OwnerGate, NifNativeFenceNames.Members)!.GetValue(ownerA)!;
        var failures = new ConcurrentQueue<Exception>();
        var reserved = new ManualResetEventSlim();
        var start = new ManualResetEventSlim();
        var entered = new ManualResetEventSlim();
        using var cancel = new CancellationTokenSource();
        bool cancelled = false;
        var initialize = new Thread(() =>
        {
            NifImageLoadReservation? reservation = null;
            Exception? primary = null;
            try
            {
                using var flow = contextB.EnterSourceCode(sourceB.Image, sourceB.Owner);
                reservation = runtimeB.Modules.ReserveNifImageAsync(contextB).AsTask().GetAwaiter().GetResult();
                reserved.Set();
                NifNativeHarness.Require(start.Wait(NifNativeFenceNames.Deadline));
                reservation.InitializeAndCommitAsync(async cancellation =>
                {
                    using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellation, cancel.Token);
                    entered.Set();

                    return await NifNativeImageInitializer.InitializeAsync(reservation, profile, Term.I(NifNativeFenceNames.LoadInfo), linked.Token);
                }).GetAwaiter().GetResult();
                failures.Enqueue(new InvalidOperationException(NifNativeHarnessNames.Broken));
            }
            catch (OperationCanceledException failure) when (cancel.IsCancellationRequested)
            {
                primary = failure;
                cancelled = true;
            }
            catch (Exception failure)
            {
                primary = failure;
                failures.Enqueue(failure);
            }
            finally
            {
                if (reservation is not null)
                {
                    try
                    {
                        reservation.DisposeAsync().AsTask().GetAwaiter().GetResult();
                    }
                    catch (Exception failure) when (ReferenceEquals(failure, primary))
                    {
                    }
                    catch (Exception cleanupFailure)
                    {
                        failures.Enqueue(cleanupFailure);
                    }
                }
            }
        }) { IsBackground = true };
        var retirement = new Thread(() =>
        {
            try
            {
                imageA.NifBindings.RetireAsync().GetAwaiter().GetResult();
            }
            catch (Exception failure)
            {
                failures.Enqueue(failure);
            }
        }) { IsBackground = true };
        bool ownerHeld = false;
        bool initializerStarted = false;
        bool retirementStarted = false;
        try
        {
            Monitor.Enter(ownerGate, ref ownerHeld);
            initialize.Start();
            initializerStarted = true;
            NifNativeHarness.Require(reserved.Wait(NifNativeFenceNames.Deadline));
            retirement.Start();
            retirementStarted = true;
            ProveRetirementOwnsFence(retirement, failures);
            start.Set();
            NifNativeHarness.Require(entered.Wait(NifNativeFenceNames.Deadline));
            NifNativeHarness.Require(SpinWait.SpinUntil(() => !initialize.IsAlive
                || (initialize.ThreadState & System.Threading.ThreadState.WaitSleepJoin) != 0, NifNativeFenceNames.Deadline));
            NifNativeHarness.Require(initialize.IsAlive && failures.IsEmpty && runtimeB.Modules.GetNifTaints().Length == 0);
            NifNativeHarness.Require(Counter(counter) == before);
            cancel.Cancel();
        }
        catch (Exception failure)
        {
            failures.Enqueue(failure);
        }
        finally
        {
            try
            {
                cancel.Cancel();
            }
            catch (Exception cleanupFailure)
            {
                failures.Enqueue(cleanupFailure);
            }
            start.Set();
            if (ownerHeld)
                Monitor.Exit(ownerGate);
            bool retirementJoined = !retirementStarted || retirement.Join(NifNativeFenceNames.Deadline);
            bool initializerJoined = !initializerStarted || initialize.Join(NifNativeFenceNames.Deadline);
            if (!retirementJoined || !initializerJoined)
                failures.Enqueue(new TimeoutException(NifNativeFenceNames.JoinFailure));
            else
            {
                reserved.Dispose();
                start.Dispose();
                entered.Dispose();
            }
        }
        if (!failures.IsEmpty)
            throw new AggregateException(failures);
        NifNativeHarness.Require(cancelled && runtimeB.Modules.GetNifTaints().Length == 0
            && sourceB.Image.NifBindings.Phase == NifImageBindingPhase.Empty);
    }
}
