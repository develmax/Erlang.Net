using System.Text.Json;

namespace Erlang.NifNativeBackend;

internal static class NifNativeReallocationLayout
{
    internal static unsafe void Check(string proofPath, string trustedProofHash)
    {
        using var file = NifNativeCapabilityProfile.OpenVerified(proofPath, trustedProofHash);
        file.Position = 0;
        using var proof = JsonDocument.Parse(file);
        JsonElement root = proof.RootElement;
        string[] fields =
        [
            NifNativeReallocationHarnessNames.PointerBytes, NifNativeReallocationHarnessNames.TableBytes,
            NifNativeReallocationHarnessNames.FieldCount, NifNativeReallocationHarnessNames.ReallocationSlot,
            NifNativeReallocationHarnessNames.ReallocationOffset, NifNativeReallocationHarnessNames.FirstTailSlot,
            NifNativeReallocationHarnessNames.SecondTailSlot
        ];
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != fields.Length
            || fields.Any(field => !root.TryGetProperty(field, out JsonElement value) || !value.TryGetInt32(out _)))
            throw new InvalidOperationException(NifNativeReallocationHarnessNames.Broken);
        int pointerBytes = root.GetProperty(NifNativeReallocationHarnessNames.PointerBytes).GetInt32();
        int count = root.GetProperty(NifNativeReallocationHarnessNames.FieldCount).GetInt32();
        int slot = root.GetProperty(NifNativeReallocationHarnessNames.ReallocationSlot).GetInt32();
        if (pointerBytes != IntPtr.Size || count != NifNativeReallocationParameters.WindowsFunctionCount
            || slot != NifNativeReallocationParameters.WindowsReallocationSlot
            || root.GetProperty(NifNativeReallocationHarnessNames.TableBytes).GetInt32() != (count + NifNativeReallocationParameters.WindowsTailPointers) * pointerBytes
            || root.GetProperty(NifNativeReallocationHarnessNames.ReallocationOffset).GetInt32() != slot * pointerBytes
            || root.GetProperty(NifNativeReallocationHarnessNames.FirstTailSlot).GetInt32() != count
            || root.GetProperty(NifNativeReallocationHarnessNames.SecondTailSlot).GetInt32() != count + 1)
            throw new InvalidOperationException(NifNativeReallocationHarnessNames.Broken);
        using var oldTable = new NifNativeWindowsCallbacks();
        using var newTable = new NifNativeWindowsCallbacks(true);
        for (int index = 0; index < count + NifNativeReallocationParameters.WindowsTailPointers; index++)
        {
            nint previous = ((nint*)oldTable.Pointer)[index];
            nint current = ((nint*)newTable.Pointer)[index];
            if (index == slot)
            {
                if (previous != 0 || current != (nint)(delegate* unmanaged[Cdecl]<nint, nuint, nint>)&NifNativePrimitiveCallbacks.Reallocate)
                    throw new InvalidOperationException(NifNativeReallocationHarnessNames.Broken);
            }
            else if (previous != current)
                throw new InvalidOperationException(NifNativeReallocationHarnessNames.Broken);
        }
    }
}
