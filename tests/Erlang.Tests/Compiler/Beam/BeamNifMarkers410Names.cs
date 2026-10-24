using System.Reflection;

namespace Erlang.Tests;

internal static class BeamNifMarkers410Names
{
    internal const string ActualTest = "HOST/beam410/original-prim-file-exact33-nif-declarations";
    internal const string VariantsTest = "HOST/beam410/raw-nif-marker-eligibility-and-entry";
    internal const string Failure = "BEAM410 NIF marker metadata control failed";
    internal const string Descriptor = "Descriptor";
    internal const string Table = "NifBindings";
    internal const string Begin = "Begin";
    internal const string Publish = "Publish";
    internal const string Retire = "RetireAsync";
    internal const string PrimFile = "prim_file";
    internal const string PrimImage = "prim_file.beam";
    internal const string Erlang = "erlang";
    internal const string ErrorFunction = "error";
    internal const byte Marker = 179;
    internal const int NativeValue = 171;
    internal const int MarkerCount = 33;
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
}
