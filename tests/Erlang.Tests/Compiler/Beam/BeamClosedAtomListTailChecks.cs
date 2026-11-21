using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamClosedAtomListTailChecks
{
    internal const string TestName = "compiler/beam/closed-atom-list-tail";
    private const string Fixture = "beam_closed_atom_list_tail_probe.erl";
    private const string IncorrectEntry = "A closed atom selector did not share return paths or retain the original function arguments on failure.";
    private const string IncorrectValue = "A closed atom list-tail clause changed its return or failure value.";
    private const string Source = """
        -module(beam_closed_atom_list_tail_probe).
        -export([decode/3, encode/3, reverse_order/3, distinct/3, fallback/3, two/3, direct/3, chain/3, entry_chain/3, entry_fallback/3]).
        decode(value, V, _) -> V;
        decode(params, V, _) -> V;
        decode(caps, V, [H|T]) -> decode_field(H, V, T).
        encode('Value', V, _) -> {V,0};
        encode('Params', V, _) -> {V,0};
        encode(caps, V, [H|T]) -> encode_field(H, V, T).
        reverse_order(zeta, V, _) -> V;
        reverse_order(zulu, V, _) -> V;
        reverse_order(a, V, [H|T]) -> decode_field(H, V, T).
        distinct(value, V, _) -> V;
        distinct(params, V, _) -> {V,7};
        distinct(caps, V, [H|T]) -> decode_field(H, V, T).
        fallback(value, V, _) -> V;
        fallback(params, V, _) -> V;
        fallback(caps, V, [H|T]) -> decode_field(H, V, T);
        fallback(_, _, _) -> unchanged.
        two(params, V, _) -> {V,2};
        two(caps, V, [H|T]) -> decode_field(H, V, T).
        direct('Params', V, _) -> wrap(V, [4]);
        direct(caps, V, [H|T]) -> decode_field(H, V, T).
        chain('Params', V, _) ->
            Prepared = check(V),
            wrap(Prepared, [2]);
        chain(caps, V, [H|T]) -> decode_field(H, V, T).
        entry_chain('Params', V, _) ->
            Prepared = check(V),
            wrap(Prepared, [2]).
        entry_fallback('Params', V, _) ->
            Prepared = check(V),
            wrap(Prepared, [2]);
        entry_fallback(_, _, _) -> unchanged.
        check(Value) -> {checked,Value}.
        wrap(Value, Tags) -> {Value,Tags}.
        decode_field(type, V, _) -> V.
        encode_field(type, V, _) -> {V,0}.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        CheckInstructions(image);
        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term value = Term.A("payload");
            Term improper = new Cons(Term.A("type"), Term.A("improper_tail"));
            if (!value.Equals(await module.Invoke(
                context,
                "decode",
                Term.A("caps"),
                value,
                improper
            ))
                || !value.Equals(await module.Invoke(
                    context,
                    "reverse_order",
                    Term.A("zulu"),
                    value,
                    Nil.Value
                ))
                || !Term.Tuple(value, Term.I(0)).Equals(await module.Invoke(
                    context,
                    "encode",
                    Term.A("caps"),
                    value,
                    improper
                ))
                || !Term.Tuple(value, Term.I(7)).Equals(await module.Invoke(
                    context,
                    "distinct",
                    Term.A("params"),
                    value,
                    Nil.Value
                ))
                || !Term.Tuple(value, Term.I(2)).Equals(await module.Invoke(
                    context,
                    "two",
                    Term.A("params"),
                    value,
                    Nil.Value
                ))
                || !Term.Tuple(value, Term.List(Term.I(4))).Equals(await module.Invoke(
                    context,
                    "direct",
                    Term.A("Params"),
                    value,
                    Nil.Value
                ))
                || !Term.Tuple(Term.Tuple(Term.A("checked"), value), Term.List(Term.I(2))).Equals(
                    await module.Invoke(
                        context,
                        "chain",
                        Term.A("Params"),
                        value,
                        Nil.Value
                    ))
                || !Term.A("unchanged").Equals(await module.Invoke(
                    context,
                    "fallback",
                    Term.A("caps"),
                    value,
                    Nil.Value
                )))
            {
                throw new InvalidOperationException(IncorrectValue);
            }

            await CheckFailure(Term.A("caps"), Nil.Value);
            await CheckFailure(Term.A("other"), improper);
            await CheckFailure(Term.A("caps"), Term.List(Term.A("invalid")));
            await CheckFailure(Term.A("other"), Nil.Value, function: "entry_chain");

            return Nil.Value;

            async Task CheckFailure(Term key, Term list, string function = "decode")
            {
                try
                {
                    _ = await module.Invoke(
                        context,
                        "decode",
                        key,
                        value,
                        list
                    );
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.A("function_clause")))
                {
                    return;
                }

                throw new InvalidOperationException(IncorrectValue);
            }
        });
    }

    private static void CheckInstructions(byte[] image)
    {
        object code = Code(image);
        object[] instructions = Children(code, "Instructions");
        string[] atoms = Children(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        CheckClosed("decode", tailFirst: false);
        CheckClosed("encode", tailFirst: true);
        CheckClosed("reverse_order", tailFirst: false);
        CheckClosed("two", tailFirst: false, expectedArms: 2);
        CheckTwoCallArms("direct", hasFrame: false);
        CheckTwoCallArms("chain", hasFrame: true);
        CheckSingle("decode_field");
        CheckSingle("encode_field");
        var (chainFailure, chain) = ReadBody("entry_chain");
        var (fallbackFailure, fallback) = ReadBody("entry_fallback");
        if (Opcode(chain[0]) != ExactEqualTest || Value(Children(chain[0], "Operands")[0]) != chainFailure
            || chain.Count(item => Opcode(item) == LocalCall) != 1
            || Opcode(chain[^1]) != TailLocalCallWithFrame
            || Opcode(fallback[0]) == ExactEqualTest && Value(Children(fallback[0], "Operands")[0]) == fallbackFailure)
        {
            throw new InvalidOperationException(IncorrectEntry);
        }


        void CheckClosed(string function, bool tailFirst, int expectedArms = 3)
        {
            var (info, body) = ReadBody(function);
            if (body[0] is not { } selector || Opcode(selector) != SelectValue)
            {
                throw new InvalidOperationException(IncorrectEntry);
            }

            object[] select = Children(selector, "Operands");
            int tail = Array.FindIndex(body, item => Opcode(item) == IsNonemptyList);
            int returning = Array.FindIndex(body, item => Opcode(item) == Return);
            if (select.Length != 3 + expectedArms * 2 || Value(select[1]) != info
                || select.Skip(4).Where((_, index) => index % 2 == 0).Select(Value).Distinct().Count() != 2
                || tail < 0 || returning < 0 || (tail < returning) != tailFirst
                || Value(Children(body[tail], "Operands")[0]) != info
                || Opcode(body[tail + 1]) != GetList
                || !Children(body[tail + 1], "Operands").Select(Value).SequenceEqual([2, 0, 2])
                || Opcode(body[tail + 2]) != TailLocalCall)
            {
                throw new InvalidOperationException(IncorrectEntry);
            }
        }

        void CheckTwoCallArms(string function, bool hasFrame)
        {
            var (info, body) = ReadBody(function);
            if (Opcode(body[0]) != SelectValue
                || Children(body[0], "Operands").Length != 7
                || Value(Children(body[0], "Operands")[1]) != info)
            {
                throw new InvalidOperationException(IncorrectEntry);
            }

            int tail = Array.FindIndex(body, item => Opcode(item) == IsNonemptyList);
            int calls = body.Count(item => Opcode(item) == LocalCall);
            var allocations = body.Where(item => Opcode(item) == Allocate).ToArray();
            if (tail < 0 || Value(Children(body[tail], "Operands")[0]) != info
                || Opcode(body[tail + 1]) != GetList || Opcode(body[tail + 2]) != TailLocalCall
                || (hasFrame ? calls != 1 || allocations.Length != 1
                    || !Children(allocations[0], "Operands").Select(Value).SequenceEqual([0, 2])
                    : calls != 0 || allocations.Length != 0))
            {
                throw new InvalidOperationException(IncorrectEntry);
            }
        }

        void CheckSingle(string function)
        {
            var (info, body) = ReadBody(function);
            if (Opcode(body[0]) != ExactEqualTest
                || Value(Children(body[0], "Operands")[0]) != info
                || Opcode(body[^1]) != Return)
            {
                throw new InvalidOperationException(IncorrectEntry);
            }
        }

        (BigInteger Failure, object[] Body) ReadBody(string function)
        {
            int index = Array.FindIndex(
                instructions,
                item => Opcode(item) == FunctionInfo && atoms[(int)Value(Children(item, "Operands")[1])] == function
            );
            object label = instructions.Take(index).Last(item => Opcode(item) == Label);
            var body = instructions.Skip(index + 1).SkipWhile(item => Opcode(item) == Label)
                .TakeWhile(item => Opcode(item) != FunctionInfo).ToArray();
            int last = Array.FindLastIndex(body, item => Opcode(item) is Return or TailLocalCall or TailLocalCallWithFrame);

            return (Value(Children(label, "Operands")[0]), body[..(last + 1)]);
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");

    private static object[] Children(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
