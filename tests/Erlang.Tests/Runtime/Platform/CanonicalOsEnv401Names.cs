using System.Reflection;

namespace Erlang.Tests;

internal static class CanonicalOsEnv401Names
{
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    internal const string Case = "runtime/os-env401/exact-captured-allocating-body-and-opaque-fallbacks";
    internal const string Os = "os";
    internal const string OsBeam = "os.beam";
    internal const string Env = "env";
    internal const string PutEnv = "putenv";
    internal const string Probe = "os_env401_probe";
    internal const string Keep = "keep";
    internal const string DictionaryKey = "os_env401_value";
    internal const string Source = "-module(os_env401_probe). -export([keep/0]). keep()->put(os_env401_value,os:env()),ok.";
    internal const string EnvironmentKey = "ERLANG401_NATIVE_ENV";
    internal const string EnvironmentValue = "canonical401";
    internal const string HostKey = "opaque401";
    internal const string HostValue = "host401";
    internal const string Execution = "ExecutionIdentity";
    internal const string Primitives = "NativePrimitives";
    internal const string Capabilities = "OwnedNativeCapabilities507";
    internal const string Key = "Key";
    internal const string Value = "Value";
    internal const string Kind = "LocalOsEnvironment401";
    internal const string CurrentOwned = "CurrentOwned506";
    internal const string BindingKind = "BindingKind507";
    internal const string Opaque = "Opaque";
    internal const string Storage = "OwnedStorage506";
    internal const string Owner = "Owner";
    internal const string LiteralDomain = "LiteralDomain800";
    internal const string Gate = "gate";
    internal const string Data = "data";
    internal const string ForeignDiagnostic = "OS primitive belongs to a different runtime";
    internal const string Failure = "Exact canonical os:env401 allocation or public/Host boundary differs.";
}
