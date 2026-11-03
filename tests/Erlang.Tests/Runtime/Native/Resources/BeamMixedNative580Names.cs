using System.Reflection;

namespace Erlang.Tests;

internal static class BeamMixedNative580Names
{
    internal const string IdentityCase = "runtime/beam-mixed580/resource-identity-and-captured-recipe";
    internal const string OpaqueCase = "runtime/beam-mixed580/local-opaque-native-and-closure-refused";
    internal const string PublicCase = "runtime/beam-mixed580/public-reregister-exact-helper-remains-opaque";
    internal const string PredicateCase = "runtime/beam-mixed580/public-reregister-function-predicate-opaque";
    internal const string IsFunction = "is_function";
    internal const string Reserve = "Reserve", Set = "Set", Read = "Read";
    internal const string Resource = "Erlang.Tests.BeamMixedNative580.erts_debug.beam";
    internal const string NativeModule = "erts_debug";
    internal const string GuestModule = "mixed_native_guest580";
    internal const string Identity = "identity", CaptureOk = "capture_ok", Opaque = "opaque", Closure = "closure";
    internal const string Functions = "functions", Actors = "actors", Domain = "ReferenceResources", Make = "MakeNativeResource524";
    internal const string ScopeRequired = "Native resource host escape requires an explicit native host scope.";
    internal const string Failure = "Mixed original BEAM guest/native boundary control failed.";
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    internal const string Source = "-module(mixed_native_guest580). -export([identity/1,capture_ok/1,opaque/1,closure/1]). identity(T) -> T =:= erts_debug:identity(T). capture_ok(T) -> F=erts_debug:capture(T), is_function(F,0). opaque(T) -> erts_debug:call_opaque(T). closure(T) -> F=erts_debug:capture(T), F().";
}
