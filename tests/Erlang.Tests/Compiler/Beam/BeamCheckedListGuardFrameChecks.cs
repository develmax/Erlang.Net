using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCheckedListGuardFrameChecks
{
    internal const string TestName = "compiler/beam/localizes-a-checked-list-guard-frame";
    private const string WrongFrame = "The guarded list clause used the wrong frame or a fallible guarded operation.";
    private const string WrongValue = "Localizing the guarded list clause changed the selected key.";
    private const string WrongTails = "Repeated tail checks must preserve the caller's original badmatch value.";
    private const string WrongPairs = "Cons projections must retain both heads and the original rejection route.";
    private const string Fixture = "beam_checked_list_guard_frame_probe.erl";
    private const string Source = """
        -module(beam_checked_list_guard_frame_probe).
        -export([match/2, barrier/2, mode/2, read_tails/3, read_checked_head/3, observed/1]).
        -export([read_pair/3, split_matches/3]).
        -record(bucket, {count, marker, shift, extra}).
        
        match(#bucket{count = Count} = State, Spec) ->
            case Spec of
                [{Head,_,_}] when is_tuple(Head), tuple_size(Head) > 2 ->
                    Key = element(2, Head),
                    case classify(Key) of
                        false -> [select_key(State, Key)];
                        true -> erlang:put(1, Count)
                    end;
                _ -> erlang:put(1, Count)
            end;
        match(OldState, Spec) ->
            State = convert(OldState),
            match(State, Spec).
        
        barrier(#bucket{count = Count} = State, Spec) ->
            case Spec of
                [{Head,_,_}] when is_tuple(Head), tuple_size(Head) > 2 ->
                    Key = element(2, Head),
                    case classify(Key) of
                        false -> [select_key(State, Key), Count];
                        true -> erlang:put(1, Count)
                    end;
                _ -> erlang:put(1, Count)
            end;
        barrier(OldState, Spec) ->
            State = convert(OldState),
            barrier(State, Spec).
        
        classify(Key) -> erlang:get(Key).
        select_key(#bucket{count = Count}, Key) -> erlang:put(selected, {Count, Key}), Count + 1.
        convert({bucket, Count, Marker, Shift}) ->
            #bucket{count = Count, marker = Marker, shift = Shift}.
        mode(Key, Value) -> erlang:put(Key, Value).

        read_tails(State, Stack, Value) ->
            [_, _, _ | Tail] = Stack,
            Result = observe(State, Value),
            {State, Tail, Result}.

        read_checked_head(State, Stack, Value) ->
            [required, _, _ | Tail] = Stack,
            Result = observe(State, Value),
            {State, Tail, Result}.

        read_pair(State, Stack, Keep) ->
            [A, B] = Stack,
            Result = observe(State, A),
            {B, Result, Keep}.

        split_matches(State, Stack, Keep) ->
            [A | Rest] = Stack,
            Result = observe(State, A),
            [B | Tail] = Rest,
            {B, Tail, Result, Keep}.

        observe(State, Value) ->
            erlang:put(State, Value),
            Value.

        observed(State) -> erlang:get(State).
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Fixture);
        CheckFrames(beam);
        var module = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term key = Term.A("key");
            Term spare = Term.A("spare");
            Term record = Term.Tuple(
                Term.A("bucket"),
                Term.I(3),
                spare,
                spare,
                spare
            );
            Term pattern = Term.Tuple(Term.A("tag"), key, spare);
            Term spec = Term.List(Term.Tuple(pattern, Nil.Value, Nil.Value));
            _ = await module.Invoke(
                context,
                "mode",
                key,
                Term.A("false")
            );
            if (!Term.List(Term.I(4)).Equals(await module.Invoke(
                context,
                "match",
                record,
                spec
            )))
            {
                throw new InvalidOperationException(WrongValue);
            }

            Term pending = Term.A("pending");
            Term observed = Term.A("observed");
            Term improper = new Cons(spare, new Cons(spare, new Cons(spare, pending)));
            if (!Term.Tuple(key, pending, observed).Equals(await module.Invoke(
                context,
                "read_tails",
                key,
                improper,
                observed
            )))
            {
                throw new InvalidOperationException(WrongTails);
            }

            await Reject("read_tails", Term.List(spare));
            await Reject("read_tails", new Cons(spare, pending));
            await Reject("read_checked_head", improper);
            if (!observed.Equals(await module.Invoke(context, "observed", key)))
            {
                throw new InvalidOperationException(WrongTails);
            }

            if (!Term.Tuple(pending, spare, observed).Equals(await module.Invoke(
                context,
                "read_pair",
                key,
                Term.List(spare, pending),
                observed
            )))
            {
                throw new InvalidOperationException(WrongPairs);
            }

            await Reject("read_pair", Term.List(pending));
            await Reject("read_pair", Term.List(pending, spare, spare));
            if (!spare.Equals(await module.Invoke(context, "observed", key)))
            {
                throw new InvalidOperationException(WrongPairs);
            }

            await Reject("split_matches", Term.List(pending), Nil.Value);
            if (!pending.Equals(await module.Invoke(context, "observed", key)))
            {
                throw new InvalidOperationException(WrongPairs);
            }

            return Nil.Value;

            async Task Reject(string function, Term stack, Term? rejected = null)
            {
                try
                {
                    _ = await module.Invoke(
                        context,
                        function,
                        key,
                        stack,
                        pending
                    );
                    throw new InvalidOperationException(WrongTails);
                }
                catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                    && error.Reason.Equals(Term.Tuple(Term.A("badmatch"), rejected ?? stack)))
                {
                }
            }
        });
    }

    private static void CheckFrames(byte[] beam)
    {
        object code = Code(beam);
        object[] instructions = Children(code, "Instructions").ToArray();
        string[] atoms = Children(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] match = Body("match");
        object[] barrier = Body("barrier");
        int allocate = Array.FindIndex(match, instruction => Opcode(instruction) == Allocate);
        if (allocate < 0 || !Values(match[allocate]).SequenceEqual([3, 3])
            || !match.Take(allocate).Any(instruction => Opcode(instruction) == BifOne && Values(instruction).First() == 0)
            || !match.Take(allocate).Any(instruction => Opcode(instruction) == BifTwo && Values(instruction).First() == 0)
            || !match.Any(instruction => Opcode(instruction) == TrimFrame && Values(instruction).SequenceEqual([3, 0]))
            || !match.Any(instruction => Opcode(instruction) == TailExternalCall)
            || barrier.Any(instruction => Opcode(instruction) == TrimFrame && Values(instruction).SequenceEqual([3, 0])))
        {
            throw new InvalidOperationException(WrongFrame);
        }

        object[] tails = Body("read_tails");
        int frame = Array.FindIndex(tails, instruction => Opcode(instruction) is Allocate or AllocateHeap);
        int badMatch = Array.FindIndex(tails, instruction => Opcode(instruction) == BadMatch);
        if (frame < 0 || badMatch < 0 || badMatch >= frame
            || tails.Take(badMatch).Count(instruction => Opcode(instruction) == GetTail) != 3)
        {
            throw new InvalidOperationException(WrongTails);
        }

        object[] pairs = Body("read_pair");
        int pairedFrame = Array.FindIndex(pairs, instruction => Opcode(instruction) is Allocate or AllocateHeap);
        if (pairedFrame < 0 || pairs.Take(pairedFrame).Count(instruction => Opcode(instruction) == GetList) != 2
            || !pairs.Take(pairedFrame).Any(instruction => Opcode(instruction) == IsNil))
        {
            throw new InvalidOperationException(WrongPairs);
        }

        object[] Body(string name)
        {
            int entry = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)Value(Children(instruction, "Operands").ElementAt(1))] == name
            );

            return instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");

    private static IEnumerable<int> Values(object instruction) => Children(instruction, "Operands").Select(operand => (int)Value(operand));

    private static IEnumerable<object> Children(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
