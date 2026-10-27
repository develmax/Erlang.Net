using System.Reflection;
using Erlang.Compiler;
using static Erlang.Tests.HybridAst507Names;

namespace Erlang.Tests;

// Reproduces mutation of public pattern arrays after source registration.
internal static class HybridAst507Probe
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static async Task<HybridAst507Observation> Observe()
    {
        Pattern[] patterns = [new Pattern.Any()];
        var clause = new Clause(patterns, null, new Expr.Literal(Term.A(Ok)));
        var function = new FunctionDefinition(Function, 1, new Clause[] { clause });
        var definition = new ModuleDefinition(GuestModule, new (string Name, int Arity)[] { (Function, 1) },
            new FunctionDefinition[] { function });
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        definition.Register(runtime.Modules);
        Term? retained = null;
        // Registration/code-generation already succeeded with built-in Any.
        // The public records retain this nested mutable array by reference.
        patterns[0] = new HybridRetainingPattern507(value => retained = value);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
            await runtime.Modules.Call(
                context,
                GuestModule,
                Function,
                Term.Tuple(Term.I(507), Term.List(Term.I(1)))
            ));
        bool owned = retained is TupleTerm tuple
            && tuple.GetType().GetProperty(Storage, Members)!.GetValue(tuple) is not null;
        ProcessContext owner = await actor.Ready;
        object heap = owner.GetType().GetProperty(Heap, Members)!.GetValue(owner)!;
        heap.GetType().GetMethod(Collect, Members)!.Invoke(heap, null);
        if (retained is null)
            throw new InvalidOperationException(Failure);
        bool readable;
        string? failure = null;
        try
        {
            _ = retained.ToString();
            readable = true;
        }
        catch (ObjectDisposedException error)
        {
            readable = false;
            failure = error.GetType().FullName;
        }

        return new(
            true,
            owned,
            readable,
            failure
        );
    }
}
