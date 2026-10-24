using Erlang;
using static Erlang.Tests.BeamCollectionFloatControlNames;

namespace Erlang.Tests;

internal static class BeamCollectionFloatControlChecks
{
    internal static Task LocalCallStale() => Refuse(BeamCollectionFloatControlBytes.AcrossCall(false, false));

    internal static Task LocalCallFresh() => Accept(BeamCollectionFloatControlBytes.AcrossCall(false, true), 3);

    internal static Task NativeCallStale() => Refuse(BeamCollectionFloatControlBytes.AcrossCall(true, false));

    internal static Task NativeCallFresh() => Accept(BeamCollectionFloatControlBytes.AcrossCall(true, true), 3);

    internal static Task HeapRetains() => Accept(BeamCollectionFloatControlBytes.AcrossHeap(), 2);

    internal static Task InitialRead() => Refuse(BeamCollectionFloatControlBytes.Uninitialized());

    private static Task Refuse(byte[] image)
    {
        bool refused;
        try
        {
            refused = !BeamModule.Prepare(image).Supports(BeamOpcodeEdgeNames.Run, 0);
        }
        catch (Exception failure) when (failure is InvalidDataException or NotSupportedException)
        {
            refused = true;
        }
        Require(refused);

        return Task.CompletedTask;
    }

    private static async Task Accept(byte[] image, double expected)
    {
        BeamModule module = BeamModule.Prepare(image);
        Require(module.Supports(BeamOpcodeEdgeNames.Run, 0));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term actual = await module.Invoke(context, BeamOpcodeEdgeNames.Run);
            Require(actual is FloatTerm number && number.Value == expected);

            return true;
        }).WaitAsync(Deadline);
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(Failure);
    }
}
