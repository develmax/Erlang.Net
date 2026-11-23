using System.Collections;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTupleChoiceListSiblingHelperChecks
{
    internal const string TestName = "compiler/beam/lifts-list-siblings-through-literal-tuple-choices";
    private const string Fixture = "beam_tuple_choice_list_sibling_helpers.erl";
    private const string WrongOrder = "Tuple choice changed the expected comprehension helper identities.";
    private const string WrongValue = "Tuple choice selected a different comprehension.";

    private const string Source = """
        -module(beam_tuple_choice_list_sibling_helpers).
        -export([choose/2, constant_selector/1, duplicate_pattern/2]).

        choose(Rows, Flags) ->
            case {lists:member(first, Flags), lists:member(second, Flags)} of
                {true,true} -> [tag(both, Row) || Row <- Rows];
                {false,true} -> [tag(second, Row) || Row <- Rows];
                {true,false} -> [tag(first, Row) || Row <- Rows];
                {false,false} -> [tag(neither, Row) || Row <- Rows]
            end.

        constant_selector(Rows) ->
            case {true,false} of
                {true,true} -> [tag(other, Row) || Row <- Rows];
                {true,false} -> [tag(first, Row) || Row <- Rows]
            end.

        duplicate_pattern(Rows, Flags) ->
            case {lists:member(first, Flags), lists:member(second, Flags)} of
                {true,false} -> [tag(first, Row) || Row <- Rows];
                {true,false} -> [tag(other, Row) || Row <- Rows];
                {false,false} -> []
            end.

        tag(Key, Row) -> {Key, Row}.
        """;

    internal static async Task Run()
    {
        byte[] bytecode = CodeGeneration.CompileBeamModule(Source, Fixture);
        string[] names = ((IEnumerable)Property(Code(bytecode), "Functions")).Cast<object>()
            .OrderBy(function => (int)Property(function, "FirstInstructionIndex"))
            .Select(function => (string)Property(function, "Name"))
            .ToArray();
        if (!Helpers("choose", 2).SequenceEqual([
                "-choose/2-lc$^0/1-0-",
                "-choose/2-lc$^2/1-1-",
                "-choose/2-lc$^1/1-2-",
                "-choose/2-lc$^3/1-3-"
            ])
            || Helpers("constant_selector", 1).Any()
            || Helpers("duplicate_pattern", 2).Any())
        {
            throw new InvalidOperationException(WrongOrder);
        }

        var module = BeamModule.Prepare(bytecode);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term row = Term.A("row");
            Term first = Term.A("first");
            Term second = Term.A("second");
            Term rows = Term.List(row);
            (Term Flags, Term Tag)[] cases =
            [
                (Term.List(first, second), Term.A("both")),
                (Term.List(first), first),
                (Term.List(second), second),
                (Nil.Value, Term.A("neither"))
            ];
            foreach (var (flags, tag) in cases)
            {
                Term value = await module.Invoke(
                    context,
                    "choose",
                    rows,
                    flags
                );
                if (!Term.List(Term.Tuple(tag, row)).Equals(value))
                {
                    throw new InvalidOperationException(WrongValue);
                }
            }

            return Nil.Value;
        });

        IEnumerable<string> Helpers(string function, int arity) => names
            .Where(name => name.StartsWith(FormattableString.Invariant($"-{function}/{arity}-lc$^"), StringComparison.Ordinal));
    }
}
