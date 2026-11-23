using System.Collections;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamClosedEntryListPatternChecks
{
    internal const string TestName = "compiler/beam/compares-a-closed-list-head-pattern-as-one-literal";
    private const string WrongShape = "A closed entry list must use one exact literal test and keep the original fallback value.";
    private const string Source = """
        -module(beam_closed_entry_list).
        -export([f/1]).
        f([[]]) -> yes;
        f(Other) -> {other,Other}.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, "beam_closed_entry_list.erl");
        object code = Code(image);
        byte[] opcodes = ((IEnumerable)Property(code, "Instructions")).Cast<object>()
            .Select(instruction => (byte)Property(Property(instruction, "Definition"), "Opcode")).ToArray();
        if (!opcodes.Contains(ExactEqualTest) || opcodes.Contains(GetList))
        {
            throw new InvalidOperationException(WrongShape);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            if (!Term.A("yes").Equals(await module.Invoke(context, "f", [Term.List(Nil.Value)])))
            {
                throw new InvalidOperationException(WrongShape);
            }

            foreach (Term bad in new Term[] { Nil.Value, Term.List(Term.A("different")), new Cons(Nil.Value, Term.I(7)) })
            {
                if (!Term.Tuple(Term.A("other"), bad).Equals(await module.Invoke(context, "f", [bad])))
                {
                    throw new InvalidOperationException(WrongShape);
                }
            }

            return Nil.Value;
        });
    }
}
