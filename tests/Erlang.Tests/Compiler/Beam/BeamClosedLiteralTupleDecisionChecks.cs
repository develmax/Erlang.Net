using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamClosedLiteralTupleDecisionChecks
{
    internal const string TestName = "compiler/beam/branches-on-nested-literal-tuple-fields-without-a-frame";
    private const string Fixture = "beam_closed_literal_tuple_decision_probe.erl";
    private const string IncorrectInstructions = "The emitted function has the wrong register or branch layout.";
    private const string IncorrectResults = "The emitted function returned unexpected values.";

    private const string Source = """
        -module(beam_closed_literal_tuple_decision_probe).
        -export([value/1]).

        value(Key) -> (factory(Key))().
        factory({1,2,7,3}) -> fun() -> a end;
        factory({1,2,7,4}) -> fun() -> b end;
        factory({1,3,1,5}) -> fun() -> c end;
        factory({2,4}) -> fun() -> d end;
        factory(_) -> fun() -> fallback end.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
            && Items(instruction, "Operands").ToArray() is [_, var atom, _] && atoms[Value(atom)] == "factory"
        );
        if (start < 0)
        {
            throw new InvalidOperationException(IncorrectInstructions);
        }

        object[] body = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        if (body.Any(value => Opcode(value) == Allocate)
            || body.Count(value => Opcode(value) == SelectTupleArity) != 1
            || body.Count(value => Opcode(value) == SelectValue) != 2)
        {
            throw new InvalidOperationException(IncorrectInstructions);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            foreach (var (key, expected) in new (Term, Term)[]
                {
                    (Term.Tuple(
                        Term.I(1),
                        Term.I(2),
                        Term.I(7),
                        Term.I(4)
                    ), Term.A("b")),
                    (Term.Tuple(
                        Term.I(1),
                        Term.I(3),
                        Term.I(1),
                        Term.I(5)
                    ), Term.A("c")),
                    (Term.Tuple(Term.I(2), Term.I(4)), Term.A("d")),
                    (Term.Tuple(
                        Term.I(1),
                        Term.I(3),
                        Term.I(1),
                        Term.I(9)
                    ), Term.A("fallback")),
                    (Term.A("atom"), Term.A("fallback"))
                })
            {
                if (!expected.Equals(await module.Invoke(context, "value", key)))
                {
                    throw new InvalidOperationException(IncorrectResults);
                }
            }

            return Nil.Value;
        });
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string member) => ((IEnumerable)Property(owner, member)).Cast<object>();
}
