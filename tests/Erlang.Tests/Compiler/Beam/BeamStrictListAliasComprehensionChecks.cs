using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamStrictListAliasComprehensionChecks
{
    internal const string TestName = "compiler/beam/lifts-strict-tuple-aliases-and-independent-list-siblings";
    private const string Fixture = "beam_strict_list_alias_comprehension_probe.erl";
    private const string MissingHelper = "A strict tuple alias or its ordinary sibling did not produce its pinned list helper.";

    internal const string Source = """
        -module(beam_strict_list_alias_comprehension_probe).
        -export([strict/3, mixed/4, siblings/2, repeat/2]).

        strict(Rows, Target, Expected) ->
            [begin
                 Expected = observe(Value, Target),
                 {Whole, Expected}
             end || {row, Value} = Whole <:- Rows].

        mixed(run, First, Second, Target) ->
            A = [observe({strict, Whole, Value}, Target) || Whole = {row, Value} <:- First],
            B = [observe({ordinary, Value}, Target) || Value <- Second],
            {A, B};
        mixed(skip, _, _, _) ->
            skipped.

        siblings(First, Second) ->
            A = [Whole || Whole = {_, _} <- First],
            B = [{Value, Whole} || {_, Value} = Whole <- Second],
            {A, B}.

        repeat(First, Second) ->
            A = [Whole || Whole = {Value, Value} <- First],
            B = [Value || Value <- Second],
            {A, B}.

        observe(Value, Target) -> Target ! {seen, Value}, Value.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static void Run()
    {
        object code = Code(CompileFixture());
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        if (!HasRecursiveListResult("-strict/3-lc$^0/1-0-", 3)
            || !HasRecursiveListResult("-mixed/4-lc$^1/1-1-", 2)
            || !HasRecursiveListResult("-siblings/2-lc$^0/1-0-", 1)
            || !HasRecursiveListResult("-siblings/2-lc$^1/1-1-", 1)
            || HasRecursiveListResult("-repeat/2-lc$^0/1-0-", 1))
        {
            throw new InvalidOperationException(MissingHelper);
        }

        bool HasRecursiveListResult(string name, int arity)
        {
            int info = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && Operands(instruction) is [_, var atom, var width] && atoms[Value(atom)] == name && Value(width) == arity
            );
            if (info < 0 || Opcode(instructions[info + 1]) != Label)
            {
                return false;
            }

            int target = Value(Operands(instructions[info + 1])[0]);
            var body = instructions.Skip(info + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
            int recurse = Array.FindIndex(
                body,
                instruction => Opcode(instruction) == LocalCall && Value(Operands(instruction)[1]) == target
            );

            return recurse >= 0 && body.Skip(recurse + 1).Any(instruction => Opcode(instruction) == PutList);
        }
    }

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
