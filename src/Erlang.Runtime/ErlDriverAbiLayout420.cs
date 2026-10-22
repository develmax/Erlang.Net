using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang;

// Host-private capability input: the root-owned build must bind this probe to
// exact compiler, header and execution receipts before publishing a provider.
internal sealed class ErlDriverAbiLayout420
{
    private readonly Dictionary<string, int> fields;

    private ErlDriverAbiLayout420(Dictionary<string, int> fields)
    {
        this.fields = fields;
        if (!OperatingSystem.IsWindows() || IntPtr.Size != 8
            || Required(ErlDriverAbiNames420.PointerBytes) != IntPtr.Size)
            throw new PlatformNotSupportedException(ErlDriverAbiNames420.InvalidLayout);
        CallbackBytes = Required(ErlDriverAbiNames420.CallbackBytes);
        var intervals = new List<(int Start, int End)>();
        foreach (var slot in ErlDriverCallbackSlots420.Fields)
        {
            int offset = CallbackOffset(slot.Name);
            if (offset < 0 || offset > CallbackBytes - IntPtr.Size)
                throw new InvalidOperationException(ErlDriverAbiNames420.InvalidLayout);
            intervals.Add((offset, checked(offset + IntPtr.Size)));
        }
        var sorted = intervals.OrderBy(value => value.Start).ToArray();
        for (int index = 1; index < sorted.Length; index++)
            if (sorted[index - 1].End > sorted[index].Start)
                throw new InvalidOperationException(ErlDriverAbiNames420.InvalidLayout);
    }

    internal int CallbackBytes { get; }

    internal int CallbackOffset(string field) => Required(ErlDriverAbiNames420.CallbackPrefix + field);

    internal int EntryOffset(string field, int width)
    {
        int offset = Required(ErlDriverAbiNames420.EntryPrefix + field);
        if (width < 1 || offset > Required(ErlDriverAbiNames420.EntryBytes) - width)
            throw new InvalidOperationException(ErlDriverAbiNames420.InvalidLayout);

        return offset;
    }

    internal int Field(string field) => Required(field);

    private int Required(string field) => fields.TryGetValue(field, out int value) && value >= 0
        ? value : throw new InvalidOperationException(ErlDriverAbiNames420.InvalidLayout);

    internal static ErlDriverAbiLayout420 Read(ReadOnlySpan<byte> bytes, string boundSha256)
    {
        if (!Convert.ToHexString(SHA256.HashData(bytes)).Equals(boundSha256, StringComparison.Ordinal))
            throw new InvalidOperationException(ErlDriverAbiNames420.InvalidLayout);
        var fields = JsonSerializer.Deserialize<Dictionary<string, int>>(bytes)
            ?? throw new InvalidOperationException(ErlDriverAbiNames420.InvalidLayout);

        return new(fields);
    }
}
