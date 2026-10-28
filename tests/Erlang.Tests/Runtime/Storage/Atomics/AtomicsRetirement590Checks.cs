using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using Erlang;
using static Erlang.Tests.AtomicsRetirement590Names;

namespace Erlang.Tests;

internal static class AtomicsRetirement590Checks
{
    private static int nativeCalls;
    private static readonly AtomicsRetirement590Destructor rejectingDestructor = RejectDestructor;

    private static int RejectDestructor(nint binary)
    {
        Interlocked.Increment(ref nativeCalls);

        return 0;
    }

    private static nint RejectedDestructorPointer() => Marshal.GetFunctionPointerForDelegate(rejectingDestructor);

    private static void Check(bool value)
    {
        if (!value)
            throw new InvalidOperationException(Failure);
    }

    internal static Task PhysicalFailureForgetsReference() => Run();

    private static object Invoke(object value, string name, params object[] args)
        => value.GetType().GetMethod(name, Members)!.Invoke(value, args)!;

    private static object Read(object value, string name)
        => value.GetType().GetProperty(name, Members)!.GetValue(value)!;

    private static async Task Run()
    {
        var failures = new List<Exception>();
        ProcessRuntime? runtime = null;
        IDisposable? invocation = null;
        object? array = null;
        nint originalDestructor = 0;
        nint pointer = 0;
        int offset = 0;
        try
        {
            runtime = OwnedGuest506RuntimeFactory.Create();
            object owner = Read(runtime, Owner);
            invocation = (IDisposable)Invoke(owner, Enter);
            array = Invoke(
                owner,
                Create,
                1UL,
                true
            );
            Invoke(
                array,
                Bind,
                owner,
                runtime
            );
            Type backingType = typeof(ProcessRuntime).Assembly.GetType(Backing, true)!;
            object backing = Activator.CreateInstance(
                backingType,
                Members,
                null,
                [owner, array],
                null
            )!;
            object registry = Read(runtime, Registry);
            var reference = (ReferenceTerm)Invoke(registry, Publish, backing);
            // The test exclusively owns this unexported constructor allocation:
            // no Guest/Host/Producer was admitted, so no automatic worker races.
            pointer = (nint)array.GetType().GetField(Memory, Members)!.GetValue(array)!;
            Type headerType = typeof(ProcessRuntime).Assembly.GetType(Header, true)!;
            offset = checked((int)Marshal.OffsetOf(headerType, Destructor));
            originalDestructor = Marshal.ReadIntPtr(pointer, offset);
            nativeCalls = 0;
            Marshal.WriteIntPtr(pointer, offset, RejectedDestructorPointer());
            Exception? observed = null;
            try
            {
                Invoke(owner, Retire, array);
            }
            catch (TargetInvocationException error) when (error.InnerException is not null)
            {
                observed = error.InnerException;
            }
            Check(observed is not null && nativeCalls == 1);
            Check(!(bool)Read(array, AllocationLive));
            Check(((Task)Read(array, Retired)).IsFaulted);
            Check(ReferenceEquals(observed, array.GetType().GetField(CloseFailure, Members)!.GetValue(array)));
            IDisposable? acquired = (IDisposable?)Invoke(registry, Acquire, reference);
            try
            {
                Check(acquired is null);
            }
            finally
            {
                acquired?.Dispose();
            }
            object registryGate = registry.GetType().GetField(RegistryGate, Members)!.GetValue(registry)!;
            lock (registryGate)
            {
                var identities = (System.Collections.IDictionary)registry.GetType().GetField(IdentityIndex, Members)!.GetValue(registry)!;
                Check(identities.Count == 0 && !identities.Contains(array));
            }
            var report = runtime.AtomicsStorage413;
            Check(report.Created == 1 && report.Freed == 1 && report.LiveArrays == 0 && report.OwnedNativeBytes == 0);
            GC.KeepAlive(reference);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            // Restore only a still-allocated prefix. Never write after free.
            try
            {
                if (array is not null && originalDestructor != 0 && (bool)Read(array, AllocationLive))
                    Marshal.WriteIntPtr(pointer, offset, originalDestructor);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            try
            {
                invocation?.Dispose();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            if (runtime is not null)
            {
                ProcessRuntime closing = runtime;
                try
                {
                    await Task.Run(async () => await closing.DisposeAsync()).WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            }
        }
        if (failures.Count == 1)
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1)
            throw new AggregateException(failures);
    }
}
