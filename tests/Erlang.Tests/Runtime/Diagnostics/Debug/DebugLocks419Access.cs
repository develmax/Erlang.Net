using System.Security.Cryptography;
using System.Text.Json;
namespace Erlang.Tests;

internal static class DebugLocks419Access
{
    internal static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(DebugLocks419Names.Failure);
    }

    internal static byte[] Beam()
    {
        byte[] Read(string name)
        {
            using var s = typeof(DebugLocks419Access).Assembly.GetManifestResourceStream(name)!;
            using var m = new MemoryStream();
            s.CopyTo(m);

            return m.ToArray();
        }
        byte[] beam = Read(DebugLocks419Names.Resource);
        var hashes = JsonSerializer.Deserialize<Dictionary<string, string>>(Read(DebugLocks419Names.Manifest))!;
        Require(hashes.Count == 1 && hashes[DebugLocks419Names.Module + ".beam"] == Convert.ToHexString(SHA256.HashData(beam)));

        return beam;
    }

    internal static ErlangFunction Capture(ProcessRuntime runtime, string function, int arity)
    {
        var method = typeof(ProcessRuntime).Assembly.GetType(DebugLocks419Names.NativeType, true)!.GetMethod(DebugLocks419Names.Capture, DebugLocks419Names.Members)!;
        object?[] args = [runtime, DebugLocks419Names.Module, function, arity, null];
        Require((bool)method.Invoke(null, args)!);

        return (ErlangFunction)args[4]!;
    }

    internal static void Cancel(ProcessContext context) => ((CancellationTokenSource)typeof(ProcessContext).GetProperty(DebugLocks419Names.Stop, DebugLocks419Names.Members)!.GetValue(context)!).Cancel();
}
