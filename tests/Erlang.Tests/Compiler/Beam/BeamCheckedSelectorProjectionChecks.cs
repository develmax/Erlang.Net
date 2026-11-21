using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCheckedSelectorProjectionChecks
{
    internal const string TestName = "compiler/beam/checked-selector-projects-after-outgoing-list";
    private const string ShapeError = "The selector call did not retain the checked tuple and reuse the consumed argument slot.";
    private const string Source = """
        -module(beam_checked_selector_projection).
        -export([pick/2]).
        pick(Window, Input) ->
            {found, [[Entry]]} = service:read(Input),
            Candidate = service:resolve([Entry, first, second]),
            case service:exists(Candidate) of
                true -> Candidate;
                false ->
                    {ok, Fallback} = service:working(),
                    Message = [prefix, Candidate, suffix],
                    case service:decide(Window, Message) of
                        yes -> Candidate;
                        no -> Fallback
                    end
            end.
        """;

    internal static void Run()
    {
        string liveWindow = Source.Replace("no -> Fallback", "no -> {Window, Fallback}", StringComparison.Ordinal);
        string liveMessage = Source.Replace("no -> Fallback", "no -> {Message, Fallback}", StringComparison.Ordinal);
        string readField = Source.Replace("[prefix, Candidate, suffix]", "[prefix, Fallback, suffix]", StringComparison.Ordinal);
        if (!HasDeferredProjection(Source) || HasDeferredProjection(liveWindow)
            || HasDeferredProjection(liveMessage) || HasDeferredProjection(readField))
        {
            throw new InvalidOperationException(ShapeError);
        }
    }

    private static bool HasDeferredProjection(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, "beam_checked_selector_projection.erl"));
        object[] operations = ((IEnumerable)Property(code, "Instructions")).Cast<object>()
            .SkipWhile(operation => Opcode(operation) != FunctionInfo).Skip(1)
            .TakeWhile(operation => Opcode(operation) != FunctionInfo).ToArray();
        int selector = Array.FindIndex(operations, operation =>
            Opcode(operation) == ExternalCall && Value(Operands(operation)[0]) == 2);
        if (selector < 1 || selector + 2 >= operations.Length
            || Opcode(operations[selector + 1]) != GetTupleElement || Opcode(operations[selector + 2]) != SelectValue)
        {
            return false;
        }

        object? cons = operations.Take(selector).LastOrDefault(operation => Opcode(operation) == PutList);
        object[] tupleRead = Operands(operations[selector + 1]);
        object? kill = operations.Take(selector).LastOrDefault(operation => Opcode(operation) == InitYRegisters);
        int projection = Value(tupleRead[2]);

        return cons is not null && Kind(Operands(cons)[2]) == "x" && Value(Operands(cons)[2]) == 1
            && Kind(tupleRead[0]) == "y" && Kind(tupleRead[2]) == "y"
            && kill is not null && Operands(kill).Skip(1).Any(slot => Value(slot) == projection);
    }

    private static string Kind(object operand) => (string)Property(operand, "Kind");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object operation) => ((IEnumerable)Property(operation, "Operands")).Cast<object>().ToArray();

    private static byte Opcode(object operation) => (byte)Property(Property(operation, "Definition"), "Opcode");
}
