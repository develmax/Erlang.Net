using Erlang;
using static Erlang.Tests.BeamInitializedRootNames;

namespace Erlang.Tests;

internal static class BeamInitializedRootChecks
{
    internal static Task GcOnly() => Accept(BeamInitializedRootShape.GcOnly);

    internal static Task TrimGc() => Accept(BeamInitializedRootShape.TrimGc);

    internal static Task TransportEscape() => Refuse(BeamInitializedRootShape.TransportEscape);

    internal static Task TupleEscape() => Refuse(BeamInitializedRootShape.TupleEscape);

    internal static Task TrimEscape() => Refuse(BeamInitializedRootShape.TrimEscape);

    internal static Task ReallocateUninitialized() => Refuse(BeamInitializedRootShape.ReallocateUninitialized);

    internal static Task OverwriteThenRead() => Accept(BeamInitializedRootShape.OverwriteThenRead);

    private static async Task Accept(BeamInitializedRootShape shape)
    {
        BeamModule module = BeamModule.Prepare(BeamInitializedRootBytes.Image(shape));
        Require(module.Supports(BeamOpcodeEdgeNames.Run, Arity));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term expected = shape == BeamInitializedRootShape.OverwriteThenRead
                ? Nil.Value : Term.A(BeamOpcodeEdgeNames.Marker);
            foreach (Term argument in new[] { Term.A(BeamOpcodeEdgeNames.Marker), Term.A(BeamOpcodeEdgeNames.Other) })
            {
                Term actual = await module.Invoke(context, BeamOpcodeEdgeNames.Run, argument);
                Require(actual.Equals(expected));
            }

            return true;
        }).WaitAsync(Deadline);
    }

    private static Task Refuse(BeamInitializedRootShape shape)
    {
        bool refused;
        try
        {
            refused = !BeamModule.Prepare(BeamInitializedRootBytes.Image(shape)).Supports(BeamOpcodeEdgeNames.Run, Arity);
        }
        catch (Exception failure) when (failure is InvalidDataException or NotSupportedException)
        {
            refused = true;
        }
        Require(refused);

        return Task.CompletedTask;
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(Failure);
    }
}
