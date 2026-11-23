using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamAtomCompanionEntryChecks
{
    internal const string TestName = "compiler/beam/partitions-atom-entries-with-companion-tests";
    internal const string FrameTestName = "compiler/beam/localizes-guarded-receive-recurrence-frames";
    private const string Fixture = "beam_atom_companion_entry_probe.erl";
    private const string MissingSelector = "Atom entry arms with shared companion tests must use one selector.";
    private const string ReorderedTuple = "Mixed constructor entries must retain their own constructor partition.";
    private const string FrameFixture = "beam_guarded_receive_recurrence_probe.erl";
    private const string FrameFailure = "Guarded recurrence and receive must allocate only after their own live values are known.";
    private const string Source = """
        -module(beam_atom_companion_entry_probe).
        -export([dispatch/3, mixed/2, scalar/2]).

        dispatch(idle,0,[]) ->
            receive tick -> report(idle); Other -> report(Other)
            after 0 -> report(no_message) end;
        dispatch(active,0,[]) ->
            report(active);
        dispatch(done,0,[]) ->
            report(done);
        dispatch(Mode,Count,Items) ->
            {Mode,Count,Items}.

        mixed(left,0) -> l;
        mixed(right,0) -> r;
        mixed({Kind,Value},N) -> {Kind,Value,N};
        mixed(Other,N) -> {Other,N}.

        scalar(info,warning) -> informational;
        scalar(error,warning) -> failure;
        scalar(_,Type) -> Type.

        report(Value) -> {event,Value}.
        """;

    private const string FrameSource = """
        -module(beam_guarded_receive_recurrence_probe).
        -export([run/2, idle/0]).

        idle() ->
            state(idle, undefined, 0, []).

        run(Limit, Work) ->
            state(busy, Limit, 0, Work).

        state(idle, _, 0, []) ->
            receive
                ready -> finish();
                Unexpected ->
                    erlang:display({?MODULE, unexpected, Unexpected}),
                    state(idle, undefined, 0, [])
            after 60_000 ->
                erlang:hibernate(?MODULE, idle, [])
            end;
        state(busy, Limit, 0, []) ->
            state(drain, Limit, 0, erlang:ports());
        state(drain, _, 0, []) ->
            finish();
        state(Mode, Limit, Count, [Job | Rest]) when Count < Limit ->
            Started = score(Job),
            state(Mode, Limit, Count + Started, Rest);
        state(Mode, Limit, Count, Jobs) ->
            receive
                ready ->
                    state(Mode, Limit, Count, Jobs);
                Ack when is_pid(Ack), Count > 0 ->
                    state(Mode, Limit, Count - 1, Jobs);
                Unexpected ->
                    erlang:display({?MODULE, unexpected, Unexpected}),
                    state(Mode, Limit, Count, Jobs)
            end.

        finish() -> done.

        score(Job) ->
            case Job =:= acknowledged of
                true -> 1;
                false -> 0
            end.
        """;

    internal static void Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] dispatch = Instructions(code, "scalar");
        object[] mixed = Instructions(code, "mixed");
        if (dispatch.Length == 0 || Opcode(dispatch[0]) != SelectValue
            || dispatch.Count(instruction => Opcode(instruction) == ExactEqualTest) != 2)
        {
            throw new InvalidOperationException(MissingSelector);
        }
        if (mixed.Length == 0 || Opcode(mixed[0]) == SelectValue)
        {
            throw new InvalidOperationException(ReorderedTuple);
        }
    }

    internal static void RunFrames()
    {
        object code = Code(CodeGeneration.CompileBeamModule(FrameSource, FrameFixture));
        object[] state = Instructions(code, "state");
        int cons = Array.FindIndex(state, instruction => Opcode(instruction) == IsNonemptyList);
        int receive = Array.FindLastIndex(state, instruction => Opcode(instruction) == LoopReceive);
        if (cons < 0 || cons + 3 >= state.Length
            || Opcode(state[cons + 1]) != GetList || Opcode(state[cons + 2]) != IsLess
            || Opcode(state[cons + 3]) != Allocate || Value(state[cons + 3], 0) != 4
            || Value(state[cons + 3], 1) != 6 || Value(state[cons], 1) != 3
            || receive < 0 || receive >= state.Length - 1 || Opcode(state[receive + 1]) != ExactEqualTest
            || Value(state[receive + 1], 1) != 0)
        {
            throw new InvalidOperationException(FrameFailure);
        }
    }

    private static int Value(object instruction, int operand) =>
        (int)(BigInteger)Property(Items(instruction, "Operands").ElementAt(operand), "Value");

    private static object[] Instructions(object code, string function)
    {
        string?[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string?>().ToArray();
        object[] body = Items(code, "Instructions").ToArray();
        int start = Array.FindIndex(
            body,
            instruction => Opcode(instruction) == FunctionInfo
            && atoms[(int)(BigInteger)Property(Items(instruction, "Operands").ElementAt(1), "Value")] == function
        );

        return body.Skip(start + 2)
            .TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
            .ToArray();
    }

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object container, string name) =>
        ((IEnumerable)Property(container, name)).Cast<object>();
}
