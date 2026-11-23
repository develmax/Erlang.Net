using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCheckedPairStringCaseChecks
{
    internal const string TestName = "compiler/beam/sinks-checked-pair-field-into-closed-string-arms";
    private const string Fixture = "beam_checked_pair_string.erl";
    private const string BadLayout = "Unexpected lifetime or failure edge for a checked string-dispatch pair.";
    private const string BadValue = "Checked string dispatch changed a branch value or its original badmatch.";

    private const string Source = """
        -module(beam_checked_pair_string).
        -export([route/2, payload_default/2, constructed_empty/2]).

        route([], _) -> empty;
        route([Item | _], Carry) ->
            (Pair = {Key, Payload}) = erlang:hd(Item),
            case Key of
                "location" -> {location, Payload, Pair, Carry};
                "status" -> {status, Payload, Carry};
                _ -> {fallback, Pair, Carry}
            end.

        constructed_empty([], Carry) -> {empty, Carry};
        constructed_empty([Item | _], Carry) ->
            (Pair = {Key, Payload}) = erlang:hd(Item),
            case Key of
                "location" -> {location, Payload, Pair, Carry};
                "status" -> {status, Payload, Carry};
                _ -> {fallback, Pair, Carry}
            end.

        payload_default([], _) -> empty;
        payload_default([Item | _], Carry) ->
            (Pair = {Key, Payload}) = erlang:hd(Item),
            case Key of
                "location" -> {location, Payload, Pair, Carry};
                "status" -> {status, Payload, Carry};
                _ -> {fallback, Pair, Payload, Carry}
            end.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        object code = Code(image);
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] body = ReadBody("route");
        object[] constructed = ReadBody("constructed_empty");
        if (!HasSunkProjection(body) || !HasSunkProjection(constructed)
            || HasSunkProjection(ReadBody("payload_default")) || !HasColdTupleFailure(constructed))
        {
            throw new InvalidOperationException(BadLayout);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term carry = Term.A("carry");
            Term payload = Term.A("payload");
            Term nonlist = Term.Tuple(Term.A("key"), payload);
            Term location = Term.Tuple(Term.String("location"), payload);
            Term status = Term.Tuple(Term.String("status"), payload);
            Term improper = Term.Tuple(new Cons(Term.I('s'), Term.A("suffix")), payload);
            if (!Term.Tuple(
                Term.A("location"),
                payload,
                location,
                carry
            ).Equals(await module.Invoke(
                context,
                "route",
                Rows(location),
                carry
            ))
                || !Term.Tuple(Term.A("status"), payload, carry).Equals(await module.Invoke(
                    context,
                    "route",
                    Rows(status),
                    carry
                ))
                || !Term.Tuple(Term.A("fallback"), nonlist, carry).Equals(await module.Invoke(
                    context,
                    "route",
                    Rows(nonlist),
                    carry
                ))
                || !Term.Tuple(Term.A("fallback"), improper, carry).Equals(await module.Invoke(
                    context,
                    "route",
                    Rows(improper),
                    carry
                ))
                || !Term.Tuple(
                    Term.A("fallback"),
                    nonlist,
                    payload,
                    carry
                ).Equals(await module.Invoke(
                    context,
                    "payload_default",
                    Rows(nonlist),
                    carry
                ))
                || !Term.Tuple(Term.A("empty"), carry).Equals(await module.Invoke(
                    context,
                    "constructed_empty",
                    Nil.Value,
                    carry
                ))
                || !Term.Tuple(Term.A("fallback"), nonlist, carry).Equals(await module.Invoke(
                    context,
                    "constructed_empty",
                    Rows(nonlist),
                    carry
                ))
                || !Term.A("empty").Equals(await module.Invoke(
                    context,
                    "route",
                    Nil.Value,
                    carry
                )))
            {
                throw new InvalidOperationException(BadValue);
            }

            Term malformed = Term.Tuple(Term.A("wrong_arity"));
            try
            {
                await module.Invoke(
                    context,
                    "route",
                    Rows(malformed),
                    carry
                );
            }
            catch (ErlangException error) when (Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), malformed).Equals(error.Reason))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(BadValue);
        });

        object[] ReadBody(string function)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && Items(instruction, "Operands") is [_, var name, var arity]
                    && atoms[Value(name)] == function && Value(arity) == 2
            );
            if (start < 0)
            {
                throw new InvalidOperationException(BadLayout);
            }

            return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static bool HasColdTupleFailure(object[] body)
    {
        int cold = Array.FindIndex(body, instruction => Opcode(instruction) == BadMatch);
        int returned = Array.FindLastIndex(body, instruction => Opcode(instruction) == Return);

        return returned >= 0 && cold > returned;
    }

    private static bool HasSunkProjection(object[] body)
    {
        int select = Array.FindIndex(body, instruction => Opcode(instruction) == SelectValue);

        return select >= 4 && Opcode(body[select - 1]) == GetList
            && Opcode(body[select - 2]) == GetTupleElement
            && Value(Items(body[select - 2], "Operands")[1]) == 1
            && Opcode(body[select - 3]) == IsNonemptyList
            && Opcode(body[select - 4]) == GetTupleElement
            && Value(Items(body[select - 4], "Operands")[1]) == 0
            && body.Skip(select + 1).Any(instruction => Opcode(instruction) == BadMatch);
    }

    private static Term Rows(Term value) => Term.List(Term.List(value));

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
