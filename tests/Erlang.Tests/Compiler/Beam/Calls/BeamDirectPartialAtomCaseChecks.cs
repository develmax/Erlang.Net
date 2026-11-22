using System.Collections;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamDirectPartialAtomCaseChecks
{
    internal const string TestName = "compiler/beam/selects-distinct-atoms-after-a-partial-call-case";
    private const string Fixture = "beam_partial_atom_case.erl";
    private const string WrongBranch = "A partial atom case must select independently and keep the unmatched call result.";
    private const string Source = """
        -module(beam_partial_atom_case).
        -export([wrap/1]).

        wrap(Args) ->
            {wrapped, case erlang:hd(Args) of
                          near -> left;
                          far -> right
                      end}.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        object code = Code(image);
        byte[] opcodes = ((IEnumerable)Property(code, "Instructions")).Cast<object>()
            .Select(instruction => (byte)Property(Property(instruction, "Definition"), "Opcode")).ToArray();
        if (opcodes.Count(opcode => opcode == SelectValue) != 1 || !opcodes.Contains(CaseEnd))
        {
            throw new InvalidOperationException(WrongBranch);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term near = await module.Invoke(context, "wrap", [Term.List(Term.A("near"))]);
            Term far = await module.Invoke(context, "wrap", [Term.List(Term.A("far"))]);
            if (!near.Equals(Term.Tuple(Term.A("wrapped"), Term.A("left")))
                || !far.Equals(Term.Tuple(Term.A("wrapped"), Term.A("right"))))
            {
                throw new InvalidOperationException(WrongBranch);
            }

            Term other = Term.I(42);
            try
            {
                await module.Invoke(context, "wrap", [Term.List(other)]);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A("case_clause"), other)))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(WrongBranch);
        });
    }
}
