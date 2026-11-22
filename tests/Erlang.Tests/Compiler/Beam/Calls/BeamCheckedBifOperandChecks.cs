using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamCallConstructionProbe;

namespace Erlang.Tests;

internal static class BeamCheckedBifOperandChecks
{
    internal const string TestName = "compiler/beam/plans-checked-bif-tail-operands-with-live-inputs";
    private const string Failure = "A checked BIF operand must respect its outgoing destination and live source registers.";

    internal static Task Run()
    {
        const string source = """
            -module(checked_bif_operand).
            -compile({no_auto_import,[length/1]}).
            -export([duplicate/2, keep/1, shadow/2]).
            duplicate(List, Term) -> lists:duplicate(erlang:length(List), Term).
            keep(List) -> lists:nth(erlang:length(List), List).
            shadow(List, Term) -> lists:duplicate(length(List), Term).
            length(_) -> 1.
            """;
        var code = new BeamCallConstructionProbe(CodeGeneration.CompileBeamModule(source, "checked_bif_operand.erl"));
        object[] duplicate = WithoutLines(code.Body("duplicate"));
        object[] keep = WithoutLines(code.Body("keep"));
        object[] shadow = WithoutLines(code.Body("shadow"));
        var direct = duplicate.Where(instruction => Opcode(instruction) is GcBifOne or Move or TailExternalCall).ToArray();
        var preserved = keep.Where(instruction => Opcode(instruction) == GcBifOne).ToArray();
        if (direct is not [var bif, var transfer] || Opcode(bif) != GcBifOne || Opcode(transfer) != TailExternalCall
            || duplicate.Any(instruction => Opcode(instruction) is Allocate or AllocateHeap)
            || !code.UsesImport(
                bif,
                2,
                "erlang",
                "length",
                1
            )
            || !code.UsesImport(
                transfer,
                1,
                "lists",
                "duplicate",
                2
            )
            || Value(Operand(bif, 0)) != 0 || Value(Operand(bif, 1)) != 2
            || !IsX(Operand(bif, 3), 0) || !IsX(Operand(bif, 4), 0)
            || preserved is not [var keepsSource] || Value(Operand(keepsSource, 1)) != 1
            || !IsX(Operand(keepsSource, 3), 0) || IsX(Operand(keepsSource, 4), 0)
            || shadow.Length == 0 || shadow.Any(instruction => Opcode(instruction) == GcBifOne))
        {
            throw new InvalidOperationException(Failure);
        }

        return Task.CompletedTask;
    }
}
