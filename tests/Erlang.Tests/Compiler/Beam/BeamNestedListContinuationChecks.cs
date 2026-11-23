using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamNestedListContinuationChecks
{
    internal const string TestName = "compiler/beam/lifts-ordinary-second-list-generators-with-owned-continuations";
    private const string Fixture = "beam_nested_list_continuation_probe.erl";
    private const string MissingHelper = "The nested generator did not preserve its pinned continuations.";

    internal const string Source = """
        -module(beam_nested_list_continuation_probe).
        -export([independent/3, correlated/2, from_prefix/2]).

        independent(Outer, Source, Target) ->
            [observe({X, Y}, Target) || X <- Outer, Y <- produce(Source, Target)].

        correlated(Rows, Target) ->
            [observe(Y, Target) || Row <- Rows, {item, Y} <- from_row(Row, Target)].

        from_prefix(Outer, Target) ->
            Suffixes = [one, two],
            case is_list(Outer) of
                true -> [observe({X, Y}, Target) || X <- Outer, Y <- Suffixes];
                false -> skipped
            end.

        produce({fail, Reason}, Target) ->
            Target ! {source, Reason},
            erlang:error({source_failed, Reason});
        produce(Source, Target) ->
            Target ! {source, Source},
            Source.

        from_row({row, Key, Values}, Target) ->
            Target ! {row, Key},
            Values.

        observe(Value, Target) ->
            Target ! {value, Value},
            Value.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static void Run()
    {
        object code = Code(CompileFixture());
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        if (!HasContinuations("independent/3", 3, 5)
            || !HasContinuations("correlated/2", 2, 3)
            || !HasContinuations("from_prefix/2", 2, 4))
        {
            throw new InvalidOperationException(MissingHelper);
        }

        bool HasContinuations(string name, int outerWidth, int innerWidth)
        {
            int outer = Find("-" + name + "-lc$^0/1-0-", outerWidth);
            int inner = Find("-" + name + "-lc$^1/1-1-", innerWidth);
            if (outer < 0 || inner < 0 || Opcode(instructions[outer + 1]) != Label || Opcode(instructions[inner + 1]) != Label)
            {
                return false;
            }

            int outerEntry = Value(Operands(instructions[outer + 1])[0]);
            int innerEntry = Value(Operands(instructions[inner + 1])[0]);
            var outerBody = instructions.Skip(outer + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo);
            var innerBody = instructions.Skip(inner + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();

            return outerBody.Any(instruction => Calls(instruction, innerEntry, innerWidth))
                && innerBody.Any(instruction => Calls(instruction, outerEntry, outerWidth))
                && innerBody.Any(instruction => Calls(instruction, innerEntry, innerWidth))
                && innerBody.Any(instruction => Opcode(instruction) == PutList);
        }

        int Find(string name, int width) => Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Operands(instruction) is [_, var atom, var arity] && atoms[Value(atom)] == name && Value(arity) == width
        );
    }

    private static bool Calls(object instruction, int entry, int width) =>
        Opcode(instruction) is LocalCall or TailLocalCall or TailLocalCallWithFrame
        && Operands(instruction) is [var arity, var target, ..]
        && Value(arity) == width && Value(target) == entry;

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
