using System.Collections;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamZeroSelectiveTimeoutChecks
{
    internal const string TestName = "compiler/beam/emits-immediate-selective-timeout";
    private const string IncorrectWait = "The selective receive did not preserve its timeout and scan instructions.";
    private const string IncorrectQueue = "The immediate timeout changed the mailbox scan or retained message order.";
    private const string Source = """
        -module(beam_zero_selective_timeout).
        -export([poll/1, positive/1, dynamic/2]).

        poll(Key) ->
            receive
                {packet, Key, Value} -> Value
            after 0 ->
                expired
            end.

        positive(Key) ->
            receive
                {packet, Key, Value} -> Value
            after 7 ->
                expired
            end.

        dynamic(Key, Milliseconds) ->
            receive
                {packet, Key, Value} -> Value
            after Milliseconds ->
                expired
            end.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, "beam_zero_selective_timeout.erl");
        object code = Code(image);
        RequireWait(code, "poll", false);
        RequireWait(code, "positive", true);
        RequireWait(code, "dynamic", true);
        BeamModule beam = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term key = Term.A("wanted");
            Term other = Term.Tuple(Term.A("packet"), Term.A("wrong"), Term.I(1));
            context.Mailbox.Send(other);
            Term expired = await beam.Invoke(context, "poll", key);
            context.Mailbox.Send(Term.Tuple(Term.A("packet"), key, Term.I(2)));
            Term accepted = await beam.Invoke(context, "poll", key);
            Term? retained = await context.ReceiveAsync(value => value, TimeSpan.Zero);
            if (!expired.Equals(Term.A("expired")) || !accepted.Equals(Term.I(2)) || !other.Equals(retained)
                || await context.ReceiveAsync(value => value, TimeSpan.Zero) is not null)
            {
                throw new InvalidOperationException(IncorrectQueue);
            }

            return Nil.Value;
        });
    }

    private static void RequireWait(object code, string name, bool timedWait)
    {
        var functions = (IEnumerable)Property(code, "Functions");
        object function = functions.Cast<object>().Single(item => (string)Property(item, "Name") == name);
        int first = (int)Property(function, "FirstInstructionIndex");
        int end = (int)Property(function, "EndInstructionIndex");
        var instructions = (IEnumerable)Property(code, "Instructions");
        byte[] opcodes = instructions.Cast<object>().Skip(first).Take(end - first)
            .Select(instruction => (byte)Property(Property(instruction, "Definition"), "Opcode"))
            .ToArray();
        if (!opcodes.Contains(LoopReceive) || !opcodes.Contains(ReceiveTimeout) || opcodes.Contains(WaitTimeout) != timedWait)
        {
            throw new InvalidOperationException(IncorrectWait);
        }
    }
}
