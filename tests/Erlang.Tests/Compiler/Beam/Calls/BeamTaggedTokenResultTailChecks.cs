using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTaggedTokenResultTailChecks
{
    internal const string TestName = "compiler/beam/forwards-tagged-token-result-into-a-local-tail";
    private const string Fixture = "beam_tagged_token_result.erl";
    private const string BadLayout = "Expected direct token extraction and the joined result in the outgoing register.";
    private const string BadValue = "The local tail lost a saved argument or changed token-list rejection.";

    private const string Source = """
        -module(beam_tagged_token_result).
        -export([route/3, prefix/3]).

        route(A, B, Input) ->
            Result = case tokenize(Input) of
                {ok, [Token, Keep]} -> {decode(Token), Keep};
                _ -> {default, missing}
            end,
            finish(A, B, Result).

        prefix(A, B, Input) ->
            Result = case tokenize(Input) of
                {ok, [Token, Keep | _]} -> {decode(Token), Keep};
                _ -> {default, missing}
            end,
            finish(A, B, Result).

        tokenize({wrapped, Tokens}) -> {ok, Tokens};
        tokenize(_) -> rejected.

        decode(number) -> 7.

        finish(A, B, Result) -> {A, B, Result}.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        object code = Code(image);
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        if (!UsesOutgoingTuple(ReadBody("route")) || UsesOutgoingTuple(ReadBody("prefix")))
        {
            throw new InvalidOperationException(BadLayout);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term left = Term.A("left");
            Term right = Term.A("right");
            Term value = Term.A("kept");
            Term number = Term.A("number");
            Term decoded = Term.Tuple(Term.I(7), value);
            Term rejected = Term.Tuple(Term.A("default"), Term.A("missing"));
            Term extra = Term.List(number, value, Term.A("extra"));
            Term improper = new Cons(number, new Cons(value, Term.A("improper")));
            await Check("route", Wrapped(Term.List(number, value)), decoded);
            await Check("route", Wrapped(extra), rejected);
            await Check("route", Wrapped(improper), rejected);
            await Check("route", Wrapped(Term.List(number)), rejected);
            await Check("route", Term.A("not_wrapped"), rejected);
            await Check("prefix", Wrapped(extra), decoded);

            try
            {
                await module.Invoke(
                    context,
                    "route",
                    left,
                    right,
                    Wrapped(Term.List(Term.A("invalid"), value))
                );
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.FunctionClause)))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(BadValue);

            async Task Check(string function, Term input, Term result)
            {
                Term expected = Term.Tuple(left, right, result);
                if (!expected.Equals(await module.Invoke(
                    context,
                    function,
                    left,
                    right,
                    input
                )))
                {
                    throw new InvalidOperationException(BadValue);
                }
            }
        });

        object[] ReadBody(string function)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && Items(instruction, "Operands") is [_, var name, var arity]
                    && atoms[Value(name)] == function && Value(arity) == 3
            );
            if (start < 0)
            {
                throw new InvalidOperationException(BadLayout);
            }

            return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static bool UsesOutgoingTuple(object[] body) =>
        body.Any(instruction => Opcode(instruction) == IsTaggedTuple)
        && body.Count(instruction => Opcode(instruction) == GetList) == 2
        && body.Any(instruction => Opcode(instruction) == PutTuple
            && Items(instruction, "Operands")[0] is var destination
            && Value(destination) == 2 && Equals(Property(destination, "Kind"), "x"))
        && body.Count(instruction => Opcode(instruction) == TailLocalCallWithFrame) == 1;

    private static Term Wrapped(Term tokens) => Term.Tuple(Term.A("wrapped"), tokens);

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
