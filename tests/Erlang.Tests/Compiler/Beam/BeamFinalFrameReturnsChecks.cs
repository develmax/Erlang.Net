using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamFinalFrameReturnsChecks
{
    internal const string TestName = "compiler/beam/shared-frame-returns-after-branch-allocations";
    internal const string RootTestName = "compiler/beam/shared-frame-roots-and-cold-exit-lines";
    private const string ShapeError = "The repeated terminal branch returns did not share the frame slots.";
    private const string Source = """
        -module(beam_shared_frame_return).
        -export([pick/1]).
        pick(Owner) ->
            {found, [[Seed]]} = service:read(initial),
            Primary = service:resolve([Seed, a, b]),
            case service:exists(Primary) of
                true -> Primary;
                false ->
                    Alternate = service:resolve(service:base(seed, option), target),
                    case service:exists(Alternate) of
                        true -> Alternate;
                        false ->
                            {ready, Fallback} = service:fallback(),
                            Request = [request, Alternate, confirmation],
                            case service:confirm(Owner, Request) of
                                accepted ->
                                    Parent = service:parent(Alternate),
                                    case service:exists(Parent) of
                                        true ->
                                            case service:effect(Alternate) of
                                                ok -> Alternate;
                                                _ -> Fallback
                                            end;
                                        false ->
                                            case service:effect(Parent) of
                                                ok ->
                                                    case service:effect(Alternate) of
                                                        ok -> Alternate;
                                                        _ -> Fallback
                                                    end;
                                                _ -> Fallback
                                            end
                                    end;
                                declined -> Fallback
                            end
                    end
            end.
        """;

    internal static void Run()
    {
        string differing = Source.Replace("ok -> Alternate", "ok -> {paired, Alternate}", StringComparison.Ordinal);
        if (!HasTwoSlotReturns(Source) || HasTwoSlotReturns(differing))
        {
            throw new InvalidOperationException(ShapeError);
        }
    }

    private static bool HasTwoSlotReturns(string source)
    {
        object[] operations = ReadOperations(source);
        int[] exits = Enumerable.Range(0, operations.Length).Where(index => Opcode(operations[index]) == Return).ToArray();
        if (exits.Length != 2)
        {
            return false;
        }

        int[] slots = exits.Where(index => index >= 2 && Opcode(operations[index - 1]) == Deallocate
                && Opcode(operations[index - 2]) == Move)
            .Select(index => Operands(operations[index - 2])[0])
            .Where(operand => (string)Property(operand, "Kind") == "y")
            .Select(operand => (int)(BigInteger)Property(operand, "Value")).ToArray();

        return slots.Length == 2 && slots[0] != slots[1];
    }

    internal static void RunFrameRootAndColdExits()
    {
        const string error = "The shared terminal frame did not clear branch roots or retain cold exit locations.";
        string differentReturn = Source.Replace("ok -> Alternate", "ok -> {paired, Alternate}", StringComparison.Ordinal);
        if (!HasColdFrameLayout(Source) || HasColdFrameLayout(differentReturn))
        {
            throw new InvalidOperationException(error);
        }
    }

    private static bool HasColdFrameLayout(string source)
    {
        object[] operations = ReadOperations(source);
        int[] cold = Enumerable.Range(0, operations.Length)
            .Where(index => Opcode(operations[index]) is CaseEnd or BadMatch).ToArray();
        int lastReturn = Array.FindLastIndex(operations, operation => Opcode(operation) == Return);
        object saved = operations.First(operation => Opcode(operation) == Move
            && Operands(operation) is [_, var target] && (string)Property(target, "Kind") == "y");

        return (int)(BigInteger)Property(Operands(saved)[1], "Value") == 2
            && operations.Count(operation => Opcode(operation) == InitYRegisters) == 6
            && cold.Length == 6
            && cold.All(index => index > lastReturn && Opcode(operations[index - 1]) == Line);
    }

    private static object[] ReadOperations(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, "beam_shared_frame_return.erl"));

        return ((IEnumerable)Property(code, "Instructions")).Cast<object>()
            .SkipWhile(operation => Opcode(operation) != FunctionInfo).Skip(1)
            .TakeWhile(operation => Opcode(operation) != FunctionInfo).ToArray();
    }

    private static object[] Operands(object operation) => ((IEnumerable)Property(operation, "Operands")).Cast<object>().ToArray();

    private static byte Opcode(object operation) => (byte)Property(Property(operation, "Definition"), "Opcode");
}
