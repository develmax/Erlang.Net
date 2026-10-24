using System.Runtime.CompilerServices;

namespace Erlang.Tests;

internal static class EtsCompiled419LifetimeChecks
{
    internal static async Task Run()
    {
        var runtime = new ProcessRuntime(1);
        RuntimeNodeFoundationHostActor? actor = null;
        try
        {
            await EtsCompiled419Access.Initialize(runtime);
            actor = new(runtime);
            var held = await Create(actor);
            _ = await actor.Invoke(_ => true);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            EtsCompiled419Access.Require(Alive(held.Literal));
            await actor.InvokeAsync(async context =>
            {
                Term result = await EtsCompiled419Access.Call(
                    context,
                    EtsCompiled419Names.Run,
                    Term.List(Term.I(1)),
                    held.Program,
                    Nil.Value
                );
                EtsCompiled419Access.Require(Cons.Items(result).Single() is BitString { BitLength: 4096 });

                return true;
            });
            _ = await actor.Invoke(_ => true);
            await runtime.DisposeAsync();
            await actor.DisposeAsync();
            GuestStoredFunction407Access.Collected(held.Literal);
            EtsCompiled419Access.Require(runtime.ReferenceResources.LiveResources == 0);
            GC.KeepAlive(held.Program); // A retained closed magic ref must not keep its program literals alive.
        }
        finally
        {
            try
            {
                if (actor is not null) await actor.DisposeAsync();
            }
            finally
            {
                await runtime.DisposeAsync();
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Alive(WeakReference<object> value) => value.TryGetTarget(out _);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Task<(Term Program, WeakReference<object> Literal)> Create(RuntimeNodeFoundationHostActor actor)
        => actor.InvokeAsync(async context =>
        {
            var value = new BitString(new byte[512]);
            Term specification = Term.List(Term.Tuple(Term.A(EtsCompiled419Names.Wildcard), Nil.Value,
                Term.List(Term.Tuple(Term.A(EtsCompiled419Names.Const), value))));
            Term program = await EtsCompiled419Access.Call(context, EtsCompiled419Names.Compile, specification);

            return (program, new WeakReference<object>(value));
        });
}
