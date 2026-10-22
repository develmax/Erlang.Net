using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class DebuggerDisabled413Access
{
    internal static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(DebuggerDisabled413Names.Failure);
    }

    private static byte[] Resource(string name)
    {
        using Stream stream = typeof(DebuggerDisabled413Access).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidDataException(DebuggerDisabled413Names.Failure);
        using var memory = new MemoryStream();
        stream.CopyTo(memory);

        return memory.ToArray();
    }

    internal static byte[] Beam()
    {
        byte[] beam = Resource(DebuggerDisabled413Names.Resource);
        var rows = JsonSerializer.Deserialize<Dictionary<string, string>>(Resource(DebuggerDisabled413Names.Manifest))!;
        Require(rows.Count == 1 && rows.TryGetValue(DebuggerDisabled413Names.Module + ".beam", out string? hash)
            && Convert.ToHexString(SHA256.HashData(beam)) == hash);

        return beam;
    }

    internal static ErlangFunction Capture(ProcessRuntime runtime)
    {
        Type type = typeof(ProcessRuntime).Assembly.GetType(DebuggerDisabled413Names.NativeType, true)!;
        object?[] args = [runtime, DebuggerDisabled413Names.Module, "supported", 0, null];
        Require((bool)type.GetMethod(DebuggerDisabled413Names.Capture, DebuggerDisabled413Names.Members)!.Invoke(null, args)!);

        return (ErlangFunction)args[4]!;
    }

    internal static void Cancel(ProcessContext context)
        => ((CancellationTokenSource)typeof(ProcessContext).GetProperty(DebuggerDisabled413Names.Stop,
            DebuggerDisabled413Names.Members)!.GetValue(context)!).Cancel();
}
