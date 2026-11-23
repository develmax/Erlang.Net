using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamGuardedCallResultTailsChecks
{
    internal const string TestName = "compiler/beam/guarded-call-result-tails-keep-projections-in-x-registers";
    private const string ShapeError = "The guarded call result did not preserve the zero-width frame and the guard-failure join.";
    internal const string Source = """
        -module(beam_guarded_result).
        -export([check/2]).
        -record(result, {address, index, first, second}).

        check(0, Options) ->
            case probe:options([{kind,0} | Options], probe) of
                {error, Reason} -> erlang:exit(Reason);
                {ok, #result{address = Address, index = 0, first = First, second = Second}}
                  when tuple_size(Address) =:= 2, element(1, Address) =:= expected;
                       Address =:= default ->
                    probe:open(First, Address, 0, Second, mode, family, type, source);
                {ok, _} -> erlang:exit(rejected)
            end.
        """;

    internal static void Run()
    {
        string retained = Source.Replace("family, type, source", "family, type, Options", StringComparison.Ordinal);
        string differentGuard = Source.Replace("Address =:= default", "Options =:= default", StringComparison.Ordinal);
        if (!HasRegisterOnlyGuardedTail(Source) || HasRegisterOnlyGuardedTail(retained)
            || HasRegisterOnlyGuardedTail(differentGuard))
        {
            throw new InvalidOperationException(ShapeError);
        }
    }

    private static bool HasRegisterOnlyGuardedTail(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, "beam_guarded_result.erl"));
        var operations = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        if (operations.Any(operation => Operands(operation).Any(operand => Kind(operand) == BeamBinaryHostNames.Y)))
        {
            return false;
        }

        bool zeroFrameWithCons = operations.Any(operation =>
            Opcode(operation) == AllocateHeap && Operands(operation) is [var frame, var heap, var live]
            && Value(frame) == 0 && Value(heap) == 2 && Value(live) == 2);
        int tupleSize = Array.FindIndex(operations, operation =>
            Opcode(operation) == BifOne && Value(Operands(operation)[0]) > 0);
        if (!zeroFrameWithCons || tupleSize < 0 || tupleSize + 4 >= operations.Length)
        {
            return false;
        }

        object[] size = Operands(operations[tupleSize]);
        object[] element = Operands(operations[tupleSize + 2]);

        return Opcode(operations[tupleSize + 1]) == BifTwo
            && Opcode(operations[tupleSize + 2]) == BifTwo
            && Opcode(operations[tupleSize + 3]) == BifTwo
            && Opcode(operations[tupleSize + 4]) == BifTwo
            && Value(size[0]) == Value(element[0]);
    }

    private static string Kind(object operand) => (string)Property(operand, "Kind");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object operation) => ((IEnumerable)Property(operation, "Operands")).Cast<object>().ToArray();

    private static byte Opcode(object operation) => (byte)Property(Property(operation, "Definition"), "Opcode");
}
