using System.Reflection;
namespace Erlang.Tests;

internal static class DebugSame420HostNames
{
    internal const string SourceTest = "HOST same actual source word identity and boxed alias distinction", OwnerTest = "HOST same source owner foreign/canceled/closed and full identifier domains";
    internal const string Module = "erts_debug", Function = "same", Runtime = "erlang", Binary = "term_to_binary", Decode = "binary_to_term", Native = "Erlang.DebugSameNative420", Capture = "TryCapture", Prefix = "Erlang.Tests.DebugSame420.", Manifest = "manifest.json", Beam = "erts_debug.beam", Failure = "same420 actual source word identity control failed", Atom = "same420_atom", Other = "same420_other";
    internal const BindingFlags Members = BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic;
}
