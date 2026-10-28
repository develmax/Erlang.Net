using System.Reflection;
using Erlang.Compiler;
using static Erlang.Tests.HybridBoundary507Names;

namespace Erlang.Tests;

internal static class HybridBoundary507Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static async Task MutablePatterns()
    {
        HybridAst507Observation top = await HybridAst507Probe.Observe();
        Check(top.CallbackExecuted && !top.CallbackReceivedOwnedTuple && top.RetainedValueReadableAfterCollection);
        foreach (bool owned in new[] { false, true })
            foreach (bool list in new[] { false, true })
                foreach (bool fail in new[] { false, true })
                    await Nested(owned, list, fail);
    }

    private static async Task Nested(bool owned, bool list, bool fail)
    {
        Pattern[] items = [new Pattern.Any()];
        Pattern shape = list ? new Pattern.List(items) : new Pattern.Tuple(items);
        var body = new Expr.Sequence(new Expr[]
        {
            new Expr.Match(shape, new Expr.Variable(Input)),
            new Expr.Variable(Output)
        });
        var definition = new ModuleDefinition(
            GuestModule,
            new (string Name, int Arity)[] { (Function, 1) },
            new FunctionDefinition[] { new(Function, 1, new Clause[] { new(new Pattern[] { new Pattern.Variable(Input) }, null, body) }) }
        );
        await using var runtime = OwnedGuest506RuntimeFactory.Create(owned);
        definition.Register(runtime.Modules);
        var retained = new List<Term>();
        Dictionary<string, Term>? retainedBindings = null;
        runtime.Modules.Register(
            Probe,
            Echo,
            1,
            (_, values) =>
        {
            Check(Portable(values[0]));

            return ValueTask.FromResult(values[0]);
        }
        );
        // Mutation occurs only after successful built-in pattern publication.
        items[0] = new HybridAsyncPattern507(async (
            context,
            value,
            bindings,
            keys
        ) =>
        {
            await Task.Yield();
            Check(Portable(value) && bindings.Values.All(Portable) && keys.Values.All(Portable));
            retained.Add(value);
            retained.AddRange(bindings.Values);
            retained.AddRange(keys.Values);
            retainedBindings = bindings;
            Term echo = await context.Runtime.Modules.Call(
                context,
                Probe,
                Echo,
                value
            );
            Check(Portable(echo));
            retained.Add(echo);
            var function = new FunctionTerm(1, (_, arguments) =>
            {
                Check(Portable(arguments[0]));

                return ValueTask.FromResult(arguments[0]);
            });
            Term functionEcho = await function.Invoke(context, new Term[] { value });
            Check(Portable(functionEcho));
            retained.Add(functionEcho);
            var nestedBindings = new Dictionary<string, Term>(StringComparer.Ordinal) { [Input] = value };
            var publicPattern = new HybridRetainingPattern507(retained.Add);
            Check(publicPattern.Match(value, nestedBindings, context));
            Check(nestedBindings.Values.All(Portable));
            retained.AddRange(nestedBindings.Values);
            if (fail)
                throw new ErlangException(Term.Tuple(value, bindings[Input], keys[Input]));
            bindings[Output] = Term.Tuple(value, echo);

            return true;
        });
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term child = Term.Tuple(Term.I(507), Term.List(Term.I(1)));
        Term argument = list ? Term.List(child) : Term.Tuple(child);
        ErlangException? observed = null;
        Term? result = null;
        try
        {
            result = await actor.InvokeAsync(async context => await runtime.Modules.Call(
                context,
                GuestModule,
                Function,
                argument
            ));
        }
        catch (ErlangException error) when (fail)
        {
            observed = error;
        }
        if (fail)
            Check(observed is not null && Portable(observed.Reason) && Portable(observed.StackTraceTerm));
        else
            Check(result is not null && result.Equals(Term.Tuple(child, child)) && Portable(result));
        ProcessContext owner = await actor.Ready;
        if (owned)
        {
            object heap = owner.GetType().GetProperty(Heap, Members)!.GetValue(owner)!;
            heap.GetType().GetMethod(Collect, Members)!.Invoke(heap, null);
            object snapshot = heap.GetType().GetProperty(Snapshot, Members)!.GetValue(heap)!;
            Check((long)snapshot.GetType().GetProperty(Words, Members)!.GetValue(snapshot)! == 0);
        }
        Check(retained.Count > 0 && retained.All(Portable));
        foreach (Term value in retained)
            _ = value.ToString();
        Check(retainedBindings is not null && retainedBindings.Values.All(Portable));
    }

    private static bool Portable(Term root)
    {
        var pending = new Stack<Term>();
        pending.Push(root);
        while (pending.TryPop(out Term? value))
        {
            if (value.GetType().GetProperty(Storage, Members)?.GetValue(value) is not null)
                return false;
            if (value is TupleTerm tuple)
                foreach (Term child in tuple.Items) pending.Push(child);
            else if (value is Cons cell)
            {
                pending.Push(cell.Head);
                pending.Push(cell.Tail);
            }
        }

        return true;
    }

    private static void Check(bool value)
    {
        if (!value)
            throw new InvalidOperationException(Failure);
    }
}
