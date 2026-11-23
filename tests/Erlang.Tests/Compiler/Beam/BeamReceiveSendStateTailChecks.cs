using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamReceiveSendStateTailChecks
{
    internal const string TestName = "compiler/beam/retains-register-operands-for-sends-and-selective-receive";
    private const string Fixture = "beam_receive_send_state_tail.erl";
    private const string WrongSend = "The discarded send result unnecessarily spilled the receiver.";
    private const string WrongReceive = "The direct receive recursion did not consume and forward its match in order.";
    private const string Missing = "A compiled fixture function is missing.";

    private const string Source = """
        -module(beam_receive_send_state_tail).
        -export([notify/1, negative/1, loop/1, guarded_loop/1, relay/1, changed_recipient/1]).

        notify(Destination) ->
            Destination ! ready,
            sent.

        negative(Destination) ->
            Destination ! ready.

        loop(State) ->
            receive
                {update, Payload} ->
                    Updated = step(Payload, State),
                    loop(Updated);
                finish ->
                    done
            end.

        guarded_loop(State) ->
            receive
                {update, Payload} when is_integer(Payload) ->
                    Updated = step(Payload, State),
                    guarded_loop(Updated);
                finish ->
                    done
            end.

        relay(Pid) ->
            receive
                Message -> Pid ! Message, relay(Pid)
            end.

        changed_recipient(Pid) ->
            receive
                Message -> Pid ! Message, changed_recipient(Message)
            end.

        step(Payload, {Pid, Counter}) ->
            Pid ! {observed, Counter, Payload},
            {Pid, Counter+1}.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();

        object[] send = Operations("notify");
        object? frame = send.FirstOrDefault(instruction => Opcode(instruction) == Allocate);
        if (frame is null || Value(Operands(frame)[0]) != 0
            || send.Count(instruction => Opcode(instruction) == Send) != 1
            || send.Any(instruction => Operands(instruction).Any(operand => Property(operand, "Kind").Equals("y"))))
        {
            throw new InvalidOperationException(WrongSend);
        }

        object[] receive = Operations("loop");
        int remove = Array.FindIndex(receive, instruction => Opcode(instruction) == RemoveMessage);
        int project = Array.FindIndex(receive, instruction => Opcode(instruction) == GetTupleElement);
        if (remove < 0 || project <= remove
            || receive.Count(instruction => Opcode(instruction) == TrimFrame) != 1
            || !receive.Any(instruction => Opcode(instruction) == TailLocalCallWithFrame && Value(Operands(instruction)[2]) == 0)
            || !receive.Any(instruction => Opcode(instruction) == Wait))
        {
            throw new InvalidOperationException(WrongReceive);
        }

        object[] guarded = Operations("guarded_loop");
        if (!guarded.Any(instruction => Opcode(instruction) == IsInteger))
        {
            throw new InvalidOperationException(WrongReceive);
        }

        object[] relay = Operations("relay");
        if (!HasDirectRelay(relay) || HasDirectRelay(Operations("changed_recipient")))
        {
            throw new InvalidOperationException(WrongReceive);
        }

        return Task.CompletedTask;

        static bool HasDirectRelay(object[] body)
        {
            int message = Array.FindIndex(body, instruction => Opcode(instruction) == LoopReceive);

            return message >= 0 && message + 2 < body.Length
                && Opcode(body[message + 1]) == Move && Opcode(body[message + 2]) == RemoveMessage
                && body.Where(instruction => Opcode(instruction) == Allocate)
                    .Select(instruction => Value(Operands(instruction)[0])).ToArray() is [1]
                && body.Count(instruction => Opcode(instruction) == Send) == 1;
        }

        object[] Operations(string name)
        {
            int entry = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Operands(instruction)[1])] == name);

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
