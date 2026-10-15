using System.Reflection;

namespace Erlang.Tests;

internal static class BeamAllocationHostChecks
{
    internal static async Task Run()
    {
        byte[] original = RawOnLoadExportsHostChecks.Resource(RawOnLoadExportsHostNames.NewRole + RawOnLoadExportsHostNames.BeamExtension);
        await Reject(original, BeamAllocationHostWire.StackOffset, BeamAllocationHostWire.SignedZero);
        await Reject(original, BeamAllocationHostWire.LiveOffset, BeamAllocationHostWire.SignedZero);
        await Reject(original, BeamAllocationHostWire.AllocationCountOffset, BeamAllocationHostWire.FourEntries);
        await Reject(original, BeamAllocationHostWire.FunKindOffset, BeamAllocationHostWire.UnknownKind);
        await Reject(original, BeamAllocationHostWire.FunCountOffset, BeamAllocationHostWire.SignedZero);
        // Pinned marshal_allocation_list sums repeated kinds; retain that behavior.
        await using var runtime = new ProcessRuntime();
        runtime.Modules.PrepareBeam(Change(original, BeamAllocationHostWire.FloatKindOffset, BeamAllocationHostWire.WordsKind));
    }

    private static async Task Reject(byte[] original, int offset, int encoded)
    {
        await using var runtime = new ProcessRuntime();
        try
        {
            runtime.Modules.PrepareBeam(Change(original, offset, encoded));
        }
        catch (InvalidDataException)
        {
            return;
        }

        throw new InvalidOperationException(BeamAllocationHostWire.Failure);
    }

    private static byte[] Change(byte[] original, int offset, int encoded)
        => (byte[])typeof(BeamOnLoadModeledBytes).GetMethod(BeamAllocationHostWire.Mutate, BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [original, BeamAllocationHostWire.AllocateHeap, offset, encoded])!;
}
