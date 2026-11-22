using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamSerialCaughtCallSelectionChecks
{
    internal const string TestName = "compiler/beam/reuses-one-tag-for-serial-caught-calls";
    private const string Fixture = "beam_serial_caught_call_selection_probe.erl";
    private const string UnexpectedLayout = "Serial caught calls must keep their argument roots until the guarded branch is selected.";

    private const string Source = """
        -module(beam_serial_caught_call_selection_probe).
        -export([run/4]).
        run(A,B,C,D) ->
            case try serial_probe:read(key_table,{protocol,A},2)
                 catch error:_ -> {failed,[]}
                 end of
                {Version,_} when Version > {8,4} ->
                    try serial_probe:invoke({local,A},{invoke,B,C,D},infinity)
                    catch _:Reason -> {failure,{exception,Reason}}
                    end;
                _ ->
                    serial_probe:fallback(A,B,C,D)
            end.
        """;

    internal static Task Run()
    {
        object[] body = Body(Source);
        object[] effect = Body(Source.Replace("{protocol,A}", "{protocol,serial_probe:key(A)}", StringComparison.Ordinal));
        object[] reordered = Body(Source.Replace("fallback(A,B,C,D)", "fallback(A,C,B,D)", StringComparison.Ordinal));
        byte[] prefix = [AllocateHeap, Move, Move, Move, Move, PutTuple, TryEnter];
        byte[] guard = [IsTuple, TestArity, GetTupleElement, IsLess];
        object[] tags = body.Where(instruction => Opcode(instruction) is TryEnter or TryEnd or TryCase).ToArray();
        object[] release = body.Where(instruction => Opcode(instruction) == InitYRegisters).ToArray();
        int join = Array.FindIndex(body, instruction => Opcode(instruction) == IsTuple);
        if (!body.Take(prefix.Length).Select(Opcode).SequenceEqual(prefix)
            || effect.Take(prefix.Length).Select(Opcode).SequenceEqual(prefix)
            || reordered.Take(prefix.Length).Select(Opcode).SequenceEqual(prefix)
            || join < 0 || !body.Skip(join).Take(guard.Length).Select(Opcode).SequenceEqual(guard)
            || tags.Length != 6 || tags.Any(instruction => Value(Operands(instruction)[0]) != 4)
            || Value(Operands(body[0])[0]) != 5
            || release.Length != 1 || !Operands(release[0]).Skip(1).Select(Value).SequenceEqual([0, 1, 2, 3])
            || body.Count(instruction => Opcode(instruction) == Raise) != 1
            || body.Count(instruction => Opcode(instruction) == TailExternalCallWithFrame) != 1)
        {
            throw new InvalidOperationException(UnexpectedLayout);
        }

        return Task.CompletedTask;
    }

    private static object[] Body(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, Fixture));
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int entry = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Operands(instruction) is [_, var name, var arity] && atoms[Value(name)] == "run" && Value(arity) == 4
        );
        if (entry < 0)
        {
            throw new InvalidOperationException(UnexpectedLayout);
        }

        return instructions.Skip(entry + 2).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
    }

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
