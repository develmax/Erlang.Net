using Erlang;
using static Erlang.Tests.BeamBinaryProtocol390Bytes;
using static Erlang.Tests.BeamBinaryProtocol390Names;

namespace Erlang.Tests;

internal static class BeamBinaryProtocol390Checks
{
    internal static async Task PositionRoundtrip()
    {
        BitString source = new([0xab, 0xcd]);
        Term actual = await Run(PositionRoundtripImage(), source);
        Require(actual.Equals(source));
    }

    internal static Task OpaquePositionEscape()
    {
        Reject(PositionEscapeImage(native: false), 1);
        Reject(PositionEscapeImage(native: true), 1);
        Reject(ForgedPositionImage(), 2);

        return Task.CompletedTask;
    }

    internal static Task ResumeRejectsOrdinary()
    {
        Reject(LocalResumeImage(contextArgument: false), 1);

        return Task.CompletedTask;
    }

    internal static async Task LocalContextTransport()
    {
        BitString source = new([1, 127, 255]);
        Require((await Run(LocalResumeImage(contextArgument: true), source)).Equals(source));
    }

    internal static async Task UnitTwo()
    {
        byte[] image = UnitTwoImage();
        Require((await Run(image, new BitString([0, 0xab]))).Equals(new BitString([0xab])));
        // Nine remaining bits cannot satisfy ensure_at_least8 plus unit2.
        Require((await Run(image, new BitString([0, 0], 9))).Equals(Term.A(BeamOpcodeEdgeNames.Marker)));
    }

    internal static async Task LiteralNoFail()
    {
        BitString source = new([1, 127, 255]);
        Require((await Run(LiteralImage(source))).Equals(source));
    }

    private static async Task<Term> Run(byte[] image, params Term[] arguments)
    {
        BeamModule module = BeamModule.Prepare(image);
        Require(module.Supports(BeamOpcodeEdgeNames.Run, arguments.Length));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);

        return await actor.InvokeAsync(context => module.Invoke(context, BeamOpcodeEdgeNames.Run, arguments).AsTask()).WaitAsync(Deadline);
    }

    private static void Reject(byte[] image, int arity)
    {
        bool refused;
        try
        {
            refused = !BeamModule.Prepare(image).Supports(BeamOpcodeEdgeNames.Run, arity);
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            refused = true;
        }
        Require(refused);
    }

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(Failure);
    }
}
