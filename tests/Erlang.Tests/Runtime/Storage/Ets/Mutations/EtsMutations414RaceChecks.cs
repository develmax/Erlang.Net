using Erlang;
namespace Erlang.Tests;

internal static class EtsMutations414RaceChecks
{
    internal static async Task SingleWinners()
    {
        await using var runtime = new ProcessRuntime();
        EtsMutations414Checks.Register(runtime);
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        Term table = await owner.InvokeAsync(async c => await runtime.Modules.Call(
            c,
            EtsMutations414Names.Ets,
            EtsMutations414Names.New,
            Term.A(EtsMutations414Names.Table),
            Term.List(Term.A(EtsMutations414Names.Public))
        ));
        var actors = Enumerable.Range(0, EtsMutations414Names.Actors).Select(_ => new RuntimeNodeFoundationHostActor(runtime)).ToArray();
        try
        {
            Term[] inserted = await Task.WhenAll(actors.Select((actor, index) => actor.InvokeAsync(async c => await runtime.Modules.Call(
                c,
                EtsMutations414Names.Ets,
                EtsMutations414Names.InsertNew,
                table,
                Term.Tuple(Term.I(1), Term.I(index))
            ))));
            EtsMutations414Checks.Require(inserted.Count(x => x.Equals(Term.A(ErlangBooleanAtoms.True))) == 1);
            Term[] taken = await Task.WhenAll(actors.Select(actor => actor.InvokeAsync(async c => await runtime.Modules.Call(
                c,
                EtsMutations414Names.Ets,
                EtsMutations414Names.Take,
                table,
                Term.I(1)
            ))));
            EtsMutations414Checks.Require(taken.Count(x => x is Cons) == 1 && taken.Count(x => x is Nil) == actors.Length - 1);
        }
        finally
        {
            foreach (var actor in actors) await actor.DisposeAsync();
            _ = await owner.InvokeAsync(async c => await runtime.Modules.Call(
                c,
                EtsMutations414Names.Ets,
                EtsMutations414Names.Delete,
                table
            ));
        }
    }

    internal static async Task Updates()
    {
        await using var runtime = new ProcessRuntime();
        EtsMutations414Checks.Register(runtime);
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        await using var left = new RuntimeNodeFoundationHostActor(runtime);
        await using var right = new RuntimeNodeFoundationHostActor(runtime);
        Term table = await owner.InvokeAsync(async c => await runtime.Modules.Call(
            c,
            EtsMutations414Names.Ets,
            EtsMutations414Names.New,
            Term.A(EtsMutations414Names.Table),
            Term.List(Term.A(EtsMutations414Names.Public))
        ));
        _ = await owner.InvokeAsync(async c => await runtime.Modules.Call(
            c,
            EtsMutations414Names.Ets,
            EtsMutations414Names.Insert,
            table,
            Term.Tuple(Term.I(1), Term.I(0), Term.I(0))
        ));

        async Task Write(RuntimeNodeFoundationHostActor actor, int position)
        {
            for (int index = 1; index <= EtsMutations414Names.Iterations; index++)
                _ = await actor.InvokeAsync(async c => await runtime.Modules.Call(
                    c,
                    EtsMutations414Names.Ets,
                    EtsMutations414Names.Update,
                    table,
                    Term.I(1),
                    Term.Tuple(Term.I(position), Term.I(index))
                ));
        }
        try
        {
            await Task.WhenAll(Write(left, 2), Write(right, 3));
            Term value = await owner.InvokeAsync(async c => await runtime.Modules.Call(
                c,
                EtsMutations414Names.Ets,
                EtsMutations414Names.Lookup,
                table,
                Term.I(1)
            ));
            EtsMutations414Checks.Require(value.Equals(Term.List(Term.Tuple(Term.I(1), Term.I(EtsMutations414Names.Iterations), Term.I(EtsMutations414Names.Iterations)))));
        }
        finally
        {
            _ = await owner.InvokeAsync(async c => await runtime.Modules.Call(
                c,
                EtsMutations414Names.Ets,
                EtsMutations414Names.Delete,
                table
            ));
        }
    }
}
