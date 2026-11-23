using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCheckedNilStateRetryChecks
{
    internal const string TestName = "compiler/beam/shares-checked-nil-state-retry-prefix";
    private const string Fixture = "beam_checked_nil_state_retry_probe.erl";
    private const string WrongLayout = "The nil match and its retry must share a checked inner record projection before frame allocation.";
    private const string WrongOutcome = "The checked nil match changed the selected source clause, original arguments, or error.";

    private const string Source = """
        -module(beam_checked_nil_state_retry_probe).
        -export([dispatch/4]).
        -record(inner, {items, other}).
        -record(outer, {nested, marker}).
        dispatch(internal, {failure, _, _}, Route={retry, client},
                 Outer=#outer{nested=#inner{items=[]}}) ->
            {first, Route, erlang:hd(Outer#outer.marker)};
        dispatch(internal, {failure, Payload, _}, {retry, client}=Route,
                 Outer=#outer{nested=Inner}) ->
            case Inner#inner.items of
                alternate -> {alternate, Route, Payload, Outer};
                _ -> {second, Route, Payload, Inner}
            end;
        dispatch(internal, {failure, _, _}, {state_b, client}, Data) -> {owned_b, Data};
        dispatch(internal, {failure, _, _}, {state_c, client}, Data) -> {owned_c, Data};
        dispatch(internal, {peer, Payload, _}, {state_b, client}, Data) -> {peer_b, Payload, Data};
        dispatch(internal, {peer, Payload, _}, {state_c, client}, Data) -> {peer_c, Payload, Data};
        dispatch(internal, {small}, {state_b, client}, Data) -> {small, Data};
        dispatch(Type, Event, State, Data) -> {fallback, Type, Event, State, Data}.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        if (!HasSharedNilPrefix(image)
            || HasSharedNilPrefix(CodeGeneration.CompileBeamModule(Source.Replace(
                "case Inner#inner.items of",
                "case Inner#inner.other of",
                StringComparison.Ordinal
            ), Fixture))
            || HasSharedNilPrefix(CodeGeneration.CompileBeamModule(
                Source.Replace(
                "case Inner#inner.items of",
                "erlang:put(marker, Payload), case Inner#inner.items of",
                StringComparison.Ordinal
            ),
                Fixture
            )))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term internalEvent = Term.A("internal");
            Term payload = Term.A("payload");
            Term message = Term.Tuple(Term.A("failure"), payload, Term.A("unused"));
            Term route = Term.Tuple(Term.A("retry"), Term.A("client"));
            Term wrongRole = Term.Tuple(Term.A("retry"), Term.A("server"));
            Term marker = Term.A("marker");
            Term other = Term.A("other");
            Term empty = Term.Tuple(Term.A("inner"), Nil.Value, other);
            Term alternate = Term.Tuple(Term.A("inner"), Term.A("alternate"), other);
            Term improper = Term.Tuple(Term.A("inner"), new Cons(marker, other), other);
            Term bad = Term.Tuple(Term.A("invalid"), Nil.Value, other);
            Term emptyOuter = Term.Tuple(Term.A("outer"), empty, Term.List(marker));
            Term alternateOuter = Term.Tuple(Term.A("outer"), alternate, marker);
            Term improperOuter = Term.Tuple(Term.A("outer"), improper, marker);

            await Check(route, emptyOuter, Term.Tuple(Term.A("first"), route, marker));
            await Check(route, alternateOuter, Term.Tuple(
                Term.A("alternate"),
                route,
                payload,
                alternateOuter
            ));
            await Check(route, improperOuter, Term.Tuple(
                Term.A("second"),
                route,
                payload,
                improper
            ));
            await Check(wrongRole, emptyOuter, Term.Tuple(
                Term.A("fallback"),
                internalEvent,
                message,
                wrongRole,
                emptyOuter
            ));
            await Check(route, bad, Term.Tuple(
                Term.A("fallback"),
                internalEvent,
                message,
                route,
                bad
            ));
            await CheckError(
                Term.Tuple(Term.A("outer"), bad, marker),
                Term.Tuple(Term.A(RecordAtoms.BadRecord), bad)
            );
            await CheckError(Term.Tuple(Term.A("outer"), empty, marker), Term.A(ErlangErrorReasons.BadArgument));

            return Nil.Value;

            async Task Check(Term state, Term data, Term expected)
            {
                Term result = await Invoke(state, data);
                if (!result.Equals(expected))
                {
                    throw new InvalidOperationException(WrongOutcome);
                }
            }

            async Task CheckError(Term data, Term reason)
            {
                try
                {
                    _ = await Invoke(route, data);
                }
                catch (ErlangException error) when (error.Reason.Equals(reason))
                {
                    return;
                }

                throw new InvalidOperationException(WrongOutcome);
            }

            async Task<Term> Invoke(Term state, Term data) => await module.Invoke(
                context,
                "dispatch",
                internalEvent,
                message,
                state,
                data
            );
        });
    }

    private static bool HasSharedNilPrefix(byte[] image)
    {
        object[] code = Items(Code(image), "Instructions").ToArray();
        byte[] prefix = [GetTupleElement, IsTaggedTuple, GetTupleElement, IsNil];
        for (int start = 0; start + prefix.Length <= code.Length; start++)
        {
            if (!code.Skip(start).Take(prefix.Length).Select(Opcode).SequenceEqual(prefix)
                || Operands(code[start]) is not [var outer, _, var inner]
                || Operands(code[start + 1]) is not [var failure, var tested, _, _]
                || Operands(code[start + 2]) is not [var source, _, var selected]
                || Operands(code[start + 3]) is not [_, var nil]
                || !IsX(outer, 3) || !IsX(inner, 0) || !IsX(tested, 0)
                || !IsX(source, 0) || !IsX(selected, 1) || !IsX(nil, 1))
            {
                continue;
            }

            int target = Array.FindIndex(code, instruction =>
                Opcode(instruction) == Label && Value(Operands(instruction)[0]) == Value(failure));
            if (target >= 0 && code.Skip(target + 1).Take(2).Select(Opcode).SequenceEqual([Line, BadRecord])
                && IsX(Operands(code[target + 2])[0], 0))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsX(object operand, int register) => Property(operand, "Kind") is "x" && Value(operand) == register;

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
