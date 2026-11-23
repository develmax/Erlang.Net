using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamPositionedRecursiveBitSegmentsChecks
{
    internal const string TestName = "compiler/beam/forwards-owned-position-through-recursive-segment-construction";
    private const string WrongSchedule = "The recursive cons builder lost its cursor or admitted an escaping consumer.";
    private const string WrongResult = "The recursive cons builder changed the bytes or its short suffix.";
    private const string Source = """
        -module(beam_positioned_recursive_bit_segments).
        -export([read/1, escape/0]).
        -record(wrapped, {value}).
        -record(piece, {value, width, marker = tag}).

        read(Bits) when is_bitstring(Bits) -> parts(Bits);
        read(Other) -> {invalid, Other}.

        parts(<<H, T/bitstring>>) ->
            [#piece{value=#wrapped{value=H}, width=#wrapped{value=8}} | parts(T)];
        parts(<<>>) -> [];
        parts(Bits) ->
            N = bit_size(Bits),
            <<I:N>> = Bits,
            [#piece{value=#wrapped{value=I}, width=#wrapped{value=N}}].

        escape() -> fun open/1.

        open(<<H, T/bitstring>>) ->
            [{item,{value,H},{width,8}} | open(T)];
        open(<<>>) -> [];
        open(Bits) ->
            N = bit_size(Bits),
            <<I:N>> = Bits,
            [{item,{value,I},{width,N}}].
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, "beam_positioned_recursive_bit_segments.erl");
        CheckLayout(image);
        var module = BeamModule.Prepare(image, new BeamDependencySelection([]));
        var selection = new RuntimeLibrarySelection([RuntimeModuleSelection.Managed(ErlangModuleNames.Module)]);
        await using var runtime = await ProcessRuntime.CreateAsync(
            selection,
            RuntimeNativePrimitives.InternalScalars413,
            CancellationToken.None
        );
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            if (!Nil.Value.Equals(await module.Invoke(context, "read", new BitString([])))
                || !Term.List(Piece(18, 8), Piece(5, 3)).Equals(
                    await module.Invoke(context, "read", new BitString([18, 0xa0], 11))
                )
                || !Term.Tuple(Term.A("invalid"), Term.A("other")).Equals(
                    await module.Invoke(context, "read", Term.A("other"))
                ))
            {
                throw new InvalidOperationException(WrongResult);
            }

            return Nil.Value;
        });
    }

    private static Term Piece(int value, int width) => Term.Tuple(
        Term.A("piece"),
        Term.Tuple(Term.A("wrapped"), Term.I(value)),
        Term.Tuple(Term.A("wrapped"), Term.I(width)),
        Term.A("tag")
    );

    private static void CheckLayout(byte[] image)
    {
        object code = Code(image);
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        byte[] loop = Body("parts").Select(Opcode).ToArray();
        byte[] escaped = Body("open").Select(Opcode).ToArray();
        if (loop.Count(operation => operation == StartBinaryMatchKnown) != 1
            || loop.Count(operation => operation == GetBinaryPosition) != 1
            || loop.Count(operation => operation == SetBinaryPosition) != 2
            || loop.Count(operation => operation == GetBinaryTail) != 1
            || loop.Count(operation => operation == LocalCall) != 1
            || loop.Count(operation => operation == Allocate) != 1
            || escaped.Contains(StartBinaryMatchKnown))
        {
            throw new InvalidOperationException(WrongSchedule);
        }

        object[] Body(string function)
        {
            int entry = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)(BigInteger)Property(Items(instruction, "Operands")[1], "Value")] == function
            );

            return entry < 0
                ? throw new InvalidOperationException(WrongSchedule)
                : instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
