using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamSingletonTupleListReturnChecks
{
    internal const string TestName = "compiler/beam/keeps-singleton-tuple-list-returns-unframed";
    private const string Fixture = "beam_singleton_tuple_list_return.erl";
    private const string BadLayout = "A list identity arm borrowed the fallback call frame.";
    private const string BadValue = "A list fast path changed the original fallback input or repeated binding.";

    private const string Source = """
        -module(beam_singleton_tuple_list_return).
        -export([order/1, repeated/1]).

        order([]) -> [];
        order([{_,_}]=Same) -> Same;
        order(Rows) -> finish(mark, prepare(Rows, [])).

        repeated([]) -> [];
        repeated([{Key,Key}]=Same) -> Same;
        repeated(Rows) -> finish(mark, prepare(Rows, [])).

        prepare(Rows, Tail) -> {received,Rows,Tail}.
        finish(Marker, Prepared) -> {Marker,Prepared}.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        object code = Code(image);
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands") is [_, var name, var arity]
                && atoms[Value(name)] == "order" && Value(arity) == 1
        );
        if (start < 0)
        {
            throw new InvalidOperationException(BadLayout);
        }

        object[] body = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        int allocation = Array.FindIndex(body, instruction => Opcode(instruction) == Allocate);
        if (allocation < 0 || body.Take(allocation).Count(instruction => Opcode(instruction) == Return) != 2
            || body.Take(allocation).Count(instruction => Opcode(instruction) == IsNil) != 2
            || Items(body[allocation], "Operands") is not [var frame, var live] || Value(frame) != 0 || Value(live) != 1)
        {
            throw new InvalidOperationException(BadLayout);
        }

        BeamModule module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term first = Term.A("first");
            Term second = Term.A("second");
            Term pair = Term.Tuple(first, second);
            Term equal = Term.List(Term.Tuple(first, first));
            Term one = Term.List(pair);
            Term two = Term.List(pair, pair);
            Term badArity = Term.List(Term.Tuple(first));
            Term improper = new Cons(pair, first);
            await Check("order", Nil.Value, Nil.Value);
            await Check("order", one, one);
            await Check("order", two, Fallback(two));
            await Check("order", badArity, Fallback(badArity));
            await Check("order", improper, Fallback(improper));
            await Check("order", first, Fallback(first));
            await Check("repeated", equal, equal);
            await Check("repeated", one, Fallback(one));

            return Nil.Value;

            async Task Check(string function, Term input, Term expected)
            {
                if (!expected.Equals(await module.Invoke(
                    context,
                    function,
                    input
                )))
                {
                    throw new InvalidOperationException(BadValue);
                }
            }
        });
    }

    private static Term Fallback(Term input) =>
        Term.Tuple(Term.A("mark"), Term.Tuple(Term.A("received"), input, Nil.Value));

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
