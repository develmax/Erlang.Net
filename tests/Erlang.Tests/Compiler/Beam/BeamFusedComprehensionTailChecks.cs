using System.Collections;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamFusedComprehensionTailChecks
{
    internal const string TestName = "compiler/beam/lifts-comprehensions-with-bound-append-tails";
    private const string Fixture = "beam_fused_comprehension_tail.erl";
    private const string WrongTopology = "The comprehension helper has the wrong captures.";
    private const string WrongValue = "The comprehension did not preserve its append tail.";
    private const string MissingFailure = "The comprehension failed to reject the generator input.";

    private const string Source = """
        -module(beam_fused_comprehension_tail).
        -export([first/3,last/3,shared/2,shadow/2,strict/3,dynamic/1,nested/3]).

        first(Tail, Items, Key) -> [pair(X, Key) || X <- Items] ++ Tail.
        last(Key, Items, Tail) -> [pair(X, Key) || X <- Items] ++ Tail.
        shared(Items, Tail) -> [pair(X,Tail) || X <- Items] ++ Tail.
        shadow(Items, Tail) -> [pair(Tail,Tail) || Tail <- Items] ++ Tail.
        strict(Key, Items, Tail) -> [pair(X, Key) || {item,X} <:- Items] ++ Tail.
        nested(Key, Items, Tail) -> {result,[pair(X,Key) || X <- Items] ++ Tail}.
        dynamic(Items) -> [pair(X,mapped) || X <- mark(source,Items)] ++ mark(tail,[]).

        pair(A,B) -> {A,B}.
        mark(Tag,Value) -> erlang:put(mark_order,[Tag|erlang:get(mark_order)]), Value.
        """;

    internal static async Task Run()
    {
        byte[] bytecode = CodeGeneration.CompileBeamModule(Source, Fixture);
        var functions = ((IEnumerable)Property(Code(bytecode), "Functions")).Cast<object>()
            .Select(function => ((string)Property(function, "Name"), (int)Property(function, "Arity")))
            .ToHashSet();
        (string Name, int Arity)[] closures =
        [
            ("first/3", 3), ("last/3", 3), ("shared/2", 2), ("strict/3", 3), ("nested/3", 3),
            ("shadow/2", 1), ("dynamic/1", 1)
        ];
        if (closures.Any(closure => !functions.Contains(($"-{closure.Name}-lc$^0/1-0-", closure.Arity))))
        {
            throw new InvalidOperationException(WrongTopology);
        }

        var module = BeamModule.Prepare(bytecode);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term a = Term.A("a");
            Term b = Term.A("b");
            Term key = Term.A("key");
            Term tail = Term.A("tail");
            Term bad = Term.A("bad");
            Term items = Term.List(a, b);
            Term listTail = Term.List(tail);
            Term expected = new Cons(Term.Tuple(a, key), new Cons(Term.Tuple(b, key), tail));

            (string Function, Term[] Arguments, Term Expected)[] cases =
            [
                ("first", [tail, items, key], expected),
                ("last", [key, items, tail], expected),
                ("first", [tail, Nil.Value, key], tail),
                ("shared", [Term.List(a), listTail], Term.List(Term.Tuple(a, listTail), tail)),
                ("shadow", [Term.List(a), listTail], Term.List(Term.Tuple(a, a), tail)),
                ("strict", [key, Term.List(Term.Tuple(Term.A("item"), a)), tail], new Cons(Term.Tuple(a, key), tail)),
                ("nested", [key, Term.List(a), tail], Term.Tuple(Term.A("result"), new Cons(Term.Tuple(a, key), tail)))
            ];
            foreach (var test in cases)
            {
                if (!test.Expected.Equals(await module.Invoke(context, test.Function, test.Arguments)))
                {
                    throw new InvalidOperationException(WrongValue);
                }
            }

            await RequireFailure(ErlangErrorReasons.BadGenerator, "first", [tail, bad, key]);
            await RequireFailure(ErlangErrorReasons.BadMatch, "strict", [key, Term.List(bad), tail]);

            return Nil.Value;

            async Task RequireFailure(string reason, string function, Term[] arguments)
            {
                try
                {
                    await module.Invoke(context, function, arguments);
                }
                catch (ErlangException failure) when (Term.Tuple(Term.A(reason), bad).Equals(failure.Reason))
                {
                    return;
                }

                throw new InvalidOperationException(MissingFailure);
            }
        });
    }
}
