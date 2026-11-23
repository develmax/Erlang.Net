using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamOrderedConsCallChecks
{
    internal const string TestName = "compiler/beam/stages-pure-cons-heads-after-tail-call";
    private const string Module = "ordered_cons_call_fixture";
    private const string Provider = "ordered_cons_call_provider";
    private const int MultiHeapWords = 12;
    private const int WrappedHeapWords = 9;
    private const int BoundHeapWords = 15;

    private const string Source = """
        -module(ordered_cons_call_fixture).
        -export([multi/3, wrapped/3, effectful/2, rebound/3, grouped/1,
                 bound/4, bound_effectful/3, bound_rebound/3]).

        multi(A, B, Tail) ->
            [{first,A},{second,B},stop | ordered_cons_call_provider:tail(Tail)].

        wrapped(A, B, Tail) ->
            Items = [{A,B} | ordered_cons_call_provider:tail(Tail)],
            {wrapped, Items, A}.

        effectful(A, Tail) ->
            [{event,ordered_cons_call_provider:mark(A)},{plain,A}
             | ordered_cons_call_provider:tail(Tail)].

        rebound(Items, A, Tail) ->
            Items = [{head,A} | ordered_cons_call_provider:tail(Tail)],
            {wrapped, Items}.

        bound(Owner, A, B, C) ->
            Tail = ordered_cons_call_provider:read(Owner),
            Items = [{first,A},{second,B},{third,C} | ordered_cons_call_provider:tail(Tail)],
            ordered_cons_call_provider:consume(Owner, Items).

        bound_effectful(Owner, A, Tail) ->
            Items = [{event,ordered_cons_call_provider:mark(A)},{plain,A}
                     | ordered_cons_call_provider:tail(Tail)],
            ordered_cons_call_provider:consume(Owner, Items).

        bound_rebound(Items, A, Tail) ->
            Items = [{first,A},{second,A} | ordered_cons_call_provider:tail(Tail)],
            ordered_cons_call_provider:consume(A, Items).

        grouped([{Key,Value}|Rest]) when is_atom(Key) ->
            [{atom,Key},{literal,Value}|grouped(Rest)];
        grouped([Key|Rest]) when is_atom(Key) ->
            [{atom,Key},0|grouped(Rest)];
        grouped([]) ->
            [].
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Module + ".erl");
        object code = Code(beam);
        VerifyConstruction(
            code,
            "multi",
            MultiHeapWords,
            [PutList, PutTuple, PutList, PutTuple, PutList]
        );
        VerifyConstruction(
            code,
            "wrapped",
            WrappedHeapWords,
            [PutTuple, PutList, PutTuple]
        );

        VerifyConstruction(
            code,
            "bound",
            BoundHeapWords,
            [PutTuple, PutList, PutTuple, PutList, PutTuple, PutList],
            callOrdinal: 1
        );

        var image = BeamModule.Prepare(beam, new BeamDependencySelection([Provider]));
        var events = new List<Term>();
        Term failHead = Term.A("fail_head");
        Term failTail = Term.A("fail");
        await using var runtime = new ProcessRuntime();
        runtime.Modules.Register(
            Provider,
            "tail",
            1,
            (_, args) => Observe(
                "tail",
                args[0],
                failTail,
                "tail_failed"
            )
        );
        runtime.Modules.Register(
            Provider,
            "mark",
            1,
            (_, args) => Observe(
                "head",
                args[0],
                failHead,
                "head_failed"
            )
        );
        runtime.Modules.Register(
            Provider,
            "read",
            1,
            (_, args) =>
            {
                events.Add(Event("read", args[0]));

                return ValueTask.FromResult(args[0]);
            }
        );
        runtime.Modules.Register(
            Provider,
            "consume",
            2,
            (_, args) =>
            {
                events.Add(Event("consume", args[0]));

                return ValueTask.FromResult<Term>(Term.Tuple(args[0], args[1]));
            }
        );
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term a = Term.A("a");
            Term b = Term.A("b");
            Term improper = Term.A("improper");
            Term last = Term.List(Term.A("last"));
            Term first = Term.Tuple(Term.A("first"), a);
            Term second = Term.Tuple(Term.A("second"), b);
            Term payload = Term.Tuple(a, b);
            Term head = Term.Tuple(Term.A("head"), a);
            await Check(
                "multi",
                [a, b, improper],
                new Cons(first, new Cons(second, new Cons(Term.A("stop"), improper))),
                [Event("tail", improper)]
            );
            await Check(
                "wrapped",
                [a, b, last],
                Term.Tuple(Term.A("wrapped"), new Cons(payload, last), a),
                [Event("tail", last)]
            );
            await Check(
                "effectful",
                [a, Nil.Value],
                Term.List(Term.Tuple(Term.A("event"), a), Term.Tuple(Term.A("plain"), a)),
                [Event("head", a), Event("tail", Nil.Value)]
            );
            await Check(
                "effectful",
                [a, failTail],
                Term.A("tail_failed"),
                [Event("head", a), Event("tail", failTail)],
                throws: true
            );
            await Check(
                "effectful",
                [failHead, Nil.Value],
                Term.A("head_failed"),
                [Event("head", failHead)],
                throws: true
            );
            await Check(
                "rebound",
                [Nil.Value, a, Nil.Value],
                Term.Tuple(Term.A("badmatch"), Term.List(head)),
                [Event("tail", Nil.Value)],
                throws: true
            );
            Term c = Term.A("c");
            Term third = Term.Tuple(Term.A("third"), c);
            await Check(
                "bound",
                [improper, a, b, c],
                Term.Tuple(improper, new Cons(first, new Cons(second, new Cons(third, improper)))),
                [Event("read", improper), Event("tail", improper), Event("consume", improper)]
            );
            await Check(
                "bound",
                [failTail, a, b, c],
                Term.A("tail_failed"),
                [Event("read", failTail), Event("tail", failTail)],
                throws: true
            );
            await Check(
                "bound_effectful",
                [a, failHead, failTail],
                Term.A("head_failed"),
                [Event("head", failHead)],
                throws: true
            );
            await Check(
                "bound_rebound",
                [Nil.Value, a, Nil.Value],
                Term.Tuple(Term.A("badmatch"), Term.List(first, Term.Tuple(Term.A("second"), a))),
                [Event("tail", Nil.Value)],
                throws: true
            );
            await Check(
                "grouped",
                [Term.List(Term.Tuple(Term.A("key"), Term.A("value")), Term.A("plain"))],
                Term.List(
                    Term.Tuple(Term.A("atom"), Term.A("key")),
                    Term.Tuple(Term.A("literal"), Term.A("value")),
                    Term.Tuple(Term.A("atom"), Term.A("plain")),
                    Term.I(0)
                ),
                []
            );

            return Nil.Value;

            async ValueTask Check(
                string function,
                Term[] args,
                Term expected,
                Term[] expectedEvents,
                bool throws = false
            )
            {
                events.Clear();
                Term result;
                bool caught = false;
                try
                {
                    result = await image.Invoke(context, function, args);
                }
                catch (ErlangException error)
                {
                    caught = true;
                    result = error.Reason;
                }
                if (caught != throws || !result.Equals(expected) || !events.SequenceEqual(expectedEvents))
                {
                    throw new InvalidOperationException(BeamOrderedConsCallDiagnostics.EvaluationOrder);
                }
            }
        });

        ValueTask<Term> Observe(
            string kind,
            Term value,
            Term fails,
            string reason
        )
        {
            events.Add(Event(kind, value));
            if (value.Equals(fails))
            {
                throw new ErlangException(Term.A(reason));
            }

            return ValueTask.FromResult(value);
        }
    }

    private static Term Event(string kind, Term value) => Term.Tuple(Term.A(kind), value);

    private static void VerifyConstruction(
        object code,
        string function,
        int heapWords,
        byte[] expected,
        int callOrdinal = 0
    )
    {
        object entry = Items(code, "Functions").Single(item => (string)Property(item, "Name") == function);
        int start = (int)Property(entry, "FirstInstructionIndex");
        int end = (int)Property(entry, "EndInstructionIndex");
        object[] instructions = Items(code, "Instructions")[start..end];
        int call = Enumerable.Range(0, instructions.Length)
            .Where(index => Opcode(instructions[index]) == ExternalCall)
            .Skip(callOrdinal)
            .FirstOrDefault(-1);
        if (call < 0 || call + 1 >= instructions.Length || Opcode(instructions[call + 1]) != TestHeap
            || instructions.Take(call).Any(instruction => Opcode(instruction) is PutTuple or PutList)
            || (BigInteger)Property(Items(instructions[call + 1], "Operands")[0], "Value") != heapWords
            || !instructions.Skip(call + 2).Select(Opcode).Where(opcode => opcode is PutTuple or PutList).SequenceEqual(expected))
        {
            throw new InvalidOperationException(BeamOrderedConsCallDiagnostics.ConstructionSchedule);
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
