using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamOverwrittenOutgoingRootsChecks
{
    internal const string TestName = "compiler/beam/clears-a-consumed-slot-before-its-checked-result-reuses-it";
    private const string Fixture = "beam_overwritten_outgoing_root.erl";
    private const string OldRootRetained = "The dead outgoing argument stayed rooted when the call returned.";
    private const string LiveRootCleared = "The checked call still needs the preserved original argument.";

    private const string Source = """
        -module(beam_overwritten_outgoing_root).
        -export([step/1, kept/1, produce/2]).
        step(N) ->
            {Value, Next} = beam_overwritten_outgoing_root:produce(N, initial()),
            _ = erlang:put(seed, Next),
            Value.
        kept(N) ->
            {Value, Next} = beam_overwritten_outgoing_root:produce(N, initial()),
            _ = erlang:put(seed, Next),
            {N, Value}.
        initial() ->
            erlang:get(seed).
        produce(probe_error, _) -> malformed;
        produce(N, Y) -> {N,{next,Y}}.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        if (!HasOutgoingRootClear("step"))
        {
            throw new InvalidOperationException(OldRootRetained);
        }

        if (HasOutgoingRootClear("kept"))
        {
            throw new InvalidOperationException(LiveRootCleared);
        }

        return Task.CompletedTask;

        bool HasOutgoingRootClear(string name)
        {
            int entry = Array.FindIndex(
                instructions,
                item => Opcode(item) == FunctionInfo && atoms[Value(Items(item, "Operands").ElementAt(1))] == name
            );
            byte[] body = instructions.Skip(entry + 1).TakeWhile(item => Opcode(item) != FunctionInfo)
                .Select(Opcode).Where(opcode => opcode != Line).ToArray();
            for (int index = 0; index + 2 < body.Length; index++)
            {
                if (body[index] == Move && body[index + 1] == InitYRegisters && body[index + 2] == ExternalCall)
                {
                    return true;
                }
            }

            return false;
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
