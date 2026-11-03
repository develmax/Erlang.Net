using System.Collections;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Erlang.Tests;

internal static class VmStorageBirthAccess
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static object Field(object target, string name)
        => target.GetType().GetField(name, Members)!.GetValue(target)!;

    internal static object? Property(object target, string name)
        => target.GetType().GetProperty(name, Members)!.GetValue(target);

    internal static object Call(object target, string name, params object[] values)
    {
        try
        {
            return target.GetType().GetMethod(name, Members)!.Invoke(target, values)!;
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    internal static bool HasRegisteredBacking(ProcessContext context)
    {
        object? heap = typeof(ProcessContext).GetField(VmStorageBirthNames.Heap, Members)!.GetValue(context);
        object? frame = context.Dictionary.GetType().GetField(VmStorageBirthNames.DictionaryFrame, Members)!.GetValue(context.Dictionary);
        object registryGate = Field(context.Runtime, VmStorageBirthNames.OwnersGate);
        lock (registryGate)
        {
            var owners = (IDictionary)Field(context.Runtime, VmStorageBirthNames.Owners);

            return heap is not null && frame is not null && owners.Contains(context)
                && ReferenceEquals(owners[context], heap);
        }
    }

    internal static bool ExecutionEnabled(ProcessContext context)
        => (bool)Property(context, VmStorageBirthNames.ExecutionEnabled)!;

    internal static int OwnerCount(ProcessRuntime runtime)
    {
        lock (Field(runtime, VmStorageBirthNames.OwnersGate))
            return ((IDictionary)Field(runtime, VmStorageBirthNames.Owners)).Count;
    }

    internal static bool Registered(ProcessContext context)
    {
        lock (Field(context.Runtime, VmStorageBirthNames.OwnersGate))
            return ((IDictionary)Field(context.Runtime, VmStorageBirthNames.Owners)).Contains(context);
    }

    internal static IDisposable BorrowDictionary(ProcessContext context, Term key)
    {
        object dictionary = context.Dictionary;
        lock (Field(dictionary, VmStorageBirthNames.Gate))
        {
            var data = (Dictionary<Term, Term>)Field(dictionary, VmStorageBirthNames.DictionaryData);
            object frame = Field(dictionary, VmStorageBirthNames.DictionaryFrame);
            object heap = Field(context, VmStorageBirthNames.Heap);

            return (IDisposable)Call(
                heap,
                VmStorageBirthNames.Borrow,
                frame,
                data[key]
            );
        }
    }

    internal static bool HeapClosing(ProcessContext context)
    {
        object heap = Field(context, VmStorageBirthNames.Heap);
        lock (Property(heap, VmStorageBirthNames.HeapGate)!)
            return (bool)Field(heap, VmStorageBirthNames.Closing);
    }

    internal static Task<(BigInteger Collections, BigInteger Words)> Sample(ProcessContext context)
    {
        // Read the existing VM event ledger; never write its counters. Public
        // statistics still uses the historical CLR sample until S1 is complete.
        object statistics = Property(context.Runtime, VmStorageBirthNames.Statistics)!;
        var value = (TupleTerm)Call(statistics, VmStorageBirthNames.SampleVmCollections);
        Require(value.Items.Count == 3 && value.Items[2].Equals(Term.I(0)));

        return Task.FromResult((((Integer)value.Items[0]).Value, ((Integer)value.Items[1]).Value));
    }

    internal static async Task Collect(ProcessContext context)
    {
        Term result = await context.Runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            VmStorageBirthNames.GarbageCollect,
            context.Self,
            Term.List(Term.Tuple(Term.A(VmStorageBirthNames.Type), Term.A(VmStorageBirthNames.Major)))
        );
        Require(result.Equals(Term.A(ErlangBooleanAtoms.True)));
    }

    internal static async Task Until(Func<bool> condition)
    {
        long start = Stopwatch.GetTimestamp();
        while (!condition())
        {
            if (Stopwatch.GetElapsedTime(start) > VmStorageBirthNames.Timeout)
                throw new TimeoutException(VmStorageBirthNames.Failure);
            await Task.Yield();
        }
    }

    internal static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(VmStorageBirthNames.Failure);
    }

    internal static Term Payload() => Term.Tuple(
        Term.List(Term.I(1), Term.I(2), Term.I(3)),
        new FloatTerm(3.25),
        new Integer((BigInteger.One << 130) + 7)
    );
}
