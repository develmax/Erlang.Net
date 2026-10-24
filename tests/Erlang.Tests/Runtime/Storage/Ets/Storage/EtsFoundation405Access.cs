using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Erlang;

namespace Erlang.Tests;

internal static class EtsFoundation405Access
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    internal static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(EtsFoundation405Names.Failure);
    }

    internal static object Get(ProcessRuntime runtime)
        => typeof(ProcessRuntime).GetProperty(EtsFoundation405Names.Property, Members)!.GetValue(runtime)!;

    internal static bool Unallocated(ProcessRuntime runtime)
        => typeof(ProcessRuntime).GetField(EtsFoundation405Names.Field, Members)!.GetValue(runtime) is null;

    internal static object? Invoke(object owner, string name, params object?[] arguments)
    {
        try
        {
            return owner.GetType().GetMethod(name, Members)!.Invoke(owner, arguments);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    internal static object Protection(string name)
        => Enum.Parse(typeof(ProcessRuntime).Assembly.GetType(EtsFoundation405Names.ProtectionType, true)!, name);

    internal static void CaptureCanceled(
        object storage,
        ProcessRuntime runtime,
        TupleTerm value,
        CancellationToken cancellation
    )
    {
        object accounting = storage.GetType().GetField(EtsFoundation405Names.AccountingField, Members)!.GetValue(storage)!;
        MethodInfo capture = typeof(ProcessRuntime).Assembly.GetType(EtsFoundation405Names.StoredObjectType, true)!
            .GetMethod(EtsFoundation405Names.Capture, BindingFlags.Static | BindingFlags.NonPublic)!;
        try
        {
            _ = capture.Invoke(null, new object[] { accounting, runtime, value, cancellation });
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    internal static Term Create(
        object owner,
        ProcessContext context,
        bool named = false,
        string protection = EtsFoundation405Names.Protected
    )
        => (Term)Invoke(
            owner,
            EtsFoundation405Names.Create,
            context,
            Term.A(EtsFoundation405Names.Name),
            named,
            Protection(protection)
        )!;

    internal static long Store(
        object owner,
        ProcessContext context,
        Term table,
        TupleTerm value
    )
        => (long)Invoke(
            owner,
            EtsFoundation405Names.Store,
            context,
            table,
            value
        )!;

    internal static Term Read(
        object owner,
        ProcessContext context,
        Term table,
        long slot
    )
        => (Term)Invoke(
            owner,
            EtsFoundation405Names.Read,
            context,
            table,
            slot
        )!;

    internal static ITuple Snapshot(object owner) => (ITuple)Invoke(owner, EtsFoundation405Names.Snapshot)!;

    internal static void Empty(object owner, bool closed = false)
    {
        ITuple data = Snapshot(owner);
        EtsFoundation405Access.Require((int)data[0]! == 0 && (int)data[1]! == 0 && (int)data[2]! == 0
            && (long)data[3]! == 0 && (long)data[4]! == 0 && (bool)data[5]! == closed);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void Collect()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static bool Alive(WeakReference<ReferenceTerm> reference) => reference.TryGetTarget(out _);
}
