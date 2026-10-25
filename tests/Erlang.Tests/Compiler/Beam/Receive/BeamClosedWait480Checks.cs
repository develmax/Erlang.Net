using Erlang;

namespace Erlang.Tests;

internal static class BeamClosedWait480Checks
{
    internal static async Task Valid()
    {
        BeamModule module = BeamModule.Prepare(BeamClosedWait480Bytes.Image());
        BeamBinaryHostAccess.Require(module.Supports(BeamOpcodeEdgeNames.Run, 1));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term retained = Term.A(BeamOpcodeEdgeNames.Other);
            context.Mailbox.Send(retained);
            context.Mailbox.Send(Term.Tuple(Term.I(7)));
            Term result = await module.Invoke(context, BeamOpcodeEdgeNames.Run, Nil.Value);
            BeamBinaryHostAccess.Require(result.Equals(Term.A(BeamOpcodeEdgeNames.Marker)) && context.Mailbox.Count == 1);
            BeamBinaryHostAccess.Require((await context.ReceiveAsync(value => value, TimeSpan.Zero))!.Equals(retained));

            return true;
        });
    }

    private static Task Refused(byte[] image)
    {
        bool refused;
        try
        {
            refused = !BeamModule.Prepare(image).Supports(BeamOpcodeEdgeNames.Run, 1);
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            refused = true;
        }
        BeamBinaryHostAccess.Require(refused);

        return Task.CompletedTask;
    }

    internal static Task Returning() => Refused(BeamClosedWait480Bytes.Image(returning: true));

    internal static Task Foreign() => Refused(BeamClosedWait480Bytes.Image(foreign: true));

    internal static Task Roots() => Refused(BeamClosedWait480Bytes.Image(roots: true));

    internal static Task Unknown() => Refused(BeamClosedWait480Bytes.Image(unknown: true));
}
