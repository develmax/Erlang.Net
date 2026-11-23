using System.Collections;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamListFilterTupleExclusionChecks
{
    internal const string TestName = "compiler/beam/shares-one-tuple-projection-in-list-filter";
    private const string WrongLayout = "Expected one tuple projection for identical guard operands, and independent projections for distinct fields.";
    private const string WrongResult = "The guarded generator returned an unexpected value or failed to reject an improper list.";
    private const string Source = """
        -module(list_filter_tuple_exclusions).
        -export([strip/1, strip_mixed/1, sample/0, mixed/0, improper/0]).

        strip(Values) ->
            [Value || Value <- Values, element(1, Value) =/= white_space, erlang:element(1, Value) =/= comment].

        strip_mixed(Values) ->
            [Value || Value <- Values, element(1, Value) =/= comment, element(2, Value) =/= white_space].

        sample() -> [malformed, {}, {white_space, 1}, {comment, 2}, {normal, 3}, {kept}].
        mixed() -> [{normal, white_space}, {comment, keep}, {normal, keep}].
        improper() -> [{normal, 3} | bad_tail].
        """;

    internal static async Task Run()
    {
        byte[] bytes = CodeGeneration.CompileBeamModule(Source, "list_filter_tuple_exclusions.erl");
        byte[] opcodes = ((IEnumerable)Property(Code(bytes), "Instructions")).Cast<object>()
            .Select(instruction => (byte)Property(Property(instruction, "Definition"), "Opcode")).ToArray();
        if (opcodes.Count(opcode => opcode == BifTwo) != 3 || !opcodes.Contains(SelectValue))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        var image = BeamModule.Prepare(bytes);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term sample = await image.Invoke(context, "sample");
            Term mixed = await image.Invoke(context, "mixed");
            Term improper = await image.Invoke(context, "improper");
            Term expected = Term.List(Term.Tuple(Term.A("normal"), Term.I(3)), Term.Tuple(Term.A("kept")));
            Term expectedMixed = Term.List(Term.Tuple(Term.A("normal"), Term.A("keep")));
            if (!expected.Equals(await image.Invoke(context, "strip", sample))
                || !expectedMixed.Equals(await image.Invoke(context, "strip_mixed", mixed)))
            {
                throw new InvalidOperationException(WrongResult);
            }

            try
            {
                await image.Invoke(context, "strip", improper);
                throw new InvalidOperationException(WrongResult);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A("bad_generator"), Term.A("bad_tail"))))
            {
            }

            return Nil.Value;
        });
    }
}
