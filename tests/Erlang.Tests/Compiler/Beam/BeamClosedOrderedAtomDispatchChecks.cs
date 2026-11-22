using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamCallConstructionProbe;

namespace Erlang.Tests;

internal static class BeamClosedOrderedAtomDispatchChecks
{
    internal const string TestName = "compiler/beam/selects-a-real-default-for-a-closed-ordered-atom-entry";
    private const string Failure = "The ordered entry selector lost its proved default or its real open failure.";

    internal static Task Run()
    {
        const string source = """
            -module(closed_ordered_atom_dispatch).
            -export([drive/1, open/2]).
            drive(N) -> {closed(gamma,N),closed(alpha,N),closed(delta,N),closed(beta,N)}.
            closed(gamma,N) -> probe:finish(N,probe:gamma());
            closed(delta,N) -> probe:finish(N,probe:delta());
            closed(alpha,N) -> probe:finish(N,probe:alpha());
            closed(beta,N) -> probe:finish(N,probe:beta()).
            open(gamma,N) -> probe:finish(N,probe:gamma());
            open(delta,N) -> probe:finish(N,probe:delta());
            open(alpha,N) -> probe:finish(N,probe:alpha());
            open(beta,N) -> probe:finish(N,probe:beta()).
            """;
        var code = new BeamCallConstructionProbe(CodeGeneration.CompileBeamModule(source, "closed_ordered_atom_dispatch.erl"));
        object[] closed = code.Body("closed");
        object[] open = code.Body("open");
        object? selected = closed.SingleOrDefault(instruction => Opcode(instruction) == SelectValue);
        object? rejected = open.SingleOrDefault(instruction => Opcode(instruction) == SelectValue);
        if (selected is null || rejected is null || Value(Operand(selected, 2)) != 6 || Value(Operand(rejected, 2)) != 8
            || TargetsLabel(open, Value(Operand(rejected, 1))) >= 0)
        {
            throw new InvalidOperationException(Failure);
        }

        int fallback = TargetsLabel(closed, Value(Operand(selected, 1)));
        if (fallback < 0 || !closed.Skip(fallback + 1).TakeWhile(instruction => Opcode(instruction) != Label)
            .Any(instruction => Opcode(instruction) == ExternalCall && code.UsesImport(
                instruction,
                1,
                "probe",
                "alpha",
                0
            )))
        {
            throw new InvalidOperationException(Failure);
        }

        return Task.CompletedTask;
    }

    private static int TargetsLabel(object[] body, int label) => Array.FindIndex(body, instruction =>
        Opcode(instruction) == Label && Value(Operand(instruction, 0)) == label);
}
