using System.Collections;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamLiteralAppendListChecks
{
    internal const string TestName = "compiler/beam/fuses-a-literal-tail-into-a-list-mapper";
    private const string Fixture = "beam_literal_append_list_probe.erl";
    private const string Source = """
        -module(beam_literal_append_list_probe).
        -export([collect/0, unknown/1, bad_element/0, probe/1]).

        collect() ->
            Rows = [[one, two], [], [three]],
            envelope([K ++ [separator] || K <- Rows] ++ [terminal, marker]).

        unknown(Rows) ->
            envelope([K ++ [separator] || K <- Rows] ++ [terminal]).

        bad_element() ->
            Rows = [[one], bad],
            envelope([K ++ [separator] || K <- Rows] ++ [terminal]).

        probe(Rows) ->
            try unknown(Rows)
            catch error:Reason -> {failed, Reason}
            end.

        envelope(Result) -> {values, Result}.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        object code = Code(image);
        var names = ((IEnumerable)Property(code, "Functions")).Cast<object>()
            .Select(function => ((string)Property(function, "Name"), (int)Property(function, "Arity"))).ToHashSet();
        if (!names.Contains(("-collect/0-lc$^0/1-0-", 1))
            || names.Contains(("-unknown/1-lc$^0/1-0-", 1)))
        {
            throw new InvalidOperationException("Only the proper generator has the fused literal-tail helper.");
        }

        object mapping = ((IEnumerable)Property(code, "Functions")).Cast<object>().Single(function =>
            (string)Property(function, "Name") == "-collect/0-lc$^0/1-0-");
        int start = (int)Property(mapping, "FirstInstructionIndex");
        int end = (int)Property(mapping, "EndInstructionIndex");
        Term literalTail = Term.List(Term.A("terminal"), Term.A("marker"));
        var literals = ((IEnumerable)Property(Property(code, "Structure"), "Literals")).Cast<Term>();
        var operations = ((IEnumerable)Property(code, "Instructions")).Cast<object>().Skip(start).Take(end - start);
        if (!literals.Contains(literalTail) || operations.Any(operation =>
            (byte)Property(Property(operation, "Definition"), "Opcode") == IsNil))
        {
            throw new InvalidOperationException("A proven proper-list mapper must return its literal continuation.");
        }

        var dependencies = new BeamDependencySelection([ErlangModuleNames.Module]);
        var module = BeamModule.Prepare(image, dependencies);
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term separator = Term.A("separator");
            Term one = Term.A("one");
            Term invalid = Term.A("invalid");
            Term expected = Term.Tuple(
                Term.A("values"),
                Term.List(
                    Term.List(one, Term.A("two"), separator),
                    Term.List(separator),
                    Term.List(Term.A("three"), separator),
                    Term.A("terminal"),
                    Term.A("marker")
                )
            );
            Term failure = Term.Tuple(Term.A("failed"), Term.Tuple(Term.A("bad_generator"), invalid));
            if (!expected.Equals(await module.Invoke(context, "collect", []))
                || !failure.Equals(await module.Invoke(context, "probe", [new Cons(Term.List(one), invalid)])))
            {
                throw new InvalidOperationException("The fused continuation must preserve value order and the generator boundary.");
            }

            try
            {
                await module.Invoke(context, "bad_element", []);
            }
            catch (ErlangException exception) when (exception.Reason.Equals(Term.A("badarg")))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException("Appending a non-list head must raise badarg.");
        });
    }
}
