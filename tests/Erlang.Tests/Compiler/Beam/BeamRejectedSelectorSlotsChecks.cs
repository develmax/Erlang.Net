using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamRejectedSelectorSlotsChecks
{
    internal const string TestName = "compiler/beam/retire-rejected-selector-root-before-replacement-call";
    private const string ShapeError = "The replacement candidate did not reuse the rejected selector's root.";
    private const string Source = """
        -module(beam_rejected_selector_slot).
        -export([pick/1]).
        pick(Input) ->
            {found, [[Entry]]} = service:read(Input),
            Existing = service:resolve([Entry, first, second]),
            case service:exists(Existing) of
                true -> Existing;
                false ->
                    Alternative = service:resolve(service:base(local, config), dir),
                    case service:exists(Alternative) of
                        true -> Alternative;
                        false -> Input
                    end
            end.
        """;

    internal static void Run()
    {
        string liveOriginal = Source.Replace("false -> Input", "false -> {Existing, Input}", StringComparison.Ordinal);
        string differentProbe = Source.Replace("service:exists(Alternative)", "service:available(Alternative)", StringComparison.Ordinal);
        if (!ReusesCandidateRoot(Source) || ReusesCandidateRoot(liveOriginal) || ReusesCandidateRoot(differentProbe))
        {
            throw new InvalidOperationException(ShapeError);
        }
    }

    private static bool ReusesCandidateRoot(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, "beam_rejected_selector_slot.erl"));
        object[] operations = ((IEnumerable)Property(code, "Instructions")).Cast<object>()
            .SkipWhile(operation => Opcode(operation) != FunctionInfo).Skip(1)
            .TakeWhile(operation => Opcode(operation) != FunctionInfo).ToArray();
        int first = Array.FindIndex(operations, operation => Opcode(operation) == SelectValue);
        int second = Array.FindLastIndex(operations, operation => Opcode(operation) == SelectValue);
        if (first <= 0 || second <= first)
        {
            return false;
        }

        object? original = operations.Take(first).LastOrDefault(StoresY);
        object? replacement = operations.Take(second).Skip(first + 1).LastOrDefault(StoresY);
        if (original is null || replacement is null)
        {
            return false;
        }

        int slot = Value(Operands(original)[1]);

        return Value(Operands(replacement)[1]) == slot
            && operations.Skip(first + 1).Take(second - first - 1).Any(operation =>
                Opcode(operation) == InitYRegisters && Operands(operation).Skip(1).Any(operand => Value(operand) == slot));
    }

    private static bool StoresY(object operation) => Opcode(operation) == Move && Operands(operation) is [_, var target]
        && (string)Property(target, "Kind") == "y";

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object operation) => ((IEnumerable)Property(operation, "Operands")).Cast<object>().ToArray();

    private static byte Opcode(object operation) => (byte)Property(Property(operation, "Definition"), "Opcode");
}
