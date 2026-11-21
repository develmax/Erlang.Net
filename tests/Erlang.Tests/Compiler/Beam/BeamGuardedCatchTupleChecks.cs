using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamGuardedCatchTupleChecks
{
    internal const string TestName = "compiler/beam/places-leading-catch-token-and-terminal-tuple-results";
    private const string SourceName = "beam_guarded_catch_tuple_probe.erl";
    private const string WrongTuple = "The returned nested tuple did not share X0 throughout construction.";
    private const string WrongCatch = "The guarded catch lost its entry argument or framed local tail.";
    private const string WrongSibling = "Independent tuple fields lost their distinct live results.";

    private const string Source = """
        -module(beam_guarded_catch_tuple_probe).
        -export([classify/1, convert/1, siblings/1]).

        classify(File) ->
            case filelib:is_file(File) of
                true -> {ok,{regular,File}};
                false -> {error,{missing,File}}
            end.

        convert(Value) when is_binary(Value) ->
            case catch decode(Value) of
                {'EXIT',_} -> {error,bad_binary};
                Converted -> convert(Converted)
            end;
        convert(Value) when is_list(Value) ->
            lists:reverse(Value).

        siblings(Value) ->
            Rounded = erlang:trunc(Value),
            {{first,Rounded},{second,Value}}.

        decode(Value) -> binary_to_term(Value).
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, SourceName));
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] classified = Body("classify").Where(i => Opcode(i) == PutTuple).ToArray();
        if (classified.Length != 4 || classified.Any(i => Value(Operands(i)[0]) != 0))
        {
            throw new InvalidOperationException(WrongTuple);
        }

        object[] converted = Body("convert");
        int allocation = Array.FindIndex(converted, i => Opcode(i) == Allocate);
        if (allocation < 0 || Operands(converted[allocation]) is not [var slots, var live]
            || Value(slots) != 1 || Value(live) != 1 || Opcode(converted[allocation + 1]) != CatchEnter
            || !converted.Any(i => Opcode(i) == TailLocalCallWithFrame && Value(Operands(i)[2]) == 1))
        {
            throw new InvalidOperationException(WrongCatch);
        }

        object[] siblings = Body("siblings").Where(i => Opcode(i) == PutTuple).ToArray();
        if (siblings.Length != 3 || Operands(siblings[^1]) is not [_, _, var first, var second]
            || ((string)Property(first, "Kind"), Value(first)) == ((string)Property(second, "Kind"), Value(second)))
        {
            throw new InvalidOperationException(WrongSibling);
        }

        return Task.CompletedTask;

        object[] Body(string name)
        {
            int start = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)Value(Operands(instruction)[1])] == name);

            return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
