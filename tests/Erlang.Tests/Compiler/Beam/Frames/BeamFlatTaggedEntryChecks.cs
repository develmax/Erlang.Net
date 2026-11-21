using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamFlatTaggedEntryChecks
{
    internal const string TestName = "compiler/beam/shares-tuple-projections-before-tagged-entry-frames";
    private const string Fixture = "beam_flat_tagged_entry.erl";
    private const string WrongGuard = "The grouped guard must test its secondary record and return before allocating a frame.";
    private const string Missing = "The compiled tagged entry function is missing.";
    private const string WrongDispatch = "The shared field reads and tag switch must precede the branch frames.";
    private const string WrongBucket = "The repeated constructor must choose its atom alternative before allocating a frame.";
    private const string WrongBucketResult = "The repeated constructor changed a selected result, original carrier, or fallback.";
    private const string WrongRead = "A field needed in both disjoint arms should be projected once at their common entry.";

    internal const string Source = """
        -module(beam_flat_tagged_entry).
        -export([dispatch/2, repeat_tag/2, guarded/2]).
        -record(state, {key}).

        dispatch({alpha, Stamp, Field, Payload}, State) ->
            beam_flat_entry_target:observe(Stamp, Field),
            {kept, Payload, State};
        dispatch({zulu, Stamp, process, Payload}, State) ->
            beam_flat_entry_target:observe(Stamp, Payload),
            {kept, Payload, State};
        dispatch({short, Stamp}, State) ->
            {simple, Stamp, State};
        dispatch(Other, State) ->
            {fallback, Other, State}.

        repeat_tag({alpha, Stamp, short}, State) ->
            {short, Stamp, State};
        repeat_tag({alpha, Stamp, long}, State) ->
            {long, Stamp, State};
        repeat_tag({single, Stamp}, State) ->
            {single, Stamp, State};
        repeat_tag(Other, State) ->
            {fallback, Other, State}.

        guarded({halt, Key, Reason}, State) when Key == State#state.key ->
            {halted, Reason, State};
        guarded({old, Value}, State) ->
            {legacy, Value, State};
        guarded(Other, State) ->
            {fallback, Other, State}.
        """;

    private const string BucketFixture = "beam_repeated_tagged_entry.erl";
    private const string BucketSource = """
        -module(beam_repeated_tagged_entry).
        -export([bucket/2, guard_order/2, same_name/2]).

        bucket({alpha, left, Payload, kept} = Original, State) ->
            remember(Payload),
            {keyed, Original, State};
        bucket({zulu, file, Payload, _}, State) ->
            {file, Payload, State};
        bucket({alpha, Other, Payload, kept}, State) ->
            {retry, Other, Payload, State};
        bucket({zulu, tty, Payload, _}, State) ->
            {tty, Payload, State};
        bucket({short, Payload}, State) ->
            {short, Payload, State};
        bucket(Other, State) ->
            {outside, Other, State}.

        guard_order({alpha, left, Payload}, Permit) when Permit =:= allow -> {first, Payload};
        guard_order({alpha, left, Payload}, _) -> {second, Payload};
        guard_order({zulu, right, Payload}, _) -> {third, Payload};
        guard_order({short, Payload}, _) -> {short, Payload};
        guard_order(Other, _) -> {outside, Other}.

        same_name({alpha, left, Value}, Value) -> same;
        same_name({alpha, right, _}, _) -> right;
        same_name({zulu, file, _}, _) -> file;
        same_name({short, _}, _) -> short;
        same_name(Other, State) -> {outside, Other, State}.

        remember(Payload) -> erlang:put(repeated_tagged_entry_payload, Payload).
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
            && atoms[Value(Operands(instruction)[1])] == "dispatch" && Value(Operands(instruction)[2]) == 2
        );
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

        int[] shared = body.Skip(arity + 1).Take(selection - arity - 1)
            .Where(instruction => Opcode(instruction) == GetTupleElement).Select(Operands)
            .Where(operands => Property(operands[0], "Kind") is "x" && Value(operands[0]) == 0)
            .Select(operands => Value(operands[1])).ToArray();
        if (!shared.SequenceEqual([0, 1, 2, 3]))
        {
            throw new InvalidOperationException(WrongRead);
        }

        int guard = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
            && atoms[Value(Operands(instruction)[1])] == "guarded" && Value(Operands(instruction)[2]) == 2
        );
        if (guard < 0)
        {
            throw new InvalidOperationException(Missing);
        }

        object[] firstArm = instructions.Skip(guard + 1).TakeWhile(instruction => Opcode(instruction) != Return).ToArray();
        if (!firstArm.Any(instruction => Opcode(instruction) == IsTaggedTuple)
            || !firstArm.Any(instruction => Opcode(instruction) == NumericEqualTest)
            || firstArm.Any(instruction => Opcode(instruction) is Allocate or AllocateHeap))
        {
            throw new InvalidOperationException(WrongGuard);
        }

        return CheckRepeatedBuckets();
    }

    private static async Task CheckRepeatedBuckets()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(BucketSource, BucketFixture);
        object code = Code(beam);
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int entry = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
            && atoms[Value(Operands(instruction)[1])] == "bucket");
        object[] body = instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        int arity = Array.FindIndex(body, instruction => Opcode(instruction) == SelectTupleArity);
        int tag = Array.FindIndex(body, instruction => Opcode(instruction) == SelectValue);
        int selector = tag < 0 ? -1 : Array.FindIndex(body, tag + 1, instruction => Opcode(instruction) == SelectValue);
        if (entry < 0 || arity < 0 || tag <= arity || selector <= tag
            || body.Take(selector).Any(instruction => Opcode(instruction) is Allocate or AllocateHeap)
            || !body.Skip(selector + 1).Any(instruction => Opcode(instruction) is Allocate or AllocateHeap))
        {
            throw new InvalidOperationException(WrongBucket);
        }

        BeamModule module = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term alpha = Term.A("alpha");
            Term zulu = Term.A("zulu");
            Term left = Term.A("left");
            Term right = Term.A("right");
            Term value = Term.A("payload");
            Term state = Term.A("state");
            Term kept = Term.A("kept");
            Term selected = Term.Tuple(
                alpha,
                left,
                value,
                kept
            );
            Term missingCommon = Term.Tuple(
                alpha,
                left,
                value,
                state
            );
            Term guarded = Term.Tuple(alpha, left, value);
            var cases = new (string Function, Term Input, Term State, Term Expected)[]
            {
                ("bucket", selected, state, Term.Tuple(Term.A("keyed"), selected, state)),
                ("bucket", Term.Tuple(
                    alpha,
                    right,
                    value,
                    kept
                ), state, Term.Tuple(
                    Term.A("retry"),
                    right,
                    value,
                    state
                )),
                ("bucket", Term.Tuple(
                    zulu,
                    Term.A("tty"),
                    value,
                    kept
                ), state, Term.Tuple(Term.A("tty"), value, state)),
                ("bucket", missingCommon, state, Term.Tuple(Term.A("outside"), missingCommon, state)),
                ("bucket", alpha, state, Term.Tuple(Term.A("outside"), alpha, state)),
                ("guard_order", guarded, Term.A("allow"), Term.Tuple(Term.A("first"), value)),
                ("guard_order", guarded, state, Term.Tuple(Term.A("second"), value)),
                ("same_name", guarded, value, Term.A("same")),
                ("same_name", guarded, state, Term.Tuple(Term.A("outside"), guarded, state))
            };
            foreach (var (function, input, argument, expected) in cases)
            {
                if (!expected.Equals(await module.Invoke(
                    context,
                    function,
                    input,
                    argument
                )))
                {
                    throw new InvalidOperationException(WrongBucketResult);
                }
            }

            return Nil.Value;
        });
    }

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands");

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
