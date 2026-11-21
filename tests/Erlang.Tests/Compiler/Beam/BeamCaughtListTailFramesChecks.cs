using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCaughtListTailFramesChecks
{
    internal const string TestName = "compiler/beam/keeps-only-the-terminal-list-roots-across-catch";
    internal const string Fixture = "beam_caught_list_tail.erl";
    private const string OversizedCatchFrame = "The caught list continuation saved dead call arguments.";

    internal const string Source = """
        -module(beam_caught_list_tail).
        -export([items/1, complete/1]).

        items(Items) -> collect(Items, []).
        complete(Value) -> {completed, Value}.

        collect([], Acc) -> lists:reverse(Acc);
        collect([{invoke, {M, F, A}} | Tail], Acc)
            when is_atom(M) andalso is_atom(F) andalso is_list(A) ->
            collect(Tail, [(catch apply(M, F, A)) | Acc]);
        collect([Item | Tail], Acc) -> collect(Tail, [Item | Acc]).
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static Task Run()
    {
        object code = Code(CompileFixture());
        object[] instructions = Items(code, "Instructions").ToArray();
        object[] frames = instructions.Where(instruction => Opcode(instruction) == Allocate).ToArray();
        if (frames is not [var frame] || Value(Items(frame, "Operands").First()) != 3
            || !instructions.Any(instruction => Opcode(instruction) == CatchEnter))
        {
            throw new InvalidOperationException(OversizedCatchFrame);
        }

        return Task.CompletedTask;
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
