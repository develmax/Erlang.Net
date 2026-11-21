using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamNestedSingletonConsCallsChecks
{
    internal const string TestName = "compiler/beam/checked-nested-list-payload-feeds-cons-in-registers";
    private const string ShapeError = "The nested singleton match did not retain the checked payload in X registers.";
    internal const string Source = """
        -module(beam_checked_nested_cons).
        -export([resolve/1]).
        resolve(Input) ->
            {found, [[Entry]]} = service:read(Input),
            Candidate = service:resolve([Entry, first, second]),
            case service:choose(Candidate) of
                true -> Candidate;
                false -> Input
            end.
        """;

    internal static void Run()
    {
        string livePayload = Source.Replace("false -> Input", "false -> Entry", StringComparison.Ordinal);
        string boundPayload = Source.Replace("[[Entry]]", "[[Input]]", StringComparison.Ordinal)
            .Replace("[Entry, first, second]", "[Input, first, second]", StringComparison.Ordinal);
        string openListTail = Source.Replace("[[Entry]]", "[[Entry | _]]", StringComparison.Ordinal);
        if (!HasNestedRegisterChecks(Source) || HasNestedRegisterChecks(livePayload)
            || HasNestedRegisterChecks(boundPayload) || HasNestedRegisterChecks(openListTail))
        {
            throw new InvalidOperationException(ShapeError);
        }
    }

    private static bool HasNestedRegisterChecks(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, "beam_checked_nested_cons.erl"));
        object[] operations = ((IEnumerable)Property(code, "Instructions")).Cast<object>()
            .SkipWhile(operation => Opcode(operation) != FunctionInfo).Skip(1)
            .TakeWhile(operation => Opcode(operation) != FunctionInfo).ToArray();
        int checkedTuple = Array.FindIndex(operations, operation => Opcode(operation) == IsTaggedTuple);
        if (checkedTuple < 0)
        {
            return false;
        }

        var checkedPrefix = operations.Skip(checkedTuple + 1).Take(9).ToArray();
        if (!checkedPrefix.Select(Opcode).SequenceEqual(new byte[]
            {
                GetTupleElement,
                IsNonemptyList,
                GetList,
                IsNonemptyList,
                GetList,
                IsNil,
                IsNil,
                TestHeap,
                PutList
            }))
        {
            return false;
        }

        object[] projection = Operands(checkedPrefix[0]);
        object[] heap = Operands(checkedPrefix[7]);
        object[] cons = Operands(checkedPrefix[8]);

        return (string)Property(projection[2], "Kind") == "x" && Value(projection[2]) == 1
            && Value(heap[1]) == 4 && (string)Property(cons[0], "Kind") == "x" && Value(cons[0]) == 3
            && operations.Count(operation => Opcode(operation) == BadMatch) == 1;
    }

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object operation) => ((IEnumerable)Property(operation, "Operands")).Cast<object>().ToArray();

    private static byte Opcode(object operation) => (byte)Property(Property(operation, "Definition"), "Opcode");
}
