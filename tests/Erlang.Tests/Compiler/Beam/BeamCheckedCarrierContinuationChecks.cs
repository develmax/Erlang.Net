using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCheckedCarrierContinuationChecks
{
    internal const string TestName = "compiler/beam/checked-carrier-continuation";
    private const string WrongFrame = "Only the checked tuple, outer value and early projection should cross the first call.";
    private const string WrongSchedule = "Keep X0 rooted through deferred projections, then save it in the dead carrier slot.";
    private const string MissingEffect = "Keep the effect of a construction whose value is unused.";

    private const string Source = """
        -module(beam_checked_carrier_continuation).
        -export([run/3, blocked/3]).
        -record(state, {parent}).
        -record(saved, {ticket, subject, m, f, args}).

        run({attach, M, F, Args, Subject}, _From, State) ->
            Ticket = beam_checked_carrier_target:start(kind, Subject),
            Saved = #saved{ticket=Ticket, subject=Subject, m=M, f=F, args=Args},
            beam_checked_carrier_target:commit({receipt,Ticket}, Saved),
            Reply = Ticket,
            {reply, Reply, State};
        run(stop, {Parent,_}, #state{parent=Parent}=State) ->
            {stop,normal,ok,State};
        run(Request, From, State) ->
            {ignored,Request,From,State}.

        blocked({attach, M, F, Args, Subject}, _From, State) ->
            Ticket = beam_checked_carrier_target:start(kind, Subject),
            _Unused = {ignored,beam_checked_carrier_target:touch(M)},
            Saved = #saved{ticket=Ticket, subject=Subject, m=M, f=F, args=Args},
            beam_checked_carrier_target:commit({receipt,Ticket}, Saved),
            {reply, Ticket, State};
        blocked(stop, {Parent,_}, #state{parent=Parent}=State) ->
            {stop,normal,ok,State};
        blocked(Request, From, State) ->
            {ignored,Request,From,State}.
        """;

    internal static void Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, "beam_checked_carrier_continuation.erl"));
        object[] instructions = Items(code, "Instructions");
        object[] functions = Items(code, "Functions");
        object[] arm = Body("run");
        int first = Array.FindIndex(arm, instruction => Opcode(instruction) == ExternalCall);
        object? frame = arm.FirstOrDefault(instruction => Opcode(instruction) == Allocate);
        if (frame is null || !Items(frame, "Operands").Select(Number).SequenceEqual([3, 3])
            || first < 0 || arm[..first].Count(instruction => Opcode(instruction) == GetTupleElement) != 1)
        {
            throw new InvalidOperationException(WrongFrame);
        }

        int second = Array.FindIndex(arm, first + 1, instruction => Opcode(instruction) == ExternalCall);
        if (second <= first)
        {
            throw new InvalidOperationException(WrongSchedule);
        }

        object[] between = arm[(first + 1)..second];
        int lastConstructor = Array.FindLastIndex(between, instruction => Opcode(instruction) == PutTuple);
        int trim = Array.FindIndex(between, instruction => Opcode(instruction) == TrimFrame);
        if (between.Length == 0 || Opcode(between[0]) != TestHeap
            || !Items(between[0], "Operands").Select(Number).SequenceEqual([10, 1])
            || between.Count(instruction => Opcode(instruction) == GetTupleElement
                && IsRegister(Items(instruction, "Operands")[0], "y", 2)) != 3
            || lastConstructor < 0 || trim != lastConstructor + 2
            || Opcode(between[lastConstructor + 1]) != Move
            || Items(between[lastConstructor + 1], "Operands") is not [var source, var destination]
            || !IsRegister(source, "x", 0) || !IsRegister(destination, "y", 2))
        {
            throw new InvalidOperationException(WrongSchedule);
        }

        if (Body("blocked").Count(instruction => Opcode(instruction) == ExternalCall) != 3)
        {
            throw new InvalidOperationException(MissingEffect);
        }

        object[] Body(string name)
        {
            object function = functions.Single(candidate => (string)Property(candidate, "Name") == name);
            int start = (int)Property(function, "FirstInstructionIndex");
            int end = (int)Property(function, "EndInstructionIndex");

            return instructions[start..end];
        }
    }

    private static bool IsRegister(object operand, string kind, int number) =>
        (string)Property(operand, "Kind") == kind && Number(operand) == number;

    private static int Number(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string property) =>
        ((IEnumerable)Property(owner, property)).Cast<object>().ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");
}
