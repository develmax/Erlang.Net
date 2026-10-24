using Erlang;
using System.Security.Cryptography;
using System.Text.Json;
namespace Erlang.Tests;

internal static class InternalPortLoader428HostAccess
{
    internal static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(InternalPortLoader428HostNames.Failure);
    }

    internal static ErlangFunction Body(ProcessRuntime runtime, string function, int arity)
    {
        var type = typeof(ProcessRuntime).Assembly.GetType(InternalPortLoader428HostNames.NativeType, true)!;
        object?[] args = [runtime, InternalPortLoader428HostNames.Module, function, arity, null];
        Require((bool)type.GetMethod(InternalPortLoader428HostNames.Capture, InternalPortLoader428HostNames.Flags)!.Invoke(null, args)!);

        return (ErlangFunction)args[4]!;
    }

    internal static byte[] Resource(string name)
    {
        byte[] Read(string file)
        {
            using var input = typeof(InternalPortLoader428HostAccess).Assembly.GetManifestResourceStream(InternalPortLoader428HostNames.Prefix + file)!;
            using var output = new MemoryStream();
            input.CopyTo(output);

            return output.ToArray();
        }
        var hashes = JsonSerializer.Deserialize<Dictionary<string, string>>(Read(InternalPortLoader428HostNames.Manifest))!;
        byte[] bytes = Read(name);
        Require(hashes.TryGetValue(name, out string? expected) && expected == Convert.ToHexString(SHA256.HashData(bytes)));

        return bytes;
    }
}
