using System.Collections;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamClosedCatchArgumentRootsChecks
{
    internal const string TestName = "compiler/beam/clears-consumed-arguments-inside-a-closed-catch";
    internal const string Fixture = "beam_closed_catch_argument_roots.erl";
    private const string RetainedArgument = "Protected call arguments stayed rooted after their final load.";

    internal const string Source = """
        -module(beam_closed_catch_argument_roots).
        -export([send/3]).

        send(false, _, _) -> ignored;
        send(_, Label, Values) ->
            Prepared = prepare(Values, []),
            (catch deliver(Label ++ "!", Prepared)).

        prepare(Values, Empty) -> lists:reverse(Values, Empty).
        deliver(Label, Values) -> {Label, Values}.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static Task Run()
    {
        object code = Code(CompileFixture());
        var instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>();
        byte[] operations = instructions.Select(item => (byte)Property(Property(item, "Definition"), "Opcode")).ToArray();
        int enter = Array.IndexOf(operations, CatchEnter);
        if (enter < 0 || operations.Skip(enter + 1).TakeWhile(opcode => opcode != CatchEnd)
            .Count(opcode => opcode == InitYRegisters) != 2)
        {
            throw new InvalidOperationException(RetainedArgument);
        }

        return Task.CompletedTask;
    }
}
