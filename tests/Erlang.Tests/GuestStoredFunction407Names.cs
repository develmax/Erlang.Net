namespace Erlang.Tests;

internal static class GuestStoredFunction407Names
{
    internal const string AstTest = "runtime/library407/source-and-external-function-storage";
    internal const string RestoredTest = "runtime/library407/restored-function-storage-no-sdk-callback-on-read";
    internal const string BeamTest = "runtime/library407/original-beam-restored-fun-entry-and-purge";
    internal const string ModuleName = "stored_source407";
    internal const string Make = "make";
    internal const string Named = "named";
    internal const string External = "external";
    internal const string Hidden = "hidden";
    internal const string Source = "-module(stored_source407). -export([make/1,named/0,external/0]). make(E)->fun(A)->{E,A} end. named()->fun hidden/1. hidden(A)->{hidden,A}. external()->fun erlang:length/1.";
    internal const string Interpreted = "fun(A)->{interpreted,A} end";
    internal const string InterpretedAtom = "interpreted";
    internal const string SdkModule = "opaque_factory407";
    internal const string SdkName = "callback";
    internal const string SdkResult = "sdk_executed_on_invoke";
    internal const string Marker = "marker407";
    internal const string Length = "length";
    internal const string Failure = "Guest function storage recipe control failed";
    internal const string IdentityModule = "module";
    internal const string IdentityName = "name";
    internal const string IdentityArity = "arity";
    internal const string IdentityEnvironment = "env";
    internal static readonly byte[] ResourceBytes = [4, 0, 7];
}
