namespace Erlang.Tests;

internal static class BinaryPrimitiveHostChecks
{
    internal static Task Storage()
    {
        var original = new byte[259];
        for (int index = 0; index < original.Length; index++)
            original[index] = (byte)index;
        var binary = new BitString(original);
        original[0] = byte.MaxValue;
        var view = binary.SliceBits(3, 129 * 8);
        var copy = new BitString(view.ToArray());
        if (!view.Equals(copy) || view.BitLength != 129 * 8 || binary.ToArray()[0] != 0)
            throw new InvalidOperationException(BinaryPrimitiveHostNames.Failure);
        using var output = new MemoryStream();
        view.WriteTo(output);
        if (!output.ToArray().SequenceEqual(copy.ToArray()))
            throw new InvalidOperationException(BinaryPrimitiveHostNames.Failure);
        var nested = view.SliceBits(5, 65 * 8);
        var expected = binary.SliceBits(8, 65 * 8);
        if (!nested.Equals(expected) || nested.ToString() != expected.ToString())
            throw new InvalidOperationException(BinaryPrimitiveHostNames.Failure);

        return Task.CompletedTask;
    }
}
