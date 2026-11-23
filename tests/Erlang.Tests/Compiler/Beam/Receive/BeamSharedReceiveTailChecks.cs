using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamSharedReceiveTailChecks
{
    internal const string TestName = "compiler/beam/shares-accepted-receive-tails-across-tuple-arities";
    private const string BrokenDispatch = "Expected one accepted receive tail after both tagged arities, with matching fields only.";
    private const string BrokenQueue = "The receive changed an accepted result, an unmatched message or its mailbox order.";
    private const string Source = """
        -module(beam_shared_receive_tail).
        -export([drain/1, guarded/1, payload/1, repeated/1]).

        drain(Key) ->
            receive
                {packet, Key, _Ignored} -> drain(Key);
                {stopped, Key} -> drain(Key);
                {failed, Key} -> drain(Key)
            after 0 ->
                drained
            end.

        guarded(Key) ->
            receive
                {packet, Key, Data} when Data =:= wanted -> guarded(Key);
                {stopped, Key} -> guarded(Key);
                {failed, Key} -> guarded(Key)
            after 0 ->
                guarded_timeout
            end.

        payload(Key) ->
            receive
                {packet, Key, Data} -> Data;
                {stopped, Key} -> payload(Key);
                {failed, Key} -> payload(Key)
            after 0 ->
                payload_timeout
            end.

        repeated(Key) ->
            receive
                {packet, Key, _Same, _Same} -> repeated(Key);
                {stopped, Key} -> repeated(Key);
                {failed, Key} -> repeated(Key)
            after 0 ->
                repeated_timeout
            end.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, "beam_shared_receive_tail.erl");
        RequireSharedDispatch(Code(image));

        BeamModule beam = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term key = Term.I(1);
            Term otherTag = Term.Tuple(Term.A("other"), key);
            Term otherKey = Term.Tuple(Term.A("stopped"), new FloatTerm(1.0));
            Term wrongArity = Term.Tuple(Term.A("packet"), key);
            context.Mailbox.Send(otherTag);
            context.Mailbox.Send(Term.Tuple(Term.A("packet"), key, Term.I(20)));
            context.Mailbox.Send(otherKey);
            context.Mailbox.Send(Term.Tuple(Term.A("stopped"), key));
            context.Mailbox.Send(wrongArity);
            context.Mailbox.Send(Term.Tuple(Term.A("failed"), key));
            Term drained = await beam.Invoke(context, "drain", key);
            if (!drained.Equals(Term.A("drained"))
                || !otherTag.Equals(await context.ReceiveAsync(value => value, TimeSpan.Zero))
                || !otherKey.Equals(await context.ReceiveAsync(value => value, TimeSpan.Zero))
                || !wrongArity.Equals(await context.ReceiveAsync(value => value, TimeSpan.Zero)))
            {
                throw new InvalidOperationException(BrokenQueue);
            }

            Term guardFailure = Term.Tuple(Term.A("packet"), key, Term.A("unwanted"));
            context.Mailbox.Send(guardFailure);
            context.Mailbox.Send(Term.Tuple(Term.A("packet"), key, Term.A("wanted")));
            Term guarded = await beam.Invoke(context, "guarded", key);
            if (!guarded.Equals(Term.A("guarded_timeout"))
                || !guardFailure.Equals(await context.ReceiveAsync(value => value, TimeSpan.Zero)))
            {
                throw new InvalidOperationException(BrokenQueue);
            }

            context.Mailbox.Send(Term.Tuple(Term.A("packet"), key, Term.I(20)));
            Term payload = await beam.Invoke(context, "payload", key);
            Term nonEqualFields = Term.Tuple(
                Term.A("packet"),
                key,
                Term.I(2),
                Term.I(3)
            );
            context.Mailbox.Send(nonEqualFields);
            context.Mailbox.Send(Term.Tuple(
                Term.A("packet"),
                key,
                Term.I(4),
                Term.I(4)
            ));
            Term repeated = await beam.Invoke(context, "repeated", key);
            if (!payload.Equals(Term.I(20)) || !repeated.Equals(Term.A("repeated_timeout"))
                || !nonEqualFields.Equals(await context.ReceiveAsync(value => value, TimeSpan.Zero))
                || await context.ReceiveAsync(value => value, TimeSpan.Zero) is not null)
            {
                throw new InvalidOperationException(BrokenQueue);
            }

            return Nil.Value;
        });
    }

    private static void RequireSharedDispatch(object code)
    {
        var functions = (IEnumerable)Property(code, "Functions");
        object function = functions.Cast<object>().Single(item => (string)Property(item, "Name") == "drain");
        int first = (int)Property(function, "FirstInstructionIndex");
        int end = (int)Property(function, "EndInstructionIndex");
        var instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>()
            .Skip(first).Take(end - first)
            .Select(instruction => (Instruction: instruction, Opcode: (byte)Property(Property(instruction, "Definition"), "Opcode")))
            .ToArray();
        int[] fields = instructions.Where(instruction => instruction.Opcode == GetTupleElement)
            .Select(instruction => ((IEnumerable)Property(instruction.Instruction, "Operands")).Cast<object>().ElementAt(1))
            .Select(operand => (int)(BigInteger)Property(operand, "Value"))
            .ToArray();
        if (!fields.AsSpan().SequenceEqual([0, 1, 0, 1])
            || instructions.Count(instruction => instruction.Opcode == RemoveMessage) != 1
            || instructions.Count(instruction => instruction.Opcode == TailLocalCallWithFrame) != 1
            || instructions.Count(instruction => instruction.Opcode == SelectValue) != 1
            || instructions.Count(instruction => instruction.Opcode == LoopReceiveEnd) != 1
            || instructions.Count(instruction => instruction.Opcode == ReceiveTimeout) != 1
            || instructions.Any(instruction => instruction.Opcode == WaitTimeout))
        {
            throw new InvalidOperationException(BrokenDispatch);
        }
    }
}
