using Erlang;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamTupleSelectHostChecks
{
    internal static async Task Branches()
    {
        byte[] bytes = Image(Code(), labels: 6);
        await using var runtime = new ProcessRuntime();
        PreparedBeamImage image = runtime.Modules.PrepareBeam(bytes);
        Require(runtime.Modules.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        foreach (var pair in new (Term, int)[] { (Term.Tuple(), 0), (Term.Tuple(Term.I(1), Term.I(2)), 2), (Term.Tuple(Term.I(1)), 9), (Term.I(7), 9) })
        {
            Term value = await actor.InvokeAsync(context => runtime.Modules.Call(
                context,
                BeamOpcodeEdgeNames.Module,
                BeamOpcodeEdgeNames.Run,
                pair.Item1
            ).AsTask());
            Require(value.Equals(Term.I(pair.Item2)));
        }
    }

    internal static async Task Rejections()
    {
        foreach (byte[] code in new[]
        {
            Code(proof: false),
            Code(guardRegister: 1),
            Code(choices: [U(0), F(3), U(0), F(4)]),
            Code(choices: [I(0), F(3), U(2), F(4)]),
            Code(choices: [U(0), F(7), U(2), F(4)])
        })
        {
            await using var runtime = new ProcessRuntime();
            bool refused = false;
            try
            {
                _ = runtime.Modules.PrepareBeam(Image(code, labels: 6));
            }
            catch (Exception error) when (error is InvalidDataException or NotSupportedException)
            {
                refused = true;
            }
            Require(refused);
        }

    }

    internal static async Task Receive()
    {
        byte[] code = [.. Start(
            1,
            2,
            2,
            0
        ), 12, U(0), U(0),
            1, U(3), 23, F(6), X(0),
            BeamTupleSelectHostNames.TupleTestOpcode, F(5), X(0),
            BeamTupleSelectHostNames.SelectOpcode, X(0), F(5), BeamTupleSelectHostNames.ExtendedList, U(4), U(0), F(4), U(2), F(4),
            1, U(4), 21, 18, U(0), 19,
            1, U(5), 24, F(3),
            1, U(6), 26, F(3), I(0), 22, 64, I(9), X(0), 18, U(0), 19];
        await using var runtime = new ProcessRuntime();
        PreparedBeamImage image = runtime.Modules.PrepareBeam(Image(code, labels: 7, arity: 0));
        Require(runtime.Modules.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term scalar = Term.I(7);
            Term wrongArity = Term.Tuple(Term.I(1));
            Term expected = Term.Tuple(Term.I(1), Term.I(2));
            Term later = Term.Tuple();
            foreach (Term message in new[] { scalar, wrongArity, expected, later })
                context.Mailbox.Send(message);
            Term actual = await runtime.Modules.Call(context, BeamOpcodeEdgeNames.Module, BeamOpcodeEdgeNames.Run);
            Require(actual.Equals(expected) && context.Mailbox.Count == 3);
            foreach (Term message in new[] { scalar, wrongArity, later })
                Require((await context.ReceiveAsync(value => value, TimeSpan.Zero))!.Equals(message));

            return true;
        });
    }

    private static byte[] Code(bool proof = true, int guardRegister = 0, byte[]? choices = null)
    {
        choices ??= [U(0), F(3), U(2), F(4)];

        return [.. Start(
            1,
            2,
            2,
            1
        ),
            .. (proof ? new byte[] { BeamTupleSelectHostNames.TupleTestOpcode, F(5), X(guardRegister) } : Array.Empty<byte>()),
            BeamTupleSelectHostNames.SelectOpcode, X(0), F(5), BeamTupleSelectHostNames.ExtendedList, U(choices.Length), .. choices,
            1, U(3), 64, I(0), X(0), 19,
            1, U(4), 64, I(2), X(0), 19,
            1, U(5), 64, I(9), X(0), 19];
    }

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(BeamTupleSelectHostNames.Failure);
    }
}
