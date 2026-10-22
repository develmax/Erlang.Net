using System.Reflection;

namespace Erlang.Tests;

internal static class DebugUnaligned421Names
{
    internal const string SourceTest = "HOST unaligned actual shifted reference backing and visible bits";
    internal const string OwnerTest = "HOST unaligned owner cancellation closed and argument refusal";
    internal const string Prefix = "Erlang.Tests.DebugUnaligned421.";
    internal const string Module = "erts_debug", Function = "unaligned_bitstring", Runtime = "erlang";
    internal const string Native = "Erlang.DebugUnalignedNative421", Capture = "TryCapture";
    internal const string Manifest = "manifest.json", Beam = "erts_debug.beam";
    internal const string Backing = "backing", Offset = "bitOffset", Buffer = "Buffer", Reference = "IsReference";
    internal const string Invalid = "invalid";
    internal const string Failure = "Unaligned421 actual backing or native owner contract failed";
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic;
}
