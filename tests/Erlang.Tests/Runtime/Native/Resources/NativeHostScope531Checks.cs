using System.Runtime.InteropServices;
using static Erlang.Tests.NativeHostScope531Names;
using static Erlang.Tests.NativeHostScope531Access;

namespace Erlang.Tests;

internal static class NativeHostScope531Checks
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Destructor(nint environment, nint payload);

    // Finite real NativeMemory/lease/export control, not a DSO or OTP golden.
    // Each worker owns its environment; only the ambient PUBLIC host scope is shared.
    internal static async Task ParallelExports()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create(false);
        var provider = NativeResource524Access.Provider(runtime);
        var payloads = new object[Workers];
        var binaries = new BitString[Workers];
        var threads = new Thread[Workers];
        var failures = new Exception?[Workers];
        var exports = new int[Workers];
        var destroyed = new int[Workers];
        var nativeReleased = new bool[Workers];
        int baseline = Environments;
        Destructor destructor = (_, payload) => Interlocked.Increment(ref destroyed[Marshal.ReadByte(payload)]);
        nint callback = Marshal.GetFunctionPointerForDelegate(destructor);
        using var barrier = new Barrier(Workers);
        using var cancellation = new CancellationTokenSource();
        var scope = new NativeTermHostScope524(runtime);
        Exception? failure = null;
        Task? retiring = null;
        try
        {
            for (int index = 0; index < Workers; index++)
            {
                payloads[index] = NativeResource524Access.Allocate(runtime, provider.Provider, callback);
                Marshal.WriteByte((nint)NativeResource524Access.Property(payloads[index], Pointer), (byte)index);
                binaries[index] = NativeResource524Access.Binary(runtime, payloads[index]);
            }
            for (int index = 0; index < Workers; index++)
            {
                int worker = index;
                ExecutionContext captured = ExecutionContext.Capture()!;
                threads[index] = new Thread(() => ExecutionContext.Run(
                    captured,
                    _ =>
                {
                    try
                    {
                        using IDisposable environment = NativeHostScope531Access.Environment(runtime, provider.Provider);
                        nuint handle = (nuint)NativeResource524Access.Call(environment, Add, binaries[worker])!;
                        for (int iteration = 0; iteration < ExportsPerWorker; iteration++)
                        {
                            cancellation.Token.ThrowIfCancellationRequested();
                            if (iteration % Batch == 0) NativeHostScope531Access.Check(barrier.SignalAndWait(Deadline, cancellation.Token));
                            Term exported = (Term)NativeResource524Access.Call(environment, Export, handle)!;
                            NativeHostScope531Access.Check(exported is BitString bytes && bytes.ToArray()[0] == worker);
                            exports[worker]++;
                        }
                    }
                    catch (Exception error)
                    {
                        failures[worker] = error;
                        cancellation.Cancel();
                    }
                },
                    null
                ))
                { IsBackground = true };
                threads[index].Start();
            }
            foreach (Thread thread in threads) NativeHostScope531Access.Check(thread.Join(Deadline));
            if (failures.Any(error => error is not null)) throw new AggregateException(failures.OfType<Exception>());
            NativeHostScope531Access.Check(Environments == baseline && exports.All(count => count == ExportsPerWorker));
            // The actual lease list AND actual magic-binary refcount must account
            // for every completed export after all environment roots are released.
            NativeHostScope531Access.Check(ScopeRoots(scope) == Workers * ExportsPerWorker);
            for (int index = 0; index < Workers; index++)
            {
                NativeHostScope531Access.Check(PayloadRoots(payloads[index]) == ExportsPerWorker);
                NativeHostScope531Access.Check(PhysicalReferences(payloads[index]) == ExportsPerWorker + 1);
                NativeResource524Access.Call(payloads[index], "ReleaseNative524");
                nativeReleased[index] = true;
                NativeHostScope531Access.Check(PhysicalReferences(payloads[index]) == ExportsPerWorker);
            }
            retiring = (Task)NativeResource524Access.Call(provider.Bindings, "RetireAsync")!;
            NativeHostScope531Access.Check(!retiring.IsCompleted && !provider.Owner.Disposed && destroyed.All(count => count == 0));
            scope.Dispose();
            foreach (object payload in payloads)
                await ((Task)NativeResource524Access.Property(payload, "Retired524")).WaitAsync(Deadline);
            await retiring.WaitAsync(Deadline);
            NativeHostScope531Access.Check(destroyed.All(count => count == 1) && provider.Owner.Disposed && Environments == baseline);
        }
        catch (Exception error)
        {
            failure = error;
        }
        finally
        {
            // A red old implementation can lose an admitted lease from its list.
            // Drain ONLY this test's isolated leaked real roots after preserving
            // failure, so a detected leak cannot hang runtime disposal or pass.
            cancellation.Cancel();
            foreach (Thread? thread in threads)
                if (thread is { IsAlive: true } && !thread.Join(Deadline))
                    failure = Combine(failure, new TimeoutException(Failure));
            try
            {
                scope.Dispose();
            }
            catch (Exception error)
            {
                failure = Combine(failure, error);
            }
            for (int index = 0; index < Workers; index++)
            {
                if (payloads[index] is not { } payload) continue;
                try
                {
                    if (!nativeReleased[index])
                    {
                        NativeResource524Access.Call(payload, "ReleaseNative524");
                        nativeReleased[index] = true;
                    }
                    long leaked = PayloadRoots(payload);
                    if (leaked != 0) failure = Combine(failure, new InvalidOperationException(Failure));
                    for (long root = 0; root < leaked; root++) NativeResource524Access.Call(payload, "ReleaseRoot524");
                    await ((Task)NativeResource524Access.Property(payload, "Retired524")).WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    failure = Combine(failure, error);
                }
            }
            try
            {
                await ((Task)NativeResource524Access.Call(provider.Bindings, "RetireAsync")!).WaitAsync(Deadline);
            }
            catch (Exception error)
            {
                failure = Combine(failure, error);
            }
            GC.KeepAlive(destructor);
        }
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private static Exception Combine(Exception? first, Exception second)
        => first is null ? second : new AggregateException(first, second);
}
