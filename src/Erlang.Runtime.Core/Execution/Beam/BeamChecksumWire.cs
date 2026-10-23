namespace Erlang;

internal static class BeamChecksumWire
{
    internal const int LambdaHeaderBytes = 4;
    internal const int LambdaSignificantBytes = 20;
    internal const int LambdaOldUniquenessBytes = 4;
    internal static readonly string[] BeforeLambda = ["AtU8", "Code", "StrT", "ImpT", "ExpT"];
    internal static readonly string[] AfterLambda = ["LitT", "Meta", "Recs", "DbgB"];
}
