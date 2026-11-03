using System.Reflection;

namespace Erlang.Tests;

internal static class NativeGuest535Names
{
    internal const string ReadersTest = "native-resource535/canonical-readers-public-refusal";
    internal const string ReregisterTest = "native-resource535/public-reregister-canonical-delegate-opaque";
    internal const string FakeTest = "native-resource535/managed-nif-provider-opaque";
    internal const string GuestModule = "resource_guest535";
    internal const string Read = "read";
    internal const string Echo = "echo";
    internal const string Referenced = "referenced";
    internal const string BinaryModule = "binary";
    internal const string ReferencedFunction = "referenced_byte_size";
    internal const string Functions = "functions";
    internal const string MagicLayout = "Erlang.NativeResourceMagic527";
    internal const string UserOffset = "UserOffset527";
    internal const string Pointer = "Pointer524";
    internal const string Heap = "TermHeap506";
    internal const string Collect = "Collect";
    internal const string ReleaseNative = "ReleaseNative524";
    internal const string Retired = "Retired524";
    internal const string RetireImage = "RetireAsync";
    internal const string NativeImage = "Erlang.ModuleCodeImage";
    internal const string NativeBindings = "NifBindings";
    internal const string FakeModule = "managed_nif535";
    internal const string Begin = "Begin";
    internal const string Publish = "Publish";
    internal const string Admit = "Admit";
    internal const string GuestNative = "GuestNative535";
    internal const string InvokeImage = "InvokeImageNif535";
    internal const string ScopeRequired = "Native resource host escape requires an explicit native host scope.";
    internal const string Failure = "Canonical native guest admission or opaque host isolation failed.";
    internal const string Source = "-module(resource_guest535). -export([read/1,echo/1,referenced/1]). read(B) -> {binary_to_list(B),binary:referenced_byte_size(B),byte_size(B)}. echo(B) -> B. referenced(B) -> binary:referenced_byte_size(B).";
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(15);
}
