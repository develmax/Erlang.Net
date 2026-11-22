using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamRetainedRecordMapResultChecks
{
    internal const string TestName = "compiler/beam/retains-one-carrier-across-effects-and-map-result";
    private const string Fixture = "beam_retained_record_map_result_probe.erl";
    private const string WrongLayout = "The checked map result must retain one original carrier and test unused map values by presence.";
    private const string WrongOutcome = "The retained record result changed an effect, payload, or source failure.";

    private const string Source = """
        -module(beam_retained_record_map_result_probe).
        -export([dispatch/4]).
        -record(inner, {options, flag}).
        -record(outer, {member}).
        dispatch(event, {notice, _}, {accepted, client}, Carrier=#outer{member=Nested}) ->
            notify(ok),
            handshake(connected, Carrier),
            Updated = Carrier#outer{member=Nested#inner{flag=true}},
            {Discard, Interval} = begin
                #{count := C, interval := I} = lookup(options, alive, Nested#inner.options, fixture, 100),
                {C, I}
            end,
            {next_state, {connected, client}, Updated,
             [{{timeout, alive}, Interval, none}, {callback, handler}]};
        dispatch(event, {notice, _}, {waiting, client}, Data) -> {waiting, Data};
        dispatch(event, {single}, _, Data) -> {single, Data};
        dispatch(Event, Message, State, Data) -> {fallback, Event, Message, State, Data}.
        notify(ok) -> erlang:put(first_call, notified).
        handshake(connected, Carrier) -> erlang:put(second_call, Carrier).
        lookup(options, alive, Options, fixture, 100) -> erlang:put(third_call, Options), Options.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        if (!HasOwnedMapLookup(image) || HasOwnedMapLookup(CodeGeneration.CompileBeamModule(Source.Replace(
            "Interval, none",
            "Interval, Discard",
            StringComparison.Ordinal
        ), Fixture)))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term first = Term.A("first_call");
            Term second = Term.A("second_call");
            Term third = Term.A("third_call");
            Term notice = Term.Tuple(Term.A("notice"), Term.A("payload"));
            Term route = Term.Tuple(Term.A("accepted"), Term.A("client"));
            Term interval = Term.I(37);
            Term options = new MapTerm([new(Term.A("count"), Term.I(5)), new(Term.A("interval"), interval)]);
            Term original = Carrier(options, Term.A(ErlangBooleanAtoms.False));
            Term updated = Carrier(options, Term.A(ErlangBooleanAtoms.True));
            Term action = Term.Tuple(Term.Tuple(Term.A("timeout"), Term.A("alive")), interval, Term.A("none"));
            Term callback = Term.Tuple(Term.A("callback"), Term.A("handler"));
            Term expected = Term.Tuple(
                Term.A("next_state"),
                Term.Tuple(Term.A("connected"), Term.A("client")),
                updated,
                Term.List(action, callback)
            );
            if (!(await Invoke(original)).Equals(expected)
                || !context.Dictionary[first].Equals(Term.A("notified"))
                || !context.Dictionary[second].Equals(original)
                || !context.Dictionary[third].Equals(options))
            {
                throw new InvalidOperationException(WrongOutcome);
            }

            foreach (Term invalid in (Term[])
            [
                new MapTerm([new(Term.A("interval"), interval)]),
                new MapTerm([new(Term.A("count"), Term.I(5))]),
                Term.A("not_a_map")
            ])
            {
                context.Dictionary.Remove(third);
                await Fail(Carrier(invalid, Term.A("old")), Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), invalid));
                if (!context.Dictionary[third].Equals(invalid))
                {
                    throw new InvalidOperationException(WrongOutcome);
                }
            }

            context.Dictionary.Remove(first);
            context.Dictionary.Remove(second);
            context.Dictionary.Remove(third);
            Term malformed = Term.A("malformed_record");
            Term malformedCarrier = Term.Tuple(Term.A("outer"), malformed);
            await Fail(malformedCarrier, Term.Tuple(Term.A(RecordAtoms.BadRecord), malformed));
            if (!context.Dictionary[first].Equals(Term.A("notified"))
                || !context.Dictionary[second].Equals(malformedCarrier)
                || context.Dictionary.ContainsKey(third))
            {
                throw new InvalidOperationException(WrongOutcome);
            }

            return Nil.Value;

            async Task<Term> Invoke(Term carrier) => await module.Invoke(
                context,
                "dispatch",
                Term.A("event"),
                notice,
                route,
                carrier
            );

            async Task Fail(Term carrier, Term reason)
            {
                try
                {
                    _ = await Invoke(carrier);
                }
                catch (ErlangException error) when (error.Reason.Equals(reason))
                {
                    return;
                }

                throw new InvalidOperationException(WrongOutcome);
            }
        });
    }

    private static Term Carrier(Term options, Term flag) =>
        Term.Tuple(Term.A("outer"), Term.Tuple(Term.A("inner"), options, flag));

    private static bool HasOwnedMapLookup(byte[] image)
    {
        object[] body = Items(Code(image), "Instructions").ToArray();
        byte[] mapChecks = [IsMap, HasMapFields, GetMapElements];
        int lookup = Array.FindIndex(body, instruction => Opcode(instruction) == HasMapFields);
        if (lookup < 1 || !body.Skip(lookup - 1).Take(3).Select(Opcode).SequenceEqual(mapChecks))
        {
            return false;
        }

        object? frame = body.Take(lookup).LastOrDefault(instruction => Opcode(instruction) is Allocate or AllocateHeap);

        return frame is not null && Opcode(frame) == Allocate && Operands(frame).Select(Value).SequenceEqual([1, 4]);
    }

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
