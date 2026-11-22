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

        AssertDefiniteReturnRoots();

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

    private static void AssertDefiniteReturnRoots()
    {
        const byte zero = 0x00;
        const byte one = 0x10;
        const byte two = 0x20;
        const byte three = 0x30;
        const byte failed = 0x35;
        const byte x0 = 0x03;
        const byte y0 = 0x04;
        const byte nil = 0x02;
        const byte atom = 0x12;

        byte[] original = MakeImage(cleared: false);
        byte[] expected = MakeImage(cleared: true);
        if (Rewrite(original) is not { } actual || !actual.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(OldRootRetained);
        }

        if (Rewrite(MakeImage(cleared: false, readsAfterCall: true)) is not null
            || Rewrite(MakeImage(cleared: false, readsOnFailure: true)) is not null || Rewrite(expected) is not null)
        {
            throw new InvalidOperationException(LiveRootCleared);
        }

        static byte[] MakeImage(bool cleared, bool readsAfterCall = false, bool readsOnFailure = false)
        {
            byte[] initialization = cleared ? [InitYRegisters, ExtendedListOperand, one, y0] : [];
            byte[] fallback = readsOnFailure ? [Move, y0, x0] : [];

            return
            [
                Label, one, FunctionInfo, atom, atom, one, Label, two,
                Allocate, one, one, Move, x0, y0, ExternalCall, one, zero,
                Move, y0, x0, .. initialization, Line, one, ExternalCall, one, zero,
                ExactEqualTest, failed, x0, atom, Move, readsAfterCall ? y0 : nil, x0,
                Deallocate, one, Return, Label, three, .. fallback, Deallocate, one, Return, EndCode
            ];
        }

        static byte[]? Rewrite(byte[] code)
        {
            var image = new ReadOnlyMemory<byte>(code);
            object header = New(
                "BeamCodeHeader",
                0,
                (int)InitYRegisters,
                4,
                1,
                image
            );
            object body = Call(
                Type("BeamInstructionReader"),
                "Read",
                header,
                2,
                0,
                null,
                null
            );
            var compiler = new BeamModuleCompiler("beam_terminal_outgoing_root");

            return (byte[]?)Call(
                compiler,
                "ClearOverwrittenOutgoingRoots",
                image,
                body
            );
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
