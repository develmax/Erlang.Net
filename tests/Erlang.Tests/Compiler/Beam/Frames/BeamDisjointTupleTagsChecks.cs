using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamDisjointTupleTagsChecks
{
    internal const string TestName = "compiler/beam/selects-disjoint-tuple-tags-before-arm-frames";
    private const string Fixture = "beam_disjoint_tuple_tags.erl";
    private const string Missing = "The compiled disjoint tuple entry is missing.";
    private const string WrongDispatch = "The arity arm should project only the tag before dispatching and allocating.";
    private const string WrongResult = "Tuple entry dispatch changed a selected arm or fallback.";
    private const string WrongEffect = "Tuple entry dispatch changed the selected arm's effect.";
    private const string Source = """
        -module(beam_disjoint_tuple_tags).
        -export([dispatch/2, overlap/2, closures/2, update/2, checked_update/2, observed/0]).
        -record(state, {value}).

        dispatch({drop, _, allowed}, State) ->
            remember(State),
            {dropped, State};
        dispatch({take, Item, _}, State) ->
            remember(Item),
            {taken, Item, State};
        dispatch({pair, Item}, State) ->
            {pair, Item, State};
        dispatch(Other, State) ->
            {outside, Other, State}.

        overlap({take, Item, _}, State) when State =:= allow ->
            {allowed, Item};
        overlap({take, _, _}, State) ->
            {denied, State};
        overlap({drop, _, _}, State) ->
            {dropped, State};
        overlap({pair, Item}, State) ->
            {pair, Item, State};
        overlap(Other, State) ->
            {outside, Other, State}.

        closures({drop, _, _}, State) ->
            State;
        closures({take, Item, _}, State) ->
            Read = fun () -> {Item, State} end,
            Read();
        closures({pair, Item}, State) ->
            {pair, Item, State};
        closures(Other, State) ->
            {outside, Other, State}.

        update({drop, _, _}, State) ->
            State;
        update({take, Item, _}, State) ->
            State#state{value=Item};
        update({pair, Item}, State) ->
            {pair, Item, State};
        update(Other, State) ->
            {outside, Other, State}.

        checked_update(Event, State) ->
            try update(Event, State)
            catch error:Reason -> Reason
            end.

        remember(Item) -> erlang:put(disjoint_tuple_tag_effect, Item).
        observed() -> erlang:get(disjoint_tuple_tag_effect).
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
            Term item = Term.A("payload");
            Term state = Term.A("state");
            Term allowed = Term.A("allowed");
            Term taken = Term.Tuple(Term.A("take"), item, allowed);
            Term dropped = Term.Tuple(Term.A("drop"), item, allowed);
            Term rejected = Term.Tuple(Term.A("drop"), item, state);
            Term pair = Term.Tuple(Term.A("pair"), item);
            Term record = Term.Tuple(state, allowed);
            var cases = new (string Function, Term Input, Term Argument, Term Expected, Term Effect)[]
            {
                (
                    "dispatch",
                    taken,
                    state,
                    Term.Tuple(Term.A("taken"), item, state),
                    item
                ),
                (
                    "dispatch",
                    dropped,
                    state,
                    Term.Tuple(Term.A("dropped"), state),
                    state
                ),
                (
                    "dispatch",
                    rejected,
                    state,
                    Term.Tuple(Term.A("outside"), rejected, state),
                    state
                ),
                (
                    "dispatch",
                    pair,
                    state,
                    Term.Tuple(Term.A("pair"), item, state),
                    state
                ),
                (
                    "dispatch",
                    state,
                    item,
                    Term.Tuple(Term.A("outside"), state, item),
                    state
                ),
                (
                    "overlap",
                    taken,
                    Term.A("allow"),
                    Term.Tuple(allowed, item),
                    state
                ),
                (
                    "overlap",
                    taken,
                    state,
                    Term.Tuple(Term.A("denied"), state),
                    state
                ),
                (
                    "closures",
                    taken,
                    state,
                    Term.Tuple(item, state),
                    state
                ),
                (
                    "update",
                    taken,
                    record,
                    Term.Tuple(state, item),
                    state
                ),
                (
                    "checked_update",
                    taken,
                    state,
                    Term.Tuple(Term.A("badrecord"), state),
                    state
                )
            };
            foreach (var (function, input, argument, expected, effect) in cases)
            {
                if (!expected.Equals(await module.Invoke(
                    context,
                    function,
                    input,
                    argument
                )))
                {
                    throw new InvalidOperationException(WrongResult);
                }

                if (!effect.Equals(await module.Invoke(context, "observed")))
                {
                    throw new InvalidOperationException(WrongEffect);
                }
            }

            return Nil.Value;
        });
    }

    private static void CheckEntry(byte[] beam)
    {
        object code = Code(beam);
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int start = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
            && atoms[Value(Operands(instruction)[1])] == "dispatch");
        if (start < 0)
        {
            throw new InvalidOperationException(Missing);
        }

        object[] body = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        int arity = Array.FindIndex(body, instruction => Opcode(instruction) == SelectTupleArity);
        int selection = Array.FindIndex(body, instruction => Opcode(instruction) == SelectValue);
        if (arity < 0 || selection <= arity || body.Take(selection).Any(instruction => Opcode(instruction) is Allocate or AllocateHeap))
        {
            throw new InvalidOperationException(WrongDispatch);
        }

        int[] projections = body.Skip(arity + 1).Take(selection - arity - 1)
            .Where(instruction => Opcode(instruction) == GetTupleElement)
            .Select(instruction => Value(Operands(instruction)[1]))
            .ToArray();
        if (!projections.SequenceEqual([0]))
        {
            throw new InvalidOperationException(WrongDispatch);
        }
    }

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands");

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
