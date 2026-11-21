using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamChildEntryTimedMonitorChecks
{
    internal const string TestName = "compiler/beam/shares-list-entry-and-trims-timed-monitor-frame";
    private const string Fixture = "beam_child_entry_and_timed_monitor.erl";
    private const string WrongEntry = "The two list lengths tripped a frame before their entry checks.";
    private const string WrongMonitor = "The timed receive did not bind and clear the fresh monitor marker on both paths.";
    private const string WrongConstructors = "The child specification used an intermediate copy for a register-owned field.";
    private const string Missing = "A compiled fixture function is missing.";

    private const string Source = """
        -module(beam_child_entry_and_timed_monitor).
        -export([start/2, init/1, stop/1, record_start/3, trigger/2, policy_key/1]).

        start(Mode, Options) ->
            Address = {local,fixture_parent},
            ?MODULE:record_start(Address,fixture_parent,[Mode,Options]).

        init([Options]) when is_list(Options) -> init([automatic,Options]);
        init([Mode,Options]) ->
            Policy = policy(Options),
            Flags = {different,12,35},
            First = fixed_child(engine,[Options],Policy,[adapter]),
            Second = named_child(logger,[],Policy),
            Third = named_child(worker,[Mode,Options],Policy),
            Children = [First,Second,Third],
            {activated,{Flags,Children}}.

        stop(Delay) ->
            case erlang:get(target) of
                Pid when is_pid(Pid) -> finish(Pid,Delay);
                _ -> absent
            end.

        finish(Pid, Delay) when Delay =:= 0 ->
            ?MODULE:trigger(Pid,direct),
            direct;
        finish(Pid, Delay) ->
            Ref = erlang:monitor(process,Pid),
            ?MODULE:trigger(Pid,observe),
            receive
                {'DOWN',Ref,process,Pid,_} -> skip_debug, observed
            after Delay ->
                erlang:demonitor(Ref,[flush]),
                {error,elapsed}
            end.

        policy(Options) -> ?MODULE:policy_key(Options).
        policy_key(Options) -> proplists:get_value(mode,Options,ambient).

        named_child(Id, Args, Policy) ->
            {Id,{Id,bootstrap,Args},Policy,110,fixture,[Id,controller]}.
        fixed_child(Id, Args, Policy, Modules) ->
            {Id,{Id,bootstrap,Args},Policy,110,fixture,[Id] ++ Modules}.

        record_start(Address,Tag,Args) -> {recorded,Address,Tag,Args}.

        trigger(Pid, Kind) ->
            self() ! {unrelated,Kind},
            case erlang:get(action) of send -> Pid ! finish; keep -> ok end.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();

        byte[] init = Operations("init", 1).Select(Opcode).ToArray();
        if (Array.LastIndexOf(init, GetList) >= Array.IndexOf(init, Allocate)
            || init.Count(opcode => opcode == GetList) != 2
            || !init.Contains(TrimFrame) || !init.Contains(TailLocalCall))
        {
            throw new InvalidOperationException(WrongEntry);
        }

        byte[] receive = Operations("finish", 2).Select(Opcode).ToArray();
        if (receive.Count(opcode => opcode == ReceiveMarkerClear) != 2
            || !receive.Contains(ReceiveMarkerReserve) || !receive.Contains(ReceiveMarkerBind)
            || !receive.Contains(ReceiveMarkerUse) || !receive.Contains(TrimFrame))
        {
            throw new InvalidOperationException(WrongMonitor);
        }

        byte[] named = Operations("named_child", 3).Select(Opcode).ToArray();
        byte[] fixedChild = Operations("fixed_child", 4).Select(Opcode).ToArray();
        byte[] start = Operations("start", 2).Select(Opcode).ToArray();
        if (named.Contains(Move) || fixedChild.Contains(Move)
            || Array.IndexOf(fixedChild, PutList) >= Array.IndexOf(fixedChild, PutTuple)
            || Array.IndexOf(start, PutList) >= Array.IndexOf(start, Move))
        {
            throw new InvalidOperationException(WrongConstructors);
        }

        return Task.CompletedTask;

        object[] Operations(string name, int arity)
        {
            int entry = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Operands(instruction)[1])] == name && Value(Operands(instruction)[2]) == arity
            );

            return entry >= 0
                ? instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray()
                : throw new InvalidOperationException(Missing);
        }
    }

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
