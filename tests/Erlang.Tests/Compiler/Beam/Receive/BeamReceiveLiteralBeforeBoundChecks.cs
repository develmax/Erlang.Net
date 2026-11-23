using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamReceiveLiteralBeforeBoundChecks
{
    internal const string TestName = "compiler/beam/checks-a-receive-literal-before-the-bound-field";
    private const string Fixture = "beam_receive_literal_before_bound.erl";
    private const string WrongOrder = "Expected tuple fields to be read before the literal and bound-field checks.";
    private const string WrongMailbox = "The receive changed a matching clause or the order of the remaining messages.";

    private const string Source = """
        -module(beam_receive_literal_before_bound).
        -export([fixed/1, alternative/1]).
        fixed(Key) ->
            receive {event, Key, stop} -> accepted
            after 0 -> timed_out
            end.
        alternative(Key) ->
            receive
                {event, Key, stop} -> accepted;
                {event, Key, Other} -> Other
            after 0 -> timed_out
            end.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        CheckInstructions(image);

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term tag = Term.A("event");
            Term stop = Term.A("stop");
            Term pause = Term.A("pause");
            Term key = Term.I(1);
            Term wrongLiteral = Term.Tuple(tag, key, pause);
            Term wrongKey = Term.Tuple(tag, new FloatTerm(1.0), stop);
            context.Mailbox.Send(wrongLiteral);
            context.Mailbox.Send(wrongKey);
            context.Mailbox.Send(Term.Tuple(tag, key, stop));
            if (!Term.A("accepted").Equals(await module.Invoke(context, "fixed", key))
                || !wrongLiteral.Equals(await context.ReceiveAsync(value => value, TimeSpan.Zero))
                || !wrongKey.Equals(await context.ReceiveAsync(value => value, TimeSpan.Zero))
                || !Term.A("timed_out").Equals(await module.Invoke(context, "fixed", key)))
            {
                throw new InvalidOperationException(WrongMailbox);
            }

            context.Mailbox.Send(wrongLiteral);
            if (!pause.Equals(await module.Invoke(context, "alternative", key)))
            {
                throw new InvalidOperationException(WrongMailbox);
            }

            return Nil.Value;
        });
    }

    private static void CheckInstructions(byte[] image)
    {
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] all = Items(code, "Instructions").ToArray();
        int info = Array.FindIndex(all, instruction => Opcode(instruction) == FunctionInfo
            && Operands(instruction) is [_, var atom, _] && atoms[Value(atom)] == "fixed");
        if (info < 0)
        {
            throw new InvalidOperationException(WrongOrder);
        }

        object[] instructions = all.Skip(info + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        int peek = Array.FindIndex(instructions, instruction => Opcode(instruction) == LoopReceive);
        if (peek < 0 || instructions.Skip(peek + 1).Take(6).Select(Opcode).ToArray() is not
            [IsTaggedTuple, GetTupleElement, GetTupleElement, ExactEqualTest, ExactEqualTest, RemoveMessage]
            || Operands(instructions[peek + 2]).Select(Value).ToArray() is not [0, 1, 1]
            || Operands(instructions[peek + 3]).Select(Value).ToArray() is not [0, 2, 0]
            || (string)Property(Operands(instructions[peek + 4])[2], "Kind") != "atom"
            || (string)Property(Operands(instructions[peek + 5])[2], "Kind") != "y")
        {
            throw new InvalidOperationException(WrongOrder);
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
