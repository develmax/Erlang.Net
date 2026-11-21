using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCoveredTaggedCallPairChecks
{
    internal const string TestName = "compiler/beam/projects-a-covered-tagged-call-pair-before-selecting";
    private const string Module = "beam_covered_tagged_call_pair";
    private const string Maps = "maps";
    private const string WrongCase = "Expected one checked payload projection with the original case-clause failure value.";
    private const string Source = """
        -module(beam_covered_tagged_call_pair).
        -export([complete/1, partial/1]).
        complete(Box) ->
            case maps:get(value, Box) of
                {error, Reason} -> {bad, Reason};
                {ok, {item, Value}}
                when tuple_size(Value) =:= 2, element(1, Value) =:= local;
                     Value =:= undefined -> {good, Value};
                {ok, _} -> retry
            end.
        partial(Box) ->
            case maps:get(value, Box) of
                {error, Reason} -> {bad, Reason};
                {ok, {item, Value}}
                when tuple_size(Value) =:= 2, element(1, Value) =:= local;
                     Value =:= undefined -> {good, Value}
            end.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Module + ".erl");
        object code = Code(image);
        if (!HasSharedPayload(code, "complete") || HasSharedPayload(code, "partial"))
        {
            throw new InvalidOperationException(WrongCase);
        }

        BeamModule module = BeamModule.Prepare(image, new BeamDependencySelection([Maps]));
        var selection = new RuntimeLibrarySelection(
        [
            RuntimeModuleSelection.Managed("erlang"),
            RuntimeModuleSelection.Managed(Maps)
        ]);
        await using var runtime = await ProcessRuntime.CreateAsync(selection);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term address = Term.Tuple(Term.A("local"), Term.A("path"));
            Term undefined = Term.A("undefined");
            Term other = Term.A("other");
            Term malformed = Term.Tuple(Term.A("unknown"), other);
            Term rejected = Term.Tuple(Term.A("ok"), other);
            Term accepted = Term.Tuple(Term.A("ok"), Term.Tuple(Term.A("item"), address));
            Term missingAddress = Term.Tuple(Term.A("ok"), Term.Tuple(Term.A("item"), undefined));
            Term error = Term.Tuple(Term.A("error"), other);
            if (!Term.Tuple(Term.A("good"), address).Equals(await module.Invoke(context, "complete", Box(accepted)))
                || !Term.Tuple(Term.A("good"), undefined).Equals(await module.Invoke(context, "complete", Box(missingAddress)))
                || !Term.A("retry").Equals(await module.Invoke(context, "complete", Box(rejected)))
                || !Term.Tuple(Term.A("bad"), other).Equals(await module.Invoke(context, "complete", Box(error))))
            {
                throw new InvalidOperationException(WrongCase);
            }

            await RequireFailure("complete", malformed);
            await RequireFailure("complete", other);
            await RequireFailure("partial", rejected);

            return Nil.Value;

            async Task RequireFailure(string function, Term result)
            {
                try
                {
                    await module.Invoke(context, function, Box(result));
                }
                catch (ErlangException failure) when (failure.Reason.Equals(Term.Tuple(Term.A("case_clause"), result)))
                {
                    return;
                }

                throw new InvalidOperationException(WrongCase);
            }
        });
    }

    private static MapTerm Box(Term result) => new([new(Term.A("value"), result)]);

    private static bool HasSharedPayload(object code, string name)
    {
        object function = Items(code, "Functions").Single(item => (string)Property(item, "Name") == name);
        int start = (int)Property(function, "FirstInstructionIndex");
        int end = (int)Property(function, "EndInstructionIndex");
        object[] body = Items(code, "Instructions").Skip(start).Take(end - start).ToArray();
        int select = Array.FindIndex(body, item => Opcode(item) == SelectValue);

        return select >= 2
            && Projection(body[select - 2], 0, "x") && Projection(body[select - 1], 1, "y");
    }

    private static bool Projection(object instruction, int field, string destinationKind) =>
        Opcode(instruction) == GetTupleElement
        && Items(instruction, "Operands").ToArray() is [var source, var index, var destination]
        && (string)Property(source, "Kind") == "x" && Value(source) == 0
        && Value(index) == field && (string)Property(destination, "Kind") == destinationKind;

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string property) => ((IEnumerable)Property(owner, property)).Cast<object>();
}
