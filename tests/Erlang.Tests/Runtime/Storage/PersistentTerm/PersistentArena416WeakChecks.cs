using System.Runtime.CompilerServices;
using Erlang.Compiler;

namespace Erlang.Tests;

internal static class PersistentArena416WeakChecks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime(1);
        new Parser(GuestStoredFunction407Names.Source).ParseModule().Register(runtime.Modules);
        object store = PersistentArena416HostAccess.Create(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        try
        {
            var weak = await actor.InvokeAsync(context => Stage(store, context));
            _ = await actor.Invoke(_ => true);
            GuestStoredFunction407Access.Collected(weak.Graph);
            GuestStoredFunction407Access.Collected(weak.BinaryWrapper);
            GuestStoredFunction407Access.Collected(weak.Function);
            await actor.InvokeAsync(async context =>
            {
                var value = (TupleTerm)PersistentArena416HostAccess.Get(store, context, Term.A(PersistentArena416HostNames.Key));
                var graph = (TupleTerm)value.Items[0];
                var function = (FunctionTerm)value.Items[1];
                PersistentArena416HostAccess.Require(ReferenceEquals(graph.Items[0], graph.Items[1])
                    && ((BitString)((TupleTerm)graph.Items[0]).Items[0]).BitLength == 1001
                    && ReferenceEquals(function.Environment.Single(), graph));
                var returned = (TupleTerm)await function.Invoke(context, [Nil.Value]);
                PersistentArena416HostAccess.Require(ReferenceEquals(returned.Items[0], graph) && returned.Items[1] is Nil);

                return true;
            });
        }
        finally
        {
            await PersistentArena416HostAccess.Close(store);
        }
        PersistentArena416HostAccess.Empty(store);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<(WeakReference<object> Graph, WeakReference<object> BinaryWrapper, WeakReference<object> Function)> Stage(object store, ProcessContext context)
    {
        var binary = new BitString(Enumerable.Range(0, 2048).Select(index => (byte)index).ToArray()).SliceBits(5, 1001);
        var shared = Term.Tuple(binary, Term.List(Term.I(1), Term.I(2)));
        var graph = Term.Tuple(shared, shared);
        var function = (FunctionTerm)await context.Runtime.Modules.Call(
            context,
            GuestStoredFunction407Names.ModuleName,
            GuestStoredFunction407Names.Make,
            graph
        );
        await PersistentArena416HostAccess.Put(
            store,
            context,
            Term.A(PersistentArena416HostNames.Key),
            Term.Tuple(graph, function)
        );

        return (new(graph), new(binary), new(function));
    }
}
