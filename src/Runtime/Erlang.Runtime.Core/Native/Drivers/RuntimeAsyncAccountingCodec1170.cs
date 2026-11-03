using System.Numerics;
using System.Text.Json;

namespace Erlang;

internal static class RuntimeAsyncAccountingCodec1170
{
    internal static string Action(RuntimeMicrostateAction action) => action switch
    {
        RuntimeMicrostateAction.Enable => ErlDriverAsyncAccountingWireNames1151.Enable,
        RuntimeMicrostateAction.Disable => ErlDriverAsyncAccountingWireNames1151.Disable,
        RuntimeMicrostateAction.Reset => ErlDriverAsyncAccountingWireNames1151.Reset,
        RuntimeMicrostateAction.Gather => ErlDriverAsyncAccountingWireNames1151.Gather,
        _ => throw new ArgumentOutOfRangeException(nameof(action))
    };

    internal static T Decode<T>(byte[] bytes)
    {
        using var document = JsonDocument.Parse(bytes);
        if (document.RootElement.ValueKind != JsonValueKind.Object) throw Invalid();
        UniqueProperties(document.RootElement);

        return ErlDriverPrimitiveWire420.Decode<T>(bytes);
    }

    private static void UniqueProperties(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw Invalid();
                UniqueProperties(property.Value);
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
            foreach (var item in value.EnumerateArray()) UniqueProperties(item);
    }

    internal static InvalidDataException Invalid() => new(DriverParentWireNames422.Invalid);

    internal static void Capture(ErlDriverAsyncAccountingCaptureReply1151 value, ulong epoch, int expected)
    {
        if (value.Version != ErlDriverAsyncAccountingWireNames1151.Version
            || value.Profile != ErlDriverAsyncAccountingWireNames1151.Profile || value.Epoch != epoch
            || value.Token == 0 || value.Generation is null
            || !Guid.TryParseExact(value.Generation, "N", out var generation)
            || generation.ToString("N") != value.Generation || expected < 0
            || expected > RuntimeDriverFrontendOptionNames424.MaximumAsyncThreads || value.Count != (uint)expected
            || value.Frequency != ErlDriverAsyncAccountingNames1126.PublicFrequency
            || (value.Count == 0 ? value.RawQpcFrequency != 0
                : value.RawQpcFrequency == 0 || value.RawQpcFrequency > uint.MaxValue))
            throw Invalid();
    }

    private static void Echo(
        int version,
        string profile,
        ulong epoch,
        string generation,
        ulong token,
        ErlDriverAsyncAccountingCaptureReply1151 captured
    )
    {
        if (version != captured.Version || profile != captured.Profile || epoch != captured.Epoch
            || generation != captured.Generation || token != captured.Token) throw Invalid();
    }

    internal static void Ack(ErlDriverAsyncAccountingAck1151 value,
        ErlDriverAsyncAccountingCaptureReply1151 captured, string phase)
    {
        Echo(
            value.Version,
            value.Profile,
            value.Epoch,
            value.Generation,
            value.Token,
            captured
        );
        if (value.Phase != phase) throw Invalid();
    }

    internal static RuntimeMicrostateSnapshot[] Rows(ErlDriverAsyncAccountingApplyReply1151 value,
        ErlDriverAsyncAccountingCaptureReply1151 captured, string action)
    {
        Echo(
            value.Version,
            value.Profile,
            value.Epoch,
            value.Generation,
            value.Token,
            captured
        );
        if (value.Action != action || value.Rows is null
            || value.Rows.Length != (action == ErlDriverAsyncAccountingWireNames1151.Gather ? captured.Count : 0))
            throw Invalid();
        var snapshots = new RuntimeMicrostateSnapshot[value.Rows.Length];
        var physicalThreads = new HashSet<uint>();
        for (int index = 0; index < value.Rows.Length; index++)
        {
            var row = value.Rows[index];
            if (row is null || row.Identity != (uint)index || row.OperatingSystemThread == 0
                || !physicalThreads.Add(row.OperatingSystemThread)
                || row.State >= ErlDriverAsyncAccountingNames1126.States || row.Frequency != captured.Frequency
                || row.Counters is null || row.Counters.Length != ErlDriverAsyncAccountingNames1126.States)
                throw Invalid();
            var counters = new BigInteger[ErlDriverAsyncAccountingNames1126.States];
            for (int state = 0; state < counters.Length; state++)
                counters[state] = new BigInteger(unchecked((long)row.Counters[state]));
            snapshots[index] = new(RuntimeMicrostateNames.Async, index, counters);
        }

        return snapshots;
    }

    internal static void RequireFrameBudget(int count, int maximum)
    {
        if (count < 0 || count > RuntimeDriverFrontendOptionNames424.MaximumAsyncThreads) throw Invalid();
        // Conservative byte-size model only, never emitted/registered sample rows.
        // Include outer byte[] Base64 expansion and all maximum-width raw words.
        long inner = checked(RuntimeAsyncAccountingNames1170.InnerHeaderBound
            + count * RuntimeAsyncAccountingNames1170.InnerRowBound);
        long total = checked(RuntimeAsyncAccountingNames1170.OuterHeaderBound + ((inner + 2) / 3) * 4);
        if (total > maximum) throw new NotSupportedException(RuntimeAsyncAccountingNames1170.Unsupported);
    }
}
