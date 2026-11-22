using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCheckedPeerTagChecks
{
    internal const string TestName = "compiler/beam/selects-disjoint-tags-before-checked-peer-records";
    private const string Fixture = "beam_checked_peer_tags.erl";
    private const string WrongDispatch = "The disjoint tuple tag switch must precede the carrier checks and branch frames.";
    private const string WrongResult = "The checked branch changed a record, equality mismatch, or original fallback arguments.";
    private const string Source = """
        -module(beam_checked_peer_tags).
        -export([dispatch/2, drop_event/2]).
        -record(state, {count, items, keys}).
        -record(entry, {value}).

        dispatch({add, Value}, #state{count=[N|Tail],items=[#entry{value=Previous}=Item|Rest]}=State) ->
            State#state{count=[N+1|Tail],items=[Item#entry{value={Previous, Value}}|Rest]};
        dispatch({erase, Key}, #state{keys=[{Key,_}|Rest]}=State) ->
            State#state{keys=Rest};
        dispatch({legacy}, State) ->
            {legacy,State};
        dispatch(Event, State) ->
            {outside,Event,State}.

        drop_event({add, Value}, #state{items=[Previous|Rest]}=State) ->
            State#state{items=[{Previous,Value}|Rest]};
        drop_event({erase, Key}, #state{keys=[{Key,_}|Rest]}=State) ->
            State#state{keys=Rest};
        drop_event({legacy}, State) ->
            {legacy,State};
        drop_event(_, State) ->
            State.
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Fixture);
        CheckEntry(beam, "dispatch", carriesPayload: false);
        CheckEntry(beam, "drop_event", carriesPayload: true);

        BeamModule module = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term state = Term.A("state");
            Term entry = Term.A("entry");
            Term before = Term.A("before");
            Term after = Term.A("after");
            Term keys = Term.List(Term.Tuple(Term.I(1), before), Term.Tuple(Term.I(2), after));
            Term items = Term.List(Term.Tuple(entry, before));
            Term count = Term.List(Term.I(3));
            Term input = Term.Tuple(
                state,
                count,
                items,
                keys
            );
            Term added = Term.Tuple(
                state,
                Term.List(Term.I(4)),
                Term.List(Term.Tuple(entry, Term.Tuple(before, after))),
                keys
            );
            Term erased = Term.Tuple(
                state,
                count,
                items,
                Term.List(Term.Tuple(Term.I(2), after))
            );
            Term add = Term.Tuple(Term.A("add"), after);
            Term erase = Term.Tuple(Term.A("erase"), Term.I(1));
            Term unequal = Term.Tuple(Term.A("erase"), new FloatTerm(1.0));
            Term unknown = Term.Tuple(Term.A("unknown"), after);
            Term malformed = Term.Tuple(
                state,
                count,
                items,
                Term.List(before)
            );

            await Check(add, input, added);
            await Check(erase, input, erased);
            await Check(unequal, input, Term.Tuple(Term.A("outside"), unequal, input));
            await Check(unknown, input, Term.Tuple(Term.A("outside"), unknown, input));
            await Check(erase, malformed, Term.Tuple(Term.A("outside"), erase, malformed));
            await Check(add, before, Term.Tuple(Term.A("outside"), add, before));
            Term appended = Term.Tuple(
                state,
                count,
                Term.List(Term.Tuple(Term.Tuple(entry, before), after)),
                keys
            );
            await Check(
                add,
                input,
                appended,
                "drop_event"
            );
            await Check(
                erase,
                input,
                erased,
                "drop_event"
            );
            await Check(
                unequal,
                input,
                input,
                "drop_event"
            );
            await Check(
                erase,
                malformed,
                malformed,
                "drop_event"
            );
            await Check(
                unknown,
                input,
                input,
                "drop_event"
            );

            return Nil.Value;

            async Task Check(
                Term evt,
                Term receiver,
                Term expected,
                string function = "dispatch"
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

    private static void CheckEntry(byte[] beam, string function, bool carriesPayload)
    {
        object code = Code(beam);
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int entry = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
            && atoms[Value(Operands(instruction)[1])] == function);
        if (entry < 0)
        {
            throw new InvalidOperationException(WrongDispatch);
        }

        object[] body = instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        int arity = Array.FindIndex(body, instruction => Opcode(instruction) == SelectTupleArity);
        int select = Array.FindIndex(body, instruction => Opcode(instruction) == SelectValue);
        if (arity < 0 || select <= arity
            || body.Take(select).Any(instruction => Opcode(instruction) is Allocate or AllocateHeap or IsTaggedTuple)
            || !body.Skip(select + 1).Any(instruction => Opcode(instruction) == IsTaggedTuple))
        {
            throw new InvalidOperationException(WrongDispatch);
        }

        var fields = body.Skip(arity + 1).Take(select - arity - 1)
            .Where(instruction => Opcode(instruction) == GetTupleElement)
            .Select(Operands).ToArray();
        if (!fields.Select(operands => Value(operands[1])).SequenceEqual(carriesPayload ? [0, 1] : [0])
            || carriesPayload && (Property(fields[1][2], "Kind") is not "x" || Value(fields[1][2]) != 0))
        {
            throw new InvalidOperationException(WrongDispatch);
        }
    }

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands");

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
