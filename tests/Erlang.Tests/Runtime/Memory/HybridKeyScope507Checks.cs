using System.Reflection;
using Erlang.Compiler;
using static Erlang.Tests.HybridKeyScope507Names;

namespace Erlang.Tests;

internal static class HybridKeyScope507Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static async Task Parity()
    {
        foreach (bool list in new[] { false, true })
            foreach (bool asynchronous in new[] { false, true })
            {
                Term ordinary = await Observe(false, list, asynchronous);
                Term owned = await Observe(true, list, asynchronous);
                Check(ordinary.Equals(owned));
            }
    }

    private static async Task<Term> Observe(bool owned, bool list, bool asynchronous)
    {
        Pattern[] children = [new Pattern.Any(), MapFieldPattern(), new Pattern.Any()];
        Pattern shape = list ? new Pattern.List(children) : new Pattern.Tuple(children);
        var body = new Expr.Sequence(new Expr[]
        {
            new Expr.Match(new Pattern.Variable(Key), new Expr.Literal(Term.A(OldKey))),
            new Expr.Match(shape, new Expr.Variable(Input)),
            new Expr.Tuple(new Expr[] { new Expr.Variable(Value), new Expr.Variable(Alias) })
        });
        var definition = new ModuleDefinition(
            GuestModule,
            new (string Name, int Arity)[] { (Function, 1) },
            new FunctionDefinition[] { new(Function, 1, new Clause[] { new(new Pattern[] { new Pattern.Variable(Input) }, null, body) }) }
        );
        await using var runtime = OwnedGuest506RuntimeFactory.Create(owned);
        definition.Register(runtime.Modules);
        var retained = new List<Term>();
        var retainedDictionaries = new List<Dictionary<string, Term>>();
        var binaryAlias = new BitString(new byte[AliasBytes]);
        bool observed = false;
        // Mutate the public nested array only after successful registration.
        children[0] = new HybridKeyScopePattern507(
            (
                value,
                bindings,
                keys,
                context
            ) =>
        {
            Capture(value, bindings, keys);
            keys[Key] = Term.A(NewKey);
            bindings[Alias] = binaryAlias;
            keys[Alias] = binaryAlias;
            Check(ReferenceEquals(BinaryBacking((BitString)bindings[Alias]), BinaryBacking((BitString)keys[Alias])));
            Check(ReferenceEquals(BinaryBacking((BitString)bindings[Alias]), BinaryBacking(binaryAlias)));
            PublicReentry(context!);
        },
            asynchronous
        );
        children[2] = new HybridKeyScopePattern507(
            (
                value,
                bindings,
                keys,
                _
            ) =>
        {
            Capture(value, bindings, keys);
            Check(keys[Key].Equals(Term.A(NewKey)));
            // This is the next opaque callback, after actual VM import/export.
            // copy_struct copies boxed occurrences; Host backing identity is
            // asserted above within one callback, not across that transfer.
            Check(bindings[Alias].Equals(keys[Alias]));
            Check(bindings[Alias].Equals(binaryAlias));
            observed = true;
        },
            asynchronous
        );
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        var map = new MapTerm(new KeyValuePair<Term, Term>[]
        {
            new(Term.A(OldKey), Term.I(0)), new(Term.A(NewKey), Term.I(1))
        });
        Term argument = list ? Term.List(Term.I(0), map, Term.I(0)) : Term.Tuple(Term.I(0), map, Term.I(0));
        Term result = await actor.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            GuestModule,
            Function,
            argument
        ));
        Check(observed && result is TupleTerm tuple && tuple.Items[0].Equals(Term.I(1))
            && ((BitString)tuple.Items[1]).BitLength == AliasBytes * 8);
        if (owned)
        {
            ProcessContext context = await actor.Ready;
            object heap = context.GetType().GetProperty(Heap, Members)!.GetValue(context)!;
            heap.GetType().GetMethod(Collect, Members)!.Invoke(heap, null);
            object snapshot = heap.GetType().GetProperty(Snapshot, Members)!.GetValue(heap)!;
            Check((long)snapshot.GetType().GetProperty(Words, Members)!.GetValue(snapshot)! == 0);
        }
        Check(retained.All(Portable) && retainedDictionaries.SelectMany(dictionary => dictionary.Values).All(Portable));
        foreach (Term value in retained)
            _ = value.ToString();

        return result;

        void Capture(Term value, Dictionary<string, Term> bindings, Dictionary<string, Term> keys)
        {
            Check(Portable(value) && bindings.Values.All(Portable) && keys.Values.All(Portable));
            retained.Add(value);
            retained.AddRange(bindings.Values);
            retained.AddRange(keys.Values);
            retainedDictionaries.Add(bindings);
            retainedDictionaries.Add(keys);
        }
    }

    private static Pattern MapFieldPattern() => new MapPattern(new MapPatternField[]
    {
        new(new Expr.Variable(Key), new Pattern.Variable(Value))
    });

    private static void PublicReentry(ProcessContext context)
    {
        Pattern[] children = [new HybridKeyScopePattern507((
            _,
            _,
            keys,
            _
        ) => keys[Key] = Term.A(NewKey), false), MapFieldPattern()];
        foreach (bool list in new[] { false, true })
        {
            Pattern shape = list ? new Pattern.List(children) : new Pattern.Tuple(children);
            var bindings = new Dictionary<string, Term> { [Key] = Term.A(OldKey) };
            var keys = new Dictionary<string, Term> { [Key] = Term.A(OldKey) };
            var map = new MapTerm(new KeyValuePair<Term, Term>[]
            {
                new(Term.A(OldKey), Term.I(0)), new(Term.A(NewKey), Term.I(1))
            });
            Term value = list ? Term.List(Term.I(0), map) : Term.Tuple(Term.I(0), map);
            Check(shape.Match(
                value,
                bindings,
                context,
                keys
            ));
            Check(bindings[Value].Equals(Term.I(1)) && bindings.Values.All(Portable));
            // Public Match deliberately clones its incoming key scope. The
            // mutation is shared by siblings, not written into that host map.
            Check(keys[Key].Equals(Term.A(OldKey)));
        }
    }

    private static object BinaryBacking(BitString binary) => typeof(BitString).GetField(Backing, Members)!.GetValue(binary)!;

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
            else if (value is MapTerm map)
                foreach (var pair in map.Entries)
                {
                    pending.Push(pair.Key);
                    pending.Push(pair.Value);
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
