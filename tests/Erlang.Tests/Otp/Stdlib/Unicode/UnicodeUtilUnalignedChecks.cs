using static Erlang.Tests.UnicodeUtilUnalignedNames;

namespace Erlang.Tests;

internal static class UnicodeUtilUnalignedChecks
{
    internal static async Task Run()
    {
        var choices = new RuntimeLibrarySelection([
            RuntimeModuleSelection.Managed("erlang"), RuntimeModuleSelection.Managed("lists"),
            RuntimeModuleSelection.Managed("unicode_util"), RuntimeModuleSelection.Managed("unicode", ["lists", "unicode_util"])]);
        await using var runtime = await ProcessRuntime.CreateAsync(choices, RuntimeNativePrimitives.InternalScalars413, CancellationToken.None);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        var allocations = new List<long>();
        foreach (int count in new[] { Small, Large })
        {
            BitString input = Input(count);
            long allocated = await actor.InvokeWithDeadline500Async(
                async context =>
            {
                long before = GC.GetTotalAllocatedBytes(precise: true);
                Term remaining = input;
                for (int index = 0; index < count; index++)
                {
                    var cell = (Cons)await UnicodeUtilModule.Invoke(context, "cp", [remaining]);
                    Require(cell.Head.Equals(Term.I(Character)));
                    if (index == 0)
                    {
                        Term backing = await runtime.Modules.Call(
                            context,
                            "binary",
                            "referenced_byte_size",
                            cell.Tail
                        );
                        Require(backing.Equals(Term.I(count + 1)));
                    }
                    remaining = cell.Tail;
                }
                Require(remaining is BitString { BitLength: 0 });

                return GC.GetTotalAllocatedBytes(precise: true) - before;
            },
                Deadline
            );
            allocations.Add(allocated);
        }
        Directory.CreateDirectory("artifacts/unicode-growth");
        await File.WriteAllTextAsync("artifacts/unicode-growth/unaligned-allocations.json", System.Text.Json.JsonSerializer.Serialize(allocations));
        Require(allocations[1] <= allocations[0] * MaximumDoubling + NoiseBytes);
        await actor.InvokeAsync(async context =>
        {
            Term normalized = await runtime.Modules.Call(
                context,
                "unicode",
                "characters_to_nfc_list",
                Input(Large)
            );
            Require(Cons.Items(normalized).Count() == Large && Cons.Items(normalized).All(value => value.Equals(Term.I(Character))));

            return true;
        });
        Directory.CreateDirectory("artifacts/unicode-growth");
        await File.WriteAllTextAsync("artifacts/unicode-growth/unaligned-allocations.json", System.Text.Json.JsonSerializer.Serialize(allocations));
    }

    private static BitString Input(int count)
    {
        byte[] source = new byte[count + 1];
        for (int index = 0; index < count; index++)
        {
            source[index] |= Character >> Offset;
            source[index + 1] |= (byte)((Character << (BitsPerByte - Offset)) & byte.MaxValue);
        }

        return new BitString(source).SliceBits(Offset, count * BitsPerByte);
    }

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(Failure);
    }
}
