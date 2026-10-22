using Erlang;

namespace Erlang.Tests;

internal static class BeamPureMailboxControlHostChecks
{
    internal static async Task SelectionAndRetention()
    {
        BeamModule module = BeamModule.Prepare(BeamPureMailboxControlBytes.Image());
        Require(module.Supports(BeamOpcodeEdgeNames.Run, 0));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term wrongShape = Term.A(BeamOpcodeEdgeNames.Other);
            Term badNode = Term.Tuple(Term.I(0), Term.I(3));
            Term below = Term.Tuple(context.Self, Term.I(1));
            Term upper = Term.Tuple(context.Self, Term.I(5));
            Term selected = Term.Tuple(context.Self, new FloatTerm(2.0));
            Term later = Term.Tuple(context.Self, Term.I(4));
            Term[] original = [wrongShape, badNode, below, upper, selected, later];
            foreach (Term message in original) context.Mailbox.Send(message);
            Term actual = await module.Invoke(context, BeamOpcodeEdgeNames.Run);
            Require(actual.Equals(selected) && context.Mailbox.Count == 5);
            foreach (Term expected in new[] { wrongShape, badNode, below, upper, later })
            {
                Term? message = await context.ReceiveAsync(value => value, TimeSpan.Zero);
                Require(message is not null && message.Equals(expected));
            }
            Require(context.Mailbox.Count == 0);
            context.Mailbox.Send(badNode);
            Require((await module.Invoke(context, BeamOpcodeEdgeNames.Run)).Equals(Term.A(BeamOpcodeEdgeNames.Marker)));
            Require(context.Mailbox.Count == 1);
            Require((await context.ReceiveAsync(value => value, TimeSpan.Zero))!.Equals(badNode));

            return true;
        }).WaitAsync(BeamCatchProtocolControlNames.Deadline);
    }

    internal static Task ImpureImportRefused()
    {
        bool refused = false;
        try
        {
            refused = !BeamModule.Prepare(BeamPureMailboxControlBytes.Image("length")).Supports(BeamOpcodeEdgeNames.Run, 0);
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            refused = true;
        }
        Require(refused);

        return Task.CompletedTask;
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(BeamCatchProtocolControlNames.Failure);
    }
}
