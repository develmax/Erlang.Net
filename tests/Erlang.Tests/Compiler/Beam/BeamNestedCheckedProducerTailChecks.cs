using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamNestedCheckedProducerTailChecks
{
    internal const string TestName = "compiler/beam/defers-nested-producer-payload-to-the-owning-arm";
    private const string WrongLayout = "The nested producer did not defer the payload to its arm or trim the retired input.";
    private const string WrongValue = "The nested producer changed the result, original failure value or effect order.";
    private const string Source = """
        -module(beam_nested_checked_producer_tail).
        -export([run/3]).
        run(Phase, Kind, Input) ->
            {hit, {Route, Payload}} = lookup(Input, route),
            case Route of
                selected ->
                    [Head, _Ignored] = Payload,
                    effect(Phase, Kind, Input, Head),
                    {ready, Members} = lookup(Input, members),
                    finish(Phase, Kind, Members);
                _ ->
                    effect(Phase, Kind, Input, Route)
            end.
        lookup({Route, _}, route) -> Route;
        lookup({_, Members}, members) -> Members.
        effect(Phase, Kind, Input, Route) ->
            erlang:put(observed, {Phase, Kind, Input, Route}),
            {fallback, Route}.
        finish(Phase, Kind, Members) -> {finished, Phase, Kind, Members}.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, "beam_nested_checked_producer_tail.erl");
        object code = Code(image);
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int info = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
            && atoms[Value(Operands(instruction)[1])] == "run");
        if (info < 0)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        object[] body = instructions.Skip(info + 1).TakeWhile(operation => Opcode(operation) != FunctionInfo).ToArray();
        int dispatch = Array.FindIndex(body, operation => Opcode(operation) == ExactEqualTest);
        if (dispatch < 0 || dispatch + 1 == body.Length || Opcode(body[dispatch + 1]) != GetTupleElement
            || !Operands(body[dispatch + 1]).Select(Value).SequenceEqual([1, 1, 0])
            || body.Where(operation => Opcode(operation) == Allocate)
                .Select(operation => Value(Operands(operation)[0])).SingleOrDefault(-1) != 3
            || !body.Where(operation => Opcode(operation) == TrimFrame)
                .SelectMany(operation => Operands(operation).Select(Value)).SequenceEqual([1, 2])
            || !body.Where(operation => Opcode(operation) == BadMatch)
                .Select(operation => Value(Operands(operation)[0])).SequenceEqual([0, 0, 0]))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term phase = Term.A("start");
            Term kind = Term.A("permanent");
            Term head = Term.A("chosen");
            Term selected = Term.A("selected");
            Term other = Term.A("other");
            Term bad = Term.A("bad");
            Term observed = Term.A("observed");
            Term members = Term.List(Term.A("member"));
            Term payload = Term.List(head, Term.A("ignored"));
            Term ready = Term.Tuple(Term.A("ready"), members);
            Term selectedInput = Term.Tuple(Route(selected, payload), ready);
            Term fallbackInput = Term.Tuple(Route(other, bad), bad);
            if (!Term.Tuple(
                Term.A("finished"),
                phase,
                kind,
                members
            ).Equals(await Invoke(selectedInput))
                || !EffectMatches(selectedInput, head)
                || !Term.Tuple(Term.A("fallback"), other).Equals(await Invoke(fallbackInput))
                || !EffectMatches(fallbackInput, other))
            {
                throw new InvalidOperationException(WrongValue);
            }

            Term malformed = Term.Tuple(Term.A("hit"), Term.Tuple(selected));
            await Reject(Term.Tuple(malformed, ready), malformed, null);
            Term tooLong = Term.List(head, bad, bad);
            await Reject(Term.Tuple(Route(selected, tooLong), ready), tooLong, null);
            Term secondFailure = Term.Tuple(Route(selected, payload), bad);
            await Reject(secondFailure, bad, head);

            return Nil.Value;

            static Term Route(Term route, Term value) => Term.Tuple(Term.A("hit"), Term.Tuple(route, value));

            bool EffectMatches(Term input, Term route) =>
                context.Dictionary.TryGetValue(observed, out Term? value)
                && Term.Tuple(
                    phase,
                    kind,
                    input,
                    route
                ).Equals(value);

            Task<Term> Invoke(Term input) => module.Invoke(
                context,
                "run",
                phase,
                kind,
                input
            ).AsTask();

            async Task Reject(Term input, Term failed, Term? effect)
            {
                _ = context.Dictionary.Remove(observed);
                try
                {
                    _ = await Invoke(input);
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A("badmatch"), failed)))
                {
                    if (effect is null ? !context.Dictionary.ContainsKey(observed) : EffectMatches(input, effect))
                    {
                        return;
                    }
                }

                throw new InvalidOperationException(WrongValue);
            }
        });
    }

    private static object[] Items(object value, string name) =>
        ((IEnumerable)Property(value, name)).Cast<object>().ToArray();

    private static object[] Operands(object value) => Items(value, "Operands");

    private static byte Opcode(object operation) => (byte)Property(Property(operation, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");
}
