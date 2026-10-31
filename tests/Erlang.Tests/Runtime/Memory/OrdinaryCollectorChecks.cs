using System.Reflection;
using Erlang.Differential;
using static Erlang.Tests.OrdinaryCollectorNames;

namespace Erlang.Tests;

internal static class OrdinaryCollectorChecks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static async Task SourceBacking()
    {
        using var artifact = GeneratedModuleCompiler.Compile(Source, GuestModule + ".erl");
        await using var runtime = OwnedGuest506RuntimeFactory.Create(false);
        artifact.Definition.Register(runtime.Modules);
        await using var otherRuntime = OwnedGuest506RuntimeFactory.Create(false);
        bool observed = false, backed = false;
        runtime.Modules.Register(
            Probe,
            Observe,
            1,
            (_, _) =>
        {
            observed = true;

            return ValueTask.FromResult<Term>(OkAtom);
        }
        );
        runtime.Modules.Register(
            Probe,
            Backing,
            0,
            (context, _) =>
        {
            // Read the existing field: invoking the heap getter would manufacture
            // backing at the observation point and hide the original defect.
            object? heap = typeof(ProcessContext).GetField(HeapField, Members)!.GetValue(context);
            if (heap is not null)
            {
                object snapshot = heap.GetType().GetProperty(HeapSnapshot, Members)!.GetValue(heap)!;
                backed = (int)snapshot.GetType().GetProperty(Blocks, Members)!.GetValue(snapshot)! > 0;
            }

            return ValueTask.FromResult<Term>(backed ? TrueAtom : FalseAtom);
        }
        );
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term result = await actor.InvokeAsync(async context => await runtime.Modules.Call(context, GuestModule, Entry));
        Require(observed && backed && result.Equals(OkAtom));
        TupleTerm sample = Collections(runtime);
        Require(sample.Items[0] is Integer count && count.Value > 0);
        Require(sample.Items[1] is Integer words && words.Value > 0);
        Require(Collections(otherRuntime).Items[0].Equals(Term.I(0)));
        GC.Collect();
        Require(Collections(runtime).Equals(sample));
        Require(Collections(otherRuntime).Items[0].Equals(Term.I(0)));
    }

    private static TupleTerm Collections(ProcessRuntime runtime)
    {
        object statistics = typeof(ProcessRuntime).GetProperty(Statistics, Members)!.GetValue(runtime)!;

        return (TupleTerm)statistics.GetType().GetMethod(Sample, Members)!.Invoke(statistics, null)!;
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(Failure);
    }
}
