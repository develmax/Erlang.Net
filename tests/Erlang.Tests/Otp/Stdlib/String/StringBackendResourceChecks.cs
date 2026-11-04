namespace Erlang.Tests;

internal static class StringBackendResourceChecks
{
    internal static async Task Run(ProcessContext context, bool managed)
    {
        var aligned = new BitString(new byte[StringBackendResourceNames.InputBytes]);
        BitString unaligned = new BitString(new byte[StringBackendResourceNames.InputBytes + 1]).SliceBits(
            1,
            StringBackendResourceNames.InputBytes * StringBackendResourceNames.BitsPerByte
        );
        foreach (BitString input in new[] { aligned, unaligned })
        {
            long before = GC.GetTotalAllocatedBytes(true);
            Term result = await context.Runtime.Modules.Call(
                context,
                StringModule.Name,
                StringBackendResourceNames.Length,
                input
            );
            long allocated = GC.GetTotalAllocatedBytes(true) - before;
            if (!result.Equals(Term.I(StringBackendResourceNames.InputBytes)))
                throw new InvalidOperationException(StringBackendResourceNames.LengthFailure);
            if (managed && allocated > StringBackendResourceNames.AllocationBudget)
                throw new InvalidOperationException(StringBackendResourceNames.Failure + allocated);
        }
    }
}
