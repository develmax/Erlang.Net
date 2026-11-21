using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamColdExactMatchFailureChecks
{
    internal const string TestName = "compiler/beam/places-exact-check-failures-in-cold-continuations";
    private const string Fixture = "beam_cold_exact_match_failure_probe.erl";
    private const string UnexpectedLayout = "Expected exact check failures at their terminal continuations.";

    internal const string Source = """
        -module(beam_cold_exact_match_failure_probe).
        -export([run/2, protected/1, shape/1, step/2, finish/0]).

        run(First, Second) ->
            case ?MODULE:shape(First) of
                {ready, Response} ->
                    accepted = ?MODULE:step(first, Response),
                    case ?MODULE:shape(Second) of
                        {ready, Value} ->
                            continued = ?MODULE:step(second, Value),
                            ?MODULE:finish();
                        Error ->
                            {second_error, Error}
                    end;
                Error ->
                    {first_error, Error}
            end.

        protected(Value) ->
            try accepted = ?MODULE:step(protected, Value)
            catch error:Error -> {caught, Error}
            end.

        shape(Value) -> self() ! {shape, Value}, Value.
        step(Name, Value) -> self() ! {step, Name, Value}, Value.
        finish() -> self() ! finished, done.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static void Run()
    {
        object code = Code(CompileFixture());
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] checks = FunctionInstructions("run");
        int[] exactFailures = checks.Where(instruction => Opcode(instruction) == ExactEqualTest)
            .Select(instruction => Value(Items(instruction, "Operands").First())).ToArray();
        object[] suffix = checks.TakeLast(6).ToArray();
        if (suffix.Length != 6 || Opcode(suffix[0]) != Label || Opcode(suffix[1]) != Line
            || Opcode(suffix[2]) != BadMatch || Opcode(suffix[3]) != Label || Opcode(suffix[4]) != Line
            || Opcode(suffix[5]) != BadMatch || checks.Count(instruction => Opcode(instruction) == BadMatch) != 2
            || !exactFailures.Contains(Value(Items(suffix[0], "Operands").First()))
            || !exactFailures.Contains(Value(Items(suffix[3], "Operands").First()))
            || Value(Items(suffix[4], "Operands").First()) >= Value(Items(suffix[1], "Operands").First())
            || !FunctionInstructions("protected").Any(instruction => Opcode(instruction) == TryEnter))
        {
            throw new InvalidOperationException(UnexpectedLayout);
        }

        object[] FunctionInstructions(string name)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Items(instruction, "Operands").ElementAt(1))] == name
            );
            object[] segment = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
            int count = segment.Length;
            while (count > 0 && Opcode(segment[count - 1]) is Line or Label)
            {
                count--;
            }

            return segment[..count];
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
