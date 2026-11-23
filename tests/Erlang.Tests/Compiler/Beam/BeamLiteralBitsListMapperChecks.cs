using System.Collections;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamLiteralBitsListMapperChecks
{
    internal const string TestName = "compiler/beam/lifts-list-mappers-with-closed-bitstring-operands";
    private const string Source = """
        -module(beam_literal_bits_list_mapper).
        -export([calls/1, tuples/1, dynamic/1]).

        calls([_ | _] = Rows) ->
            Pairs = [pack(join(<<9>>, Row)) || Row <- Rows],
            {pairs, Pairs};
        calls([]) ->
            {pairs, []}.
        tuples(Rows) ->
            [{Key, <<>>} || {add, Key} <- Rows].
        dynamic(Rows) ->
            [{Key, <<Key>>} || {add, Key} <- Rows].

        join(Separator, Row) -> {Separator, Row}.
        pack(Value) -> {packed, Value}.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, "beam_literal_bits_list_mapper.erl");
        object code = Code(image);
        var generated = ((IEnumerable)Property(code, "Functions")).Cast<object>()
            .Select(item => ((string)Property(item, "Name"), (int)Property(item, "Arity"))).ToHashSet();
        if (!generated.Contains(("-calls/1-lc$^0/1-0-", 1))
            || !generated.Contains(("-tuples/1-lc$^0/1-0-", 1))
            || generated.Contains(("-dynamic/1-lc$^0/1-0-", 1)))
        {
            throw new InvalidOperationException("Only the closed bitstrings qualify for ordinary helper lowering.");
        }

        var module = BeamModule.Prepare(image, new BeamDependencySelection([ErlangModuleNames.Module]));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term first = Term.A("first");
            Term second = Term.A("second");
            Term add = Term.A("add");
            Term packed = Term.A("packed");
            Term separator = new BitString([9]);
            Term empty = new BitString([]);
            Term calls = Term.Tuple(
                Term.A("pairs"),
                Term.List(
                    Term.Tuple(packed, Term.Tuple(separator, first)),
                    Term.Tuple(packed, Term.Tuple(separator, second))
                )
            );
            Term tuples = Term.List(Term.Tuple(first, empty), Term.Tuple(second, empty));
            Term rows = Term.List(Term.Tuple(add, first), Term.Tuple(second, add), Term.Tuple(add, second));
            if (!calls.Equals(await module.Invoke(context, "calls", [Term.List(first, second)]))
                || !tuples.Equals(await module.Invoke(context, "tuples", [rows]))
                || !Term.List(Term.Tuple(Term.I(9), separator)).Equals(await module.Invoke(
                    context,
                    "dynamic",
                    [Term.List(Term.Tuple(add, Term.I(9)))]
                )))
            {
                throw new InvalidOperationException("List helper lowering changed bitstring values or list order.");
            }

            try
            {
                await module.Invoke(context, "dynamic", [Term.List(Term.Tuple(add, first))]);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A("badarg")))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException("An invalid dynamic bit segment must still raise badarg.");
        });
    }
}
