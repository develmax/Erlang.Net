using System.Collections;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamNestedEntryConsCarrierChecks
{
    internal const string TestName = "compiler/beam/retains-matched-entry-cons-through-an-ordinary-call";
    private const string WrongLayout = "Expected the matched field to feed the call without reconstructing its cons.";
    private const string WrongValue = "Unexpected list or guard behavior from the retained field.";
    private const string Source = """
        -module(nested_entry_cons_carrier).
        -export([keep/1, live_tail/1]).

        keep({Label, [Head | Tail]}) when is_tuple(Head) ->
            {lists:reverse(Label), lists:reverse([Head | Tail])};
        keep({Label, Value}) ->
            {Label, Value}.

        live_tail({Label, [Head | Tail]}) when is_tuple(Head) ->
            {lists:reverse(Label), lists:reverse([Head | Tail]), Tail};
        live_tail({Label, Value}) ->
            {Label, Value}.
        """;

    internal static async Task Run()
    {
        byte[] bytes = CodeGeneration.CompileBeamModule(Source, "nested_entry_cons_carrier.erl");
        byte[] opcodes = ((IEnumerable)Property(Code(bytes), "Instructions")).Cast<object>()
            .Select(instruction => (byte)Property(Property(instruction, "Definition"), "Opcode")).ToArray();
        int[] entries = Enumerable.Range(0, opcodes.Length).Where(index => opcodes[index] == FunctionInfo).ToArray();
        if (entries.Length < 3)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        byte[] kept = opcodes[entries[0]..entries[1]];
        byte[] retainedTail = opcodes[entries[1]..entries[2]];
        if (!kept.Contains(GetHead) || kept.Contains(PutList) || !retainedTail.Contains(PutList))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        var image = BeamModule.Prepare(bytes, new BeamDependencySelection(["lists"]));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term label = Term.List(Term.I(107), Term.I(101), Term.I(121));
            Term reversedLabel = Term.List(Term.I(121), Term.I(101), Term.I(107));
            Term first = Term.Tuple(Term.A("head"), Term.I(11));
            Term second = Term.A("rest");
            Term tail = Term.List(second);
            Term reversed = Term.List(second, first);
            Term original = Term.Tuple(label, Term.List(first, second));
            Term guardFailure = Term.Tuple(label, Term.List(second, first));
            if (!Term.Tuple(reversedLabel, reversed).Equals(await image.Invoke(context, "keep", original))
                || !Term.Tuple(reversedLabel, reversed, tail).Equals(await image.Invoke(context, "live_tail", original))
                || !guardFailure.Equals(await image.Invoke(context, "keep", guardFailure)))
            {
                throw new InvalidOperationException(WrongValue);
            }

            return Nil.Value;
        });
    }
}
