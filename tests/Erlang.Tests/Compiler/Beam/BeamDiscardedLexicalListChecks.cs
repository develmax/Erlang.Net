using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamCallConstructionProbe;

namespace Erlang.Tests;

internal static class BeamDiscardedLexicalListChecks
{
    internal const string TestName = "compiler/beam/outlines-discarded-list-item-blocks-with-lexical-bindings";
    private const string Failure = "A discarded list helper must keep its lexical effects before the next item.";

    internal static Task Run()
    {
        const string source = """
            -module(discarded_lexical_list).
            -export([walk/2]).
            walk(First, Second) ->
                _ = [begin
                         Prepared = probe:prepare(Item),
                         _ = if is_atom(Prepared) -> probe:atom(Prepared);
                                true -> probe:other(Prepared)
                             end,
                         probe:first(Prepared)
                     end || Item <- First],
                _ = [begin
                         Prepared = probe:prepare(Item),
                         _ = if is_atom(Prepared) -> probe:atom(Prepared);
                                true -> probe:other(Prepared)
                             end,
                         probe:second(Prepared)
                     end || Item <- Second],
                ok.
            """;
        var code = new BeamCallConstructionProbe(CodeGeneration.CompileBeamModule(source, "discarded_lexical_list.erl"));
        if (!HasEffectLoop("-walk/2-lc$^0/1-0-", "first") || !HasEffectLoop("-walk/2-lc$^1/1-1-", "second"))
        {
            throw new InvalidOperationException(Failure);
        }

        return Task.CompletedTask;

        bool HasEffectLoop(string helper, string finish)
        {
            object[] body = code.Body(helper);
            int prepare = code.CallIndex(
                body,
                "probe",
                "prepare",
                1
            );
            int finishIndex = code.CallIndex(
                body,
                "probe",
                finish,
                1
            );

            return prepare >= 0 && finishIndex > prepare && body.Any(instruction => Opcode(instruction) == IsAtom)
                && body.Skip(finishIndex + 1).Any(instruction => Opcode(instruction) is TailLocalCall or TailLocalCallWithFrame)
                && !body.Any(instruction => Opcode(instruction) == PutList);
        }
    }
}
