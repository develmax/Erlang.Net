using Erlang;
using static Erlang.Tests.BeamFloatStore479Names;

namespace Erlang.Tests;

internal static class BeamFloatStore479Checks
{
    internal static Task Store() => Accept(BeamFloatStore479Bytes.Store());

    internal static Task Trim() => Accept(BeamFloatStore479Bytes.Store(trim: true));

    internal static Task GcRoots() => Refuse(BeamFloatStore479Bytes.Store(gcBeforeFilled: true));

    internal static Task UninitializedRead() => Refuse(BeamFloatStore479Bytes.Store(readBeforeFilled: true));

    internal static Task UninitializedFloat() => Refuse(BeamFloatStore479Bytes.Store(invalidFloat: true));

    internal static Task DestinationBounds() => Refuse(BeamFloatStore479Bytes.Store(invalidDestination: true));

    internal static Task HandlerOwnership() => Refuse(BeamFloatStore479Bytes.Store(overwriteHandler: true));

    private static Task Refuse(byte[] image)
    {
        bool refused = false;
        try
        {
            refused = !BeamModule.Prepare(image).Supports(BeamOpcodeEdgeNames.Run, 0);
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            refused = true;
        }
        Require(refused);

        return Task.CompletedTask;
    }

    private static async Task Accept(byte[] image)
    {
        BeamModule module = BeamModule.Prepare(image);
        Require(module.Supports(BeamOpcodeEdgeNames.Run, 0));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term result = await module.Invoke(context, BeamOpcodeEdgeNames.Run);
            Require(result is FloatTerm number && number.Value == 2.0);

            return true;
        }).WaitAsync(Deadline);
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(Failure);
    }
}
