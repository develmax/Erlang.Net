using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamRepeatedCheckedPeerTagChecks
{
    internal const string TestName = "compiler/beam/owns-repeated-tag-literal-retries-with-checked-peers";
    private const string Fixture = "beam_repeated_checked_peer_tags.erl";
    private const string WrongDispatch = "The repeated tag bank must select its outer tag before any arm frame.";
    private const string WrongResult = "The repeated tag bank changed literal priority, the peer retry, or the default value.";
    private const string Source = """
        -module(beam_repeated_checked_peer_tags).
        -export([run/2, retry/2, observe/2]).
        -record(state, {items}).

        run({prefix, [], _}, State) ->
            State;
        run({prefix, Prefix, Payload}, #state{items=Items}=State) ->
            State#state{items=[{Prefix,Payload}|Items]};
        run({route, "main", Payload}, #state{items=Items}=State) ->
            State#state{items=[{preferred,Payload}|Items]};
        run({route, Target, Payload}, #state{items=[Head|Items]}=State) ->
            State#state{items=[{Target,Head,Payload}|Items]};
        run({legacy, Payload}, State) ->
            {legacy,Payload,State};
        run(_, State) ->
            State.

        retry({prefix, Prefix, Payload}, #state{items=Items}=State) ->
            State#state{items=[{Prefix,Payload}|Items]};
        retry({route, "main", Payload}, #state{items=[Head|Items]}=State) ->
            State#state{items=[{preferred,Head,Payload}|Items]};
        retry({route, _, Payload}, #state{items=Items}=State) ->
            State#state{items=[{retry,Payload}|Items]};
        retry({legacy, Payload}, State) ->
            {legacy,Payload,State};
        retry(_, State) ->
            State.

        observe({prefix, Prefix, Payload}, #state{items=Items}=State) ->
            State#state{items=[{Prefix,Payload}|Items]};
        observe({route, "main", Payload}, #state{items=Items}=State) ->
            State#state{items=[{preferred,Payload}|Items]};
        observe({route, Target, Payload}, #state{items=[Head|Items]}=State) ->
            State#state{items=[{Target,Head,Payload}|Items]};
        observe({legacy, Payload}, State) ->
            {legacy,Payload,State};
        observe(Event, State) ->
            {Event,State}.
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Fixture);
        CheckEntry(beam);

        BeamModule module = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term state = Term.A("state");
            Term payload = Term.A("payload");
            Term source = Term.A("previous");
            Term plain = Term.Tuple(state, Nil.Value);
            Term populated = Term.Tuple(state, Term.List(source));
            Term malformed = Term.A("wrong_peer");
            Term prefix = Term.A("prefix");
            Term route = Term.A("route");
            Term literal = Term.String("main");
            Term ordinary = Term.A("other");

            await Check(
                "run",
                Term.Tuple(prefix, Nil.Value, payload),
                populated,
                populated
            );
            await Check(
                "run",
                Term.Tuple(prefix, ordinary, payload),
                plain,
                StateWith(Term.Tuple(ordinary, payload))
            );
            await Check(
                "run",
                Term.Tuple(route, literal, payload),
                plain,
                StateWith(Term.Tuple(Term.A("preferred"), payload))
            );
            await Check(
                "run",
                Term.Tuple(route, ordinary, payload),
                populated,
                StateWith(Term.Tuple(ordinary, source, payload))
            );
            await Check(
                "run",
                Term.Tuple(route, literal, payload),
                malformed,
                malformed
            );
            await Check(
                "run",
                Term.Tuple(route, ordinary, payload),
                plain,
                plain
            );
            await Check(
                "retry",
                Term.Tuple(route, literal, payload),
                plain,
                StateWith(Term.Tuple(Term.A("retry"), payload))
            );

            Term otherEvent = Term.Tuple(Term.A("missing"), ordinary, payload);
            await Check(
                "observe",
                otherEvent,
                plain,
                Term.Tuple(otherEvent, plain)
            );

            return Nil.Value;

            Term StateWith(Term item) => Term.Tuple(state, Term.List(item));

            async Task Check(
                string function,
                Term evt,
                Term receiver,
                Term expected
            )
            {
                if (!expected.Equals(await module.Invoke(
                    context,
                    function,
                    evt,
                    receiver
                )))
                {
                    throw new InvalidOperationException(WrongResult);
                }
            }
        });
    }

    private static void CheckEntry(byte[] beam)
    {
        object code = Code(beam);
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] body = Body("run");
        int arity = Array.FindIndex(body, instruction => Opcode(instruction) == SelectTupleArity);
        int select = Array.FindIndex(body, instruction => Opcode(instruction) == SelectValue);
        if (arity < 0 || select < 3 || select - 3 <= arity
            || body.Take(select).Any(instruction => Opcode(instruction) is Allocate or AllocateHeap)
            || !Projection(body[select - 3], 0, 2)
            || !Projection(body[select - 2], 1, 3)
            || !Projection(body[select - 1], 2, 0))
        {
            throw new InvalidOperationException(WrongDispatch);
        }

        object[] observed = Body("observe");
        int retainedSelect = Array.FindIndex(observed, instruction => Opcode(instruction) == SelectValue);
        if (retainedSelect < 1 || observed.Take(retainedSelect).Any(instruction => Projection(instruction, 2, 0)))
        {
            throw new InvalidOperationException(WrongDispatch);
        }

        object[] Body(string function)
        {
            int entry = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Operands(instruction)[1])] == function);

            return entry < 0
                ? throw new InvalidOperationException(WrongDispatch)
                : instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }

        static bool Projection(object instruction, int field, int target)
        {
            if (Opcode(instruction) != GetTupleElement)
            {
                return false;
            }

            object[] operands = Operands(instruction);

            return Value(operands[0]) == 0 && Value(operands[1]) == field && Value(operands[2]) == target;
        }
    }

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands");

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
