using Erlang;
using static Erlang.Tests.MapSharingSize528Names;

namespace Erlang.Tests;

internal static class MapSharingSize528Checks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry =>
        {
            RuntimeBootstrapHamtMap430.Register(runtime, registry);
            RuntimeBootstrapDebugSharing425.Register(runtime, registry);
        });
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            var rows = Enumerable.Range(1, Entries).Select(key => new KeyValuePair<Term, Term>(
                Term.I(key), Term.Tuple(Term.I(Token), Term.List(Term.I(key)), Term.Tuple())));
            var map = new MapTerm(rows);

            async Task<Term[]> ChildrenOf(Term value)
                => Cons.Items(await runtime.Modules.Call(
                    context,
                    InternalModule,
                    Children,
                    value
                )).ToArray();

            async Task<long> Size(string function, Term value)
                => (long)((Integer)await runtime.Modules.Call(
                    context,
                    DebugModule,
                    function,
                    value
                )).Value;
            Term[] children = await ChildrenOf(map);
            Check(children.Length > 0);
            long physical = HeadWords + children.Length;
            long cleanup = physical;
            var pending = new Stack<Term>(children);
            while (pending.TryPop(out Term? value))
            {
                if (value is Cons)
                    continue;
                Term[] nested;
                if (value.GetType().FullName == BitmapType)
                {
                    nested = await ChildrenOf(value);
                    long own = BoxHeaderWords + nested.Length;
                    physical += own;
                    cleanup += own;
                }
                else if (value is TupleTerm collision)
                {
                    nested = collision.Items.ToArray();
                    physical += BoxHeaderWords + nested.Length;
                }
                else
                    throw new InvalidOperationException(Failure);
                foreach (Term child in nested)
                    pending.Push(child);
            }
            long flat = await Size(Flat, map);
            long shared = await Size(Shared, map);
            Check(flat == physical + Entries * LeafAndValueWords);
            Check(shared == flat + cleanup && cleanup > HeadWords);
            Term raw = children[0];
            long rawFlat = await Size(Flat, raw);
            foreach (Term pair in new[] { Term.Tuple(map, raw), Term.Tuple(raw, map) })
            {
                Check(await Size(Flat, pair) == PairTupleWords + flat + rawFlat);
                Check(await Size(Shared, pair) == PairTupleWords + shared);
            }

            return true;
        });
    }

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(Failure);
    }
}
