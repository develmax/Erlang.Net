using System.Reflection;
using Erlang.Differential;
using static Erlang.Tests.SourceLiveness511Names;

namespace Erlang.Tests;

internal static class SourceLiveness511Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static async Task Run()
    {
        using var artifact = GeneratedModuleCompiler.Compile(Source, GuestModule + ".erl");
        foreach (string entry in Entries)
        {
            Term ordinary = await Observe(false, entry);
            Term owned = await Observe(true, entry);
            Require(ordinary.Equals(owned));
            Require(ExternalTermFormat.Encode(ordinary).AsSpan().SequenceEqual(ExternalTermFormat.Encode(owned)));
        }

        async Task<Term> Observe(bool enabled, string entry)
        {
            await using var runtime = OwnedGuest506RuntimeFactory.Create(enabled);
            artifact.Definition.Register(runtime.Modules);
            var collections = new List<(long Reclaimed, long Live)>();
            runtime.Modules.Register(
                Probe,
                Collect,
                0,
                async (context, _) =>
            {
                await Task.Yield();
                CollectOwned(context);

                return Term.A(Ok);
            }
            );
            runtime.Modules.Register(
                Probe,
                Yield,
                1,
                async (context, arguments) =>
            {
                await Task.Yield();
                CollectOwned(context);

                return arguments[0];
            }
            );
            await using var actor = new RuntimeNodeFoundationHostActor(runtime);
            Term result = await actor.InvokeAsync(async context => await runtime.Modules.Call(context, GuestModule, entry));
            if (enabled && entry == Discarded)
            {
                Require(collections.Count == 2);
                Require(collections.All(item => item.Reclaimed > 0));
                Require(collections[0].Live == 0 && collections[1].Live == 0);
            }
            if (enabled && entry == Tail)
            {
                Require(collections.Count == 33);
                Require(collections.Take(32).All(item => item.Reclaimed > 0));
                Require(collections.All(item => item.Live == collections[0].Live));
            }

            return result;

            void CollectOwned(ProcessContext context)
            {
                if (!enabled)
                    return;
                object heap = typeof(ProcessContext).GetProperty(Heap, Members)!.GetValue(context)!;
                long reclaimed = (long)heap.GetType().GetMethod(HeapCollect, Members)!.Invoke(heap, null)!;
                object snapshot = heap.GetType().GetProperty(Snapshot, Members)!.GetValue(heap)!;
                long live = (long)snapshot.GetType().GetProperty(Words, Members)!.GetValue(snapshot)!;
                collections.Add((reclaimed, live));
            }
        }
    }

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(Failure);
    }
}
