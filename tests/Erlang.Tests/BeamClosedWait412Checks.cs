using Erlang;

namespace Erlang.Tests;

internal static class BeamClosedWait412Checks
{
    internal static async Task Execution()
    {
        BeamModule module = BeamModule.Prepare(BeamClosedWait412Bytes.Image(BeamClosedWait412Shape.Throw));
        BeamBinaryHostAccess.Require(module.Supports(BeamOpcodeEdgeNames.Run, 1));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            context.Mailbox.Send(Term.A(BeamOpcodeEdgeNames.Other));
            context.Mailbox.Send(Term.A(BeamOpcodeEdgeNames.Marker));
            Term result = await module.Invoke(context, BeamOpcodeEdgeNames.Run, Nil.Value);
            BeamBinaryHostAccess.Require(result.Equals(Term.A(BeamOpcodeEdgeNames.Marker)) && context.Mailbox.Count == 1);
            Term? retained = await context.ReceiveAsync(value => value, TimeSpan.Zero);
            BeamBinaryHostAccess.Require(retained!.Equals(Term.A(BeamOpcodeEdgeNames.Other)));

            return true;
        });
    }

    internal static Task Component()
    {
        // Admission only: no unbounded loop is executed by this control.
        BeamBinaryHostAccess.Require(BeamModule.Prepare(BeamClosedWait412Bytes.Image(BeamClosedWait412Shape.MutualCycle))
            .Supports(BeamOpcodeEdgeNames.Run, 1));

        return Task.CompletedTask;
    }

    internal static Task Negatives()
    {
        foreach (BeamClosedWait412Shape shape in new[] { BeamClosedWait412Shape.Return,
            BeamClosedWait412Shape.CaughtReturn, BeamClosedWait412Shape.UnknownTail, BeamClosedWait412Shape.UninitializedRoot })
        {
            bool refused;
            try
            {
                refused = !BeamModule.Prepare(BeamClosedWait412Bytes.Image(shape)).Supports(BeamOpcodeEdgeNames.Run, 1);
            }
            catch (Exception error) when (error is InvalidDataException or NotSupportedException)
            {
                refused = true;
            }
            BeamBinaryHostAccess.Require(refused);
        }
        // Keep historical conservative no-wait pure-cycle admission unchanged.
        foreach (BeamClosureProtocolShape shape in new[] { BeamClosureProtocolShape.LocalCycle, BeamClosureProtocolShape.EntryCycle })
        {
            bool refused;
            try
            {
                refused = !BeamModule.Prepare(BeamClosureProtocolNoReturnBytes.Image(shape))
                    .Supports(BeamOpcodeEdgeNames.Run, 1);
            }
            catch (Exception error) when (error is InvalidDataException or NotSupportedException)
            {
                refused = true;
            }
            BeamBinaryHostAccess.Require(refused);
        }

        return Task.CompletedTask;
    }
}
