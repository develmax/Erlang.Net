namespace Erlang.Tests;

internal static class BinaryNestedPartOverrideNames
{
    internal const string Case = "runtime/binary-wrapper/nested-public-part-override-stays-opaque";
    internal const string Module = "binary";
    internal const string Part = "part";
    internal const string Match = "match";
    internal const string Gate = "gate";
    internal const string Functions = "functions";
    internal const string Selection = "SelectedOwnedBinding507";
    internal const string Current = "CurrentOwned506";
    internal const string Kind = "BindingKind507";
    internal const string Opaque = "Opaque";
    internal const string ReferencedBytes = "ReferencedByteSize";
    internal const string Entry = "run";
    internal const string Failure = "Nested binary part override acquired authority or bypassed its real Host boundary.";
    internal const int BitsPerByte = 8;
    internal const int ReplacementSize = 257;
    internal const string SourceModule = "binary_nested_part_override";
    internal const string Source = "-module(binary_nested_part_override). -export([run/0]). run()-> R=append_loop(257,<<>>), binary:replace(<<1>>,<<1>>,R,[{insert_replaced,[0]}]). append_loop(0,B)->B; append_loop(N,B)->append_loop(N-1,<<B/binary,7>>).";
}
